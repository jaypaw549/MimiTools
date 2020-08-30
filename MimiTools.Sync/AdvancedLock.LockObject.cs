using MimiTools.Memory;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MimiTools.Sync
{
    public partial class AdvancedLock
    {
        private class LockObject : IStackable<LockObject>, ILockObject, IDisposable
        {
            /// <summary>
            /// Enabler for recursive region entry. Will hopefully prevent deadlocks from trying to enter the same region twice.
            /// </summary>
            private readonly ThreadLocal<RegionProtect64.Region> m_current_region = new ThreadLocal<RegionProtect64.Region>();

            /// <summary>
            /// Provides thread safety in this class. Entering a region freezes the values for
            /// <see cref="m_state"/>, <see cref="m_first"/>, <see cref="m_last"/>, and <see cref="m_exclusive_requests"/>.
            /// Also freezes <see cref="m_prev"/>, <see cref="m_next"/>, <see cref="m_depth"/> and <see cref="m_parent"/> for all child objects.
            /// 
            /// Entering multiple regions must happen in order of parent => child.
            /// If you must enter the regions of two siblings, then enter the parent region first.
            /// Please use <see cref="EnterRegion(bool, out RegionType)"/> and <see cref="ExitRegion(ref RegionType)"/> to access this field,
            /// they work with <see cref="m_current_region"/> to allow re-entrancy.
            /// </summary>
            private RegionProtect64 m_protect = new RegionProtect64();

            /// <summary>
            /// The parent of this request. This value is read outside of protected regions, so take care when using it.<br/>
            /// <br/>
            /// <b>Extra Rules</b><br/>
            /// If you read the field, do the following:<br/>
            ///     1) if it's null, abort<br/>
            ///     2) Enter a shared or exclusive region on the read value<br/>
            ///     3) Verify the field is still has the same value you read<br/>
            ///     4) if it's not, leave the region and re-read the field.<br/>
            ///     5) Exit the region when you are done with this field/relationship.<br/>
            /// <br/>
            /// Do not write to this more than necessary!
            /// </summary>
            private volatile LockObject m_parent;

            /// <summary>
            /// Various sibling and child relationship fields.<br/>
            /// Siblings: <see cref="m_prev"/> and <see cref="m_next"/><br/>
            /// Children: <see cref="m_first"/> and <see cref="m_last"/>
            /// </summary>
            private LockObject m_first, m_last, m_prev, m_next;

            /// <summary>
            /// The maximum possible depth this node could be at. This value is read outside of protected regions, so take care when using it.
            /// <br/><br/><b>Extra Rules</b><br/>
            /// 1) Can only increase the value when assigning it to a parent node.<br/>
            /// 2) Can only decrease the value unless rule #1 applies
            /// </summary>
            private volatile int m_depth;

            /// <summary>
            /// The number of objects that requested this object become exclusive, including itself if applicable.
            /// </summary>
            private int m_exclusive_requests;

            /// <summary>
            /// Whether or not this object itself wishes to be exclusive
            /// </summary>
            private bool m_self_exclusive_request;

            /// <summary>
            /// The continuations to invoke when the request is granted, cancelled, or upgraded
            /// </summary>
            private Action m_continuation;

            /// <summary>
            /// The current state of the object, currently tracks shared/exclusive modes, and pending/granted/cancelled/released states.
            /// </summary>
            private StateValues m_state;

            private bool HasExclusiveRequests
            {
                get
                {
                    RegionType region = RegionType.None;
                    try
                    {
                        EnterRegion(false, out region);
                        return m_exclusive_requests > 0;
                    }
                    finally
                    {
                        ExitRegion(ref region);
                    }
                }
            }

            internal LockObject()
            {
                Init();
            }

            public bool AppendChild(LockObject child)
            {
                if (child.CheckValue(StateValues.EXCLUSIVE, StateValues.EXCLUSIVE))
                    return AppendExclusiveChild(child);

                RegionType region = RegionType.None;
                try
                {
                    EnterRegion(true, out region);

                    if (!UnsafeCheckValue(StateValues.GRANTED, StateValues.STATE_MASK))
                        return false;

                    bool upgradeable = child.CheckValue(StateValues.UPGRADEABLE, StateValues.UPGRADEABLE);
                    if (upgradeable)
                    {
                        if (UnsafeCheckValue(StateValues.UNSET, StateValues.MODE_MASK))
                            return false;

                        //We cannot let exclusive-only parent requests downgrade if we are upgradeable, so we will increment the request count
                        if (UnsafeCheckValue(StateValues.EXCLUSIVE, StateValues.MODE_MASK))
                            m_exclusive_requests++;
                    }

                    if (m_last == null)
                        m_first = m_last = child;

                    else if (!upgradeable)
                    {
                        LockObject last = m_last;

                        while (last != null && last.CheckValue(StateValues.UPGRADEABLE, StateValues.MODE_MASK))
                            last = last.m_prev;

                        if (last == null)
                        {
                            if (m_first != null)
                            {
                                child.m_next = m_first;
                                m_first.m_prev = child;
                            }
                            UnsafeUpdateChainEnds(m_first, child, m_first ?? child);
                        }
                        else
                            m_first.UnsafeInsertObjectAfter(child);
                    }

                    else
                        m_last.UnsafeInsertObjectAfter(child);

                    child.m_parent = this;
                    child.m_depth = m_depth + 1;
                }
                finally
                {
                    ExitRegion(ref region);
                }

                return true;
            }

            private bool AppendExclusiveChild(LockObject child)
            {
                Span<RegionType> regions = stackalloc RegionType[m_depth + 1];
                try
                {
                    ChainEnterRegion(regions, null, true);

                    if (!UnsafeCheckValue(StateValues.GRANTED, StateValues.STATE_MASK))
                        return false;

                    //If we cannot upgrade or hold an exclusive lock
                    if (UnsafeCheckValue(StateValues.UNSET, StateValues.MODE_MASK))
                        return false;

                    UnsafeTryUpgrade();

                    if (m_last == null)
                        m_first = m_last = child;
                    else
                        m_last.UnsafeInsertObjectAfter(child);

                    child.m_parent = this;
                    child.m_depth = m_depth + 1;
                }
                finally
                {
                    ChainExitRegion(this, regions);
                }

                return true;
            }

            public bool Cancel()
                => Release(StateValues.UNSET, StateValues.CANCELLED, false);

            public void CancelOrThrow()
                => Release(StateValues.UNSET, StateValues.CANCELLED, true);

            private bool ChainEnterRegion(in Span<RegionType> regions, LockObject child, bool exclusive)
            {
                if (m_depth > 0)
                {
                    LockObject parent = m_parent;
                    while (parent != null && !parent.ChainEnterRegion(regions, this, exclusive))
                        parent = m_parent;
                }

                EnterRegion(exclusive, out regions[m_depth]);
                if (child != null && child.m_parent != this)
                {
                    ChainExitRegion(this, regions);
                    return false;
                }

                return true;
            }

            private static void ChainExitRegion(LockObject target, in Span<RegionType> regions)
            {
                while (target != null)
                {
                    target.ExitRegion(ref regions[target.m_depth]);
                    target = target.m_parent;
                }
            }

            public bool CheckValue(StateValues value, StateValues mask)
            {
                RegionType region = default;
                try
                {
                    EnterRegion(false, out region);
                    return UnsafeCheckValue(value, mask);
                }
                finally
                {
                    ExitRegion(ref region);
                }
            }

            private static bool CheckValue(StateValues value, StateValues comparator, StateValues mask)
                => ((value ^ comparator) & mask) == StateValues.UNSET;

            public void Downgrade()
                => Downgrade(this, true);

            private static void Downgrade(LockObject obj, bool self_downgrade)
            {
                Span<RegionType> regions = stackalloc RegionType[obj.m_depth+1];
                regions.Fill(RegionType.None);
                try
                {
                    obj.ChainEnterRegion(regions, null, true);

                    if (self_downgrade)
                    {
                        if (!obj.m_self_exclusive_request)
                            return;

                        obj.m_self_exclusive_request = false;
                    }

                    while (obj != null)
                    {
                        if (--obj.m_exclusive_requests > 0)
                            return;

                        obj.UnsafeSetValue(StateValues.UNSET, StateValues.EXCLUSIVE);
                        obj.ExitRegion(ref regions[obj.m_depth]);

                        UnsafeGrant(obj.m_next);
                        obj = obj.m_parent;
                        self_downgrade = false;
                    }
                }
                finally
                {
                    ChainExitRegion(obj, regions);
                }
            }

            private void EnterRegion(bool exclusive, out RegionType access_mode)
            {
                RegionProtect64.Region region = m_current_region.Value;
                if (!region.IsActive)
                {
                    m_current_region.Value = m_protect.EnterRegion(exclusive);
                    access_mode = RegionType.Created;
                    return;
                }

                if (exclusive && !region.IsExclusive)
                    throw new InvalidOperationException("Cannot upgrade regions! (why do you think this class exists?)");

                access_mode = RegionType.Borrowed;
                return;
            }

            private static void ExecuteContinuation(object state)
                => (state as Action)?.Invoke();

            private void ExitRegion(ref RegionType access_mode)
            {
                if (access_mode == RegionType.Created)
                {
                    RegionProtect64.Region region = m_current_region.Value;
                    m_current_region.Value = default;
                    m_protect.ExitRegion(region);
                }
                access_mode = RegionType.None;
            }

            internal void Grant()
            {
                RegionType region = RegionType.None;
                try
                {
                    if (!TryEnterParentRegion(true, out region))
                        throw new InvalidOperationException("We have no parent, so we can't be granted!");

                    UnsafeGrant(this);
                }
                finally
                {
                    m_parent?.ExitRegion(ref region);
                }
            }

            /// <summary>
            /// Use only when the object doesn't have a parent.
            /// </summary>
            internal void GrantRoot()
                => UnsafeGrant(this);

            internal LockObjectState GetObjectState()
            {
                RegionType region = RegionType.None;
                try
                {
                    EnterRegion(false, out region);
                    return new LockObjectState(m_state);
                }
                finally
                {
                    ExitRegion(ref region);
                }
            }

            private void Init()
            {
                m_parent = m_first = m_last = m_prev = m_next = null;
                m_continuation = null;
                m_depth = 0;
                m_exclusive_requests = 0;
                m_self_exclusive_request = false;
                m_state = StateValues.UNSET | StateValues.UNSET;
            }

            public bool OnCancelledOrGranted(Action continuation)
            {
                if (continuation == null)
                    throw new ArgumentNullException(nameof(continuation));

                bool enqueued;
                RegionType region = RegionType.None;
                try
                {
                    EnterRegion(true, out region);

                    if (enqueued = UnsafeCheckValue(StateValues.UNSET, StateValues.STATE_MASK))
                        m_continuation += continuation;
                }
                finally
                {
                    ExitRegion(ref region);
                }

                return enqueued;
            }

            public bool OnUpgraded(Action continuation)
            {
                if (continuation == null)
                    throw new ArgumentNullException(nameof(continuation));

                bool enqueued;
                RegionType region = default;
                try
                {
                    EnterRegion(true, out region);

                    if (!UnsafeCheckValue(StateValues.GRANTED, StateValues.STATE_MASK))
                        throw new InvalidOperationException("Can only wait for an upgrade on a granted object!");

                    if (enqueued = UnsafeCheckValue(StateValues.UNSET, StateValues.EXCLUSIVE))
                        m_continuation += continuation;
                }
                finally
                {
                    ExitRegion(ref region);
                }

                return enqueued;
            }

            public void Release()
                => Release(StateValues.GRANTED, StateValues.RELEASED, true);

            private bool Release(StateValues check_state, StateValues set_state, bool throw_on_fail)
            {
                RegionType p_region = RegionType.None, c_region = RegionType.None;
                LockObject parent = null, next = null;
                try
                {
                    TryEnterParentRegion(true, out p_region);
                    EnterRegion(true, out c_region);

                    parent = m_parent;
                    next = m_first ?? m_next ?? m_parent?.m_first;

                    if (UnsafeCheckValue(set_state, StateValues.STATE_MASK))
                        return true;

                    if (!UnsafeCheckValue(check_state, StateValues.STATE_MASK))
                    {
                        if (throw_on_fail)
                            throw new InvalidOperationException();
                        return false;
                    }

                    UnsafeSetValue(set_state, StateValues.STATE_MASK);

                    if (p_region != RegionType.None)
                    {
                        int downgrades = 0;
                        UnsafeRemove(true);
                        UnsafeGrant(next);

                        //if we are upgradeable and the parent is not, we will need to try to downgrade the parent (we pseudo ugpraded when we were appended)
                        if (parent.UnsafeCheckValue(StateValues.EXCLUSIVE, StateValues.MODE_MASK) && UnsafeCheckValue(StateValues.UPGRADEABLE, StateValues.UPGRADEABLE))
                            downgrades++;

                        //Of course, if we had an upgrade request, we need to downgrade, possibly twice if the early condition was filled
                        if (m_exclusive_requests > 0)
                            downgrades++;

                        parent.ExitRegion(ref p_region);

                        while (downgrades-- > 0)
                            Downgrade(parent, false);
                    }
                }
                finally
                {
                    ExitRegion(ref c_region);

                    if (p_region != RegionType.None)
                        parent.ExitRegion(ref p_region);
                }

                return true;
            }

            internal void SetExclusive()
            {
                RegionType region = RegionType.None;
                try
                {
                    EnterRegion(true, out region);
                    UnsafeSetValue(StateValues.EXCLUSIVE, StateValues.EXCLUSIVE);
                    m_self_exclusive_request = true;
                    m_exclusive_requests++;
                }
                finally
                {
                    ExitRegion(ref region);
                }
            }

            internal void SetUpgradeable()
            {
                RegionType region = RegionType.None;
                try
                {
                    EnterRegion(true, out region);
                    UnsafeSetValue(StateValues.UPGRADEABLE, StateValues.UPGRADEABLE);
                }
                finally
                {
                    ExitRegion(ref region);
                }
            }

            private bool TryEnterParentRegion(bool exclusive, out RegionType region)
            {
                LockObject parent = m_parent;
                region = default;
                while (parent != null && !parent.TryEnterRegionAsChild(this, exclusive, out region))
                    parent = m_parent;

                return parent != null;
            }

            private bool TryEnterRegionAsChild(LockObject child, bool exclusive, out RegionType region)
            {
                EnterRegion(exclusive, out region);
                if (child.m_parent == this)
                    return true;

                ExitRegion(ref region);
                return false;
            }

            private bool? UnsafeCheckGrant()
            {
                if (m_parent == null)
                    return true;

                if (UnsafeCheckExclusiveGrant())
                    return true;

                else if (UnsafeCheckSharedGrant())
                    return false;

                else
                    return null;
            }

            private bool UnsafeCheckExclusiveGrant()
            {
                //If there is a node before us, then there's no chance we'll be exclusive
                if (m_prev != null)
                    return false;

                //If the parent isn't exclusive, then we can't be either
                if (m_parent.UnsafeCheckValue(StateValues.UNSET, StateValues.EXCLUSIVE))
                    return false;

                //If the next node is granted, then by definition we can't be exclusive
                if (m_next == null || m_next.CheckValue(StateValues.UNSET, StateValues.GRANTED))
                    return true;

                //If we have any flags that lets be exclusive, then we can be exclusive.
                return !UnsafeCheckValue(StateValues.UNSET, StateValues.MODE_MASK);
            }

            private bool UnsafeCheckSharedGrant()
            {
                //Checks on the previous node.
                if (m_prev != null)
                {
                    //If the previous node is exclusive, we can't be granted
                    if (m_prev.CheckValue(StateValues.EXCLUSIVE, StateValues.EXCLUSIVE))
                        return false;

                    //If the previous node is upgradeable, and we are too, we can't be granted.
                    if (UnsafeCheckValue(StateValues.UPGRADEABLE, StateValues.UPGRADEABLE) && m_prev.CheckValue(StateValues.UPGRADEABLE, StateValues.UPGRADEABLE))
                        return false;

                    //If the previous node isn't granted, we can't be granted.
                    if (!m_prev.CheckValue(StateValues.GRANTED, StateValues.STATE_MASK))
                        return false;
                }

                //Shouldn't be possible for us to be a child node of a parent in any other state, but just in case...
                return m_parent.UnsafeCheckValue(StateValues.GRANTED, StateValues.STATE_MASK);
            }

            private bool UnsafeCheckValue(StateValues value, StateValues mask)
                => CheckValue(m_state, value, mask);

            private void UnsafeEnqueueContinuation()
            {
                ThreadPool.UnsafeQueueUserWorkItem(ExecuteContinuation, m_continuation);
                m_continuation = null;
            }

            /// <summary>
            /// Grants the current object.
            /// <br/>
            /// Requires being in the target's parent's object's shared or exclusive region for thread safety
            /// </summary>
            /// <param name="obj">The target to grant</param>
            private static void UnsafeGrant(LockObject obj)
            {
                RegionType region = default;
                try
                {
                    while (obj != null)
                    {
                        //Next in line is going to be the next object, unless overriden later
                        LockObject next = obj.m_next;

                        //We're already in the parent region, now we're locking the target
                        obj.EnterRegion(true, out region);

                        //Check what kind of grant we're allowed to do. True = exclusive, false = shared, null = none
                        bool? exclusive = obj.UnsafeCheckGrant();
                        if (!exclusive.HasValue)
                            return;

                        //Whether or not our grant code should continue after this iteration
                        bool propogate = false;

                        //If we have an exclusive grant, or the object isn't exclusive, we can grant it.
                        if (exclusive.Value || obj.UnsafeCheckValue(StateValues.UNSET, StateValues.EXCLUSIVE))
                        {
                            //If the object isn't granted yet, actually run the code.
                            if (obj.UnsafeCheckValue(StateValues.UNSET, StateValues.STATE_MASK))
                            {
                                LockObject prev = obj.m_prev;
                                
                                //If our previous is upgradeable, it needs to be at the end so that we can make sure only one granted at a time.
                                //Should only run if we downgraded an upgraded lock.
                                if (prev != null && prev.CheckValue(StateValues.UPGRADEABLE, StateValues.UPGRADEABLE))
                                {
                                    //Override what'll get the next grant, We don't need to grant the upgradeable lock.
                                    next = prev.m_next;

                                    //Remove the upgradable lock from the queue
                                    prev.UnsafeRemove(false);

                                    //Insert it after us, effectively swapping our places
                                    obj.UnsafeInsertObjectAfter(prev);
                                }

                                //Mark the object as granted
                                obj.UnsafeSetValue(StateValues.GRANTED, StateValues.STATE_MASK);

                                //Enqueue the delegate that we're supposed to run when we're granted. Part of the async pattern.
                                obj.UnsafeEnqueueContinuation();

                                //We actually granted/upgraded something, so we can continue to try to grant/upgrade things.
                                propogate = true;
                            }
                        }

                        //If we have an exclusive grant, and we're upgradeable, and our upgrade is requested, then we shall upgrade our lock object.
                        if (exclusive.Value && obj.UnsafeCheckValue(StateValues.UPGRADEABLE, StateValues.MODE_MASK) && obj.m_exclusive_requests > 0)
                        {
                            //Mark the object as exclusive
                            obj.UnsafeSetValue(StateValues.EXCLUSIVE, StateValues.EXCLUSIVE);

                            //Enqueue the delegate that we're supposed to run when we're upgraded. Part of the async pattern for upgrades
                            obj.UnsafeEnqueueContinuation();

                            //We actually granted/upgraded something, so we can continue to try to grant/upgrade things.
                            propogate = true;
                        }

                        //If we didn't do a thing, we don't keep trying to do things
                        if (!propogate)
                            return;

                        //Since we did do a thing, start with our firstborn
                        UnsafeGrant(obj.m_first);

                        //Release our region
                        obj.ExitRegion(ref region);

                        //So that we can go to the next.
                        obj = next;
                    }
                }
                finally
                {
                    if (region != RegionType.None)
                        obj.ExitRegion(ref region);
                }
            }

            /// <summary>
            /// Inserts an object after the current object.
            /// <br/>
            /// Requires being in the parent object's exclusive region for thread safety.
            /// </summary>
            /// <param name="obj">The object to insert after the current object</param>
            private void UnsafeInsertObjectAfter(LockObject obj)
            {
                obj.m_next = m_next;

                if (m_next != null)
                    m_next.m_prev = obj;

                obj.m_prev = this;
                m_next = obj;

                m_parent.UnsafeUpdateChainEnds(this, this, m_next);
            }

            /// <summary>
            /// Removes this object from the parent.
            /// <br/>
            /// Requires being in the parent object's exclusive region for thread safety.
            /// Requires being in the current object's shared/exclusive region for thread safety if final is true
            /// </summary>
            /// <param name="final">Whether or not this object will be removed from the tree permanently</param>
            private void UnsafeRemove(bool final)
            {
                LockObject parent = m_parent, prev = m_prev, next = m_next;
                m_prev = null;
                m_next = null;

                LockObject first, last;

                if (final)
                {
                    first = m_first;
                    last = m_last;

                    m_first = null;
                    m_last = null;
                    m_parent = null;
                    m_depth = 0;

                    LockObject current = first;
                    int depth = parent.m_depth + 1;
                    while (current != null)
                    {
                        current.m_parent = parent;
                        current.m_depth = depth;
                        if (current.m_exclusive_requests > 0)
                            parent.m_exclusive_requests++;
                        current = current.m_next;
                    }

                    if (first != null)
                        first.m_prev = prev;
                    else
                        first = next;

                    if (last != null)
                        last.m_next = next;
                    else
                        last = prev;
                }
                else
                {
                    first = next;
                    last = prev;
                }

                parent.UnsafeUpdateChainEnds(this, first, last);

                if (prev != null)
                    prev.m_next = first;

                if (next != null)
                    next.m_prev = last;
            }

            /// <summary>
            /// Sets/modifies the state value.
            /// 
            /// Requires being in the current object's exclusive region for thread safety.
            /// </summary>
            /// <param name="value"></param>
            /// <param name="mask"></param>
            private void UnsafeSetValue(StateValues value, StateValues mask)
                => m_state = (m_state & ~mask) | value;

            /// <summary>
            /// Updates <see cref="m_first"/> and <see cref="m_last"/> to reflect the current state of the chain.
            /// <br/>
            /// Requires being in the current object's exclusive region for thread safety.
            /// </summary>
            /// <param name="original">The value that the fields might contain</param>
            /// <param name="new_first">The value to update the first field to, if it contains the original</param>
            /// <param name="new_last">The value to update the last field to, if it contains the original</param>
            private void UnsafeUpdateChainEnds(LockObject original, LockObject new_first, LockObject new_last)
            {
                if (m_first == original)
                    m_first = new_first;

                if (m_last == original)
                    m_last = new_last;
            }

            /// <summary>
            /// Tries to upgrade the current node into an exclusive node, or at least register it for being upgraded.
            /// <br/>
            /// Requires being in the exclusive region of all nodes in the parent/child chain, up to the root.
            /// See <see cref="ChainEnterRegion(in Span{RegionProtect64.Region}, LockObject, bool)"/>.
            /// </summary>
            /// <returns></returns>
            private bool UnsafeTryUpgrade()
            {
                if (m_exclusive_requests > 0) //Is it marked as wanting an upgrade?
                {
                    m_exclusive_requests++;
                    return UnsafeCheckValue(StateValues.EXCLUSIVE, StateValues.EXCLUSIVE);
                }

                if (m_parent == null)
                {
                    m_exclusive_requests = 1;
                    UnsafeGrant(this);
                    return true;
                }

                bool pass_upgrade = m_parent.UnsafeTryUpgrade();
                pass_upgrade &= m_prev == null;

                m_exclusive_requests = 1; //Mark as wanting an upgrade, important we do it after trying to upgrade the parent.

                LockObject next = m_next;
                if (next != null && next.CheckValue(StateValues.UNSET | StateValues.GRANTED, StateValues.EXCLUSIVE | StateValues.STATE_MASK))
                {
                    LockObject check = next.m_next;
                    while (check != null && check.CheckValue(StateValues.UNSET | StateValues.GRANTED, StateValues.EXCLUSIVE | StateValues.STATE_MASK))
                    {
                        next = check;
                        check = check.m_next;
                    }

                    UnsafeRemove(false);
                    next.UnsafeInsertObjectAfter(this);
                    UnsafeGrant(m_parent.m_first);
                    pass_upgrade = false;
                }

                else if (pass_upgrade)
                    UnsafeGrant(this);

                return pass_upgrade;
            }

            public void Upgrade()
            {
                Span<RegionType> region_chain = stackalloc RegionType[m_depth + 1];
                try
                {
                    ChainEnterRegion(region_chain, null, true);

                    if (UnsafeCheckValue(StateValues.UNSET, StateValues.UPGRADEABLE))
                        throw new InvalidOperationException("This object is not upgradeable!");

                    if (m_self_exclusive_request)
                        return;

                    m_self_exclusive_request = true;

                    UnsafeTryUpgrade();
                }
                finally
                {
                    ChainExitRegion(this, region_chain);
                }
            }

            ref LockObject IStackable<LockObject>.Next => ref m_next;

            void IStackable<LockObject>.Reset(bool live)
            {
                if (live)
                {
                    RegionType region = RegionType.None;
                    try
                    {
                        EnterRegion(true, out region);
                        Init();
                    }
                    finally
                    {
                        ExitRegion(ref region);
                    }
                }
            }

            public void Dispose()
            {
                m_current_region.Dispose();
            }

            private enum RegionType
            {
                None, Created, Borrowed
            }
        }

        [Flags]
        private enum StateValues
        {
            UNSET = 0, 
            EXCLUSIVE = 1, UPGRADEABLE = 2, MODE_MASK = 3,
            GRANTED = 4, CANCELLED = 8, RELEASED = 12, STATE_MASK = 12
        }
    }
}

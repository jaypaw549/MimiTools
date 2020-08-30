using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MimiTools.Sync
{
    public struct RegionProtect64
    {
        /// <summary>
        /// The amount to increase by if we want to enter an exclusive region.
        /// It is in the lower bytes because we don't care if we overflow when requesting exclusive, it won't interfere with anything.
        /// </summary>
        private const int EINC = 0x1;

        /// <summary>
        /// The bytes of the ID that represents the exclusive region pool.
        /// </summary>
        private const uint EMASK = 0xFFFF;

        /// <summary>
        /// The amount to increase by if we want to enter any region,
        /// It is in the upper bytes because overflowing won't affect the lower bytes, and therefore we don't need to worry about overflowing.
        /// </summary>
        private const int SINC = 0x10000;

        /// <summary>
        /// The bytes of the ID that represents the shared region pool. An exclusive lock still needs an entry from the shared pool.
        /// </summary>
        private const uint SMASK = 0xFFFF0000;

        /// <summary>
        /// The current active ID, this variable lets individual regions know if they should execute.
        /// </summary>
        private volatile int current;

        /// <summary>
        /// The current ID that's up for grabs.
        /// </summary>
        private volatile int free;

        public Region EnterExclusiveRegion()
            => EnterRegionInternal(SINC + EINC, EMASK | SMASK);

        public Region EnterSharedRegion()
            => EnterRegionInternal(SINC, EMASK);

        public Region EnterRegion(bool exclusive)
            => EnterRegionInternal(exclusive ? SINC + EINC : SINC, exclusive ? EMASK | SMASK : EMASK);

        private Region EnterRegionInternal(int inc, uint mask)
        {
            //Create our new ID, which will let us know when we can run.
            uint id = (uint) (Interlocked.Add(ref free, inc) - inc);

            //Spin wait object
            SpinWait wait = new SpinWait();

            //If the parts we care about aren't equal, spin.
            while ((((uint) current ^ id) & mask) != 0)
                wait.SpinOnce();

            return new Region(inc);
        }

        public void ExitRegion(Region region)
            => ExitRegion(region.data);

        public void ExitRegion(ref Region region)
        {
            ExitRegion(region.data);
            region = new Region(); //Erase the current region, so that it can't be used to release multiple times
        }

        internal void ExitRegion(bool exclusive)
            => ExitRegion(SINC + (exclusive ? EINC : 0));

        private void ExitRegion(int inc)
        {
            Interlocked.Add(ref current, inc);
        }

        public readonly struct Region
        {
            public bool IsActive => data != 0;

            public bool IsExclusive => data == SINC + EINC;

            internal Region(int data)
            {
                this.data = data;
            }

            internal readonly int data;
        }
    }
}

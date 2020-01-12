using System;

namespace MimiTools.Memory
{
    public readonly unsafe struct CopyDetect
    {
        private readonly CopyDetect* m_addr;

        public bool IsOriginal
        {
            get
            {
                fixed (CopyDetect* ptr = &this)
                    return m_addr == ptr;
            }
        }

        public bool IsOriginalValid => m_addr->m_addr == m_addr;

        public long Offset
        {
            get
            {
                fixed (CopyDetect* ptr = &this)
                    return ptr - m_addr;
            }
        }

        public ref readonly CopyDetect Original
        {
            get
            {
                if (IsOriginalValid)
                    return ref *m_addr;

                throw new InvalidOperationException("The original is no longer valid!");
            }
        }

        private CopyDetect(CopyDetect* addr)
            => m_addr = addr;

        public bool TryGetOriginal(out CopyDetect* target)
        {
            target = m_addr;
            if (IsOriginalValid)
                return true;

            target = null;
            return false;
        }

        public static void SetOriginal(out CopyDetect target)
        {
            fixed (CopyDetect* addr = &target)
                target = new CopyDetect(addr);
        }
    }
}

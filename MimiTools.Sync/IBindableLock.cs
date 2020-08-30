using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Sync
{
    public interface IBindableLock : ILock
    {
        public void Bind();

        //public void Unbind();
    }
}

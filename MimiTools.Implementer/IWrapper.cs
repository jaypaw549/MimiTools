using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Implementer
{
    public interface IWrapper<T>
    {
         T Target { get; }
    }
}

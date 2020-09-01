using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Implementer
{
    public interface ITypeWrapper<T>
    {
         T Target { get; }
    }
}

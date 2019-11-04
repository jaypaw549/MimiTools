using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data
{
    public static class PointerTools
    {
        /// <summary>
        /// increases the pointer by an offset multiplied by its own size (LongPtr would be 8, ShortPtr 2)
        /// </summary>
        /// <typeparam name="TPointer">The pointer type to add to</typeparam>
        /// <param name="ptr">The pointer to add to</param>
        /// <param name="offset">how much to increase it by</param>
        /// <returns>A new pointer containing the new value</returns>
        public static TPointer AddPointerOffset<TPointer>(this TPointer ptr, int offset) where TPointer : unmanaged, IPointer<TPointer>
            => ptr.Add(ptr.PtrSize * offset);

        /// <summary>
        /// increases the pointer by an offset multiplied by its own size (LongPtr would be 8, ShortPtr 2)
        /// </summary>
        /// <typeparam name="TPointer">The pointer type to add to</typeparam>
        /// <param name="ptr">The pointer to add to</param>
        /// <param name="offset">how much to increase it by</param>
        /// <returns>A new pointer containing the new value</returns>
        public static TPointer AddPointerOffset<TPointer>(this TPointer ptr, long offset) where TPointer : unmanaged, IPointer<TPointer>
            => ptr.Add(ptr.PtrSize * offset);

        /// <summary>
        /// Creates a pointer from the specified address
        /// </summary>
        /// <typeparam name="TPointer">The pointer type to create</typeparam>
        /// <param name="value">The address in int form</param>
        /// <returns>A new pointer pointing to the specified address</returns>
        public static TPointer FromAddr<TPointer>(int value) where TPointer : unmanaged, IPointer<TPointer>
            => default(TPointer).Add(value);

        /// <summary>
        /// Creates a pointer from the specified address
        /// </summary>
        /// <typeparam name="TPointer">The pointer type to create</typeparam>
        /// <param name="value">The address in long form</param>
        /// <returns>A new pointer pointing to the specified address</returns>
        public static TPointer FromAddr<TPointer>(long value) where TPointer : unmanaged, IPointer<TPointer>
            => default(TPointer).Add(value);

        public static TPointer FromOffset<TPointer>(int value) where TPointer : unmanaged, IPointer<TPointer>
            => default(TPointer).AddPointerOffset(value);

        public static TPointer FromOffset<TPointer>(long value) where TPointer : unmanaged, IPointer<TPointer>
            => default(TPointer).AddPointerOffset(value);

        /// <summary>
        /// Gets the size of the pointer type
        /// </summary>
        /// <typeparam name="TPointer">The pointer type to get the size of</typeparam>
        /// <returns>the size of the pointer type</returns>
        public static int Size<TPointer>() where TPointer : unmanaged, IPointer<TPointer>
            => default(TPointer).PtrSize;

        public static TPointer SubtractPointerOffset<TPointer>(this TPointer ptr, int offset) where TPointer : unmanaged, IPointer<TPointer>
            => ptr.Subtract(ptr.PtrSize * offset);

        public static TPointer SubtractPointerOffset<TPointer>(this TPointer ptr, long offset) where TPointer : unmanaged, IPointer<TPointer>
            => ptr.Subtract(ptr.PtrSize * offset);

        /// <summary>
        /// Creates a pointer that points to zero
        /// </summary>
        /// <typeparam name="TPointer">The pointer type to create</typeparam>
        /// <returns>a new pointer pointing to zero</returns>
        public static TPointer Zero<TPointer>() where TPointer : unmanaged, IPointer<TPointer>
            => default;
    }
}

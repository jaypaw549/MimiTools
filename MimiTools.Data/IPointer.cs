using System;

namespace MimiTools.Data
{
    public interface IPointer<TPointer> where TPointer : unmanaged, IPointer<TPointer>
    {
        /// <summary>
        /// Returns itself, acts as an unboxing operation
        /// </summary>
        TPointer AsPointer { get; }

        /// <summary>
        /// Returns a 32-bit representation of this pointer.
        /// </summary>
        int IntValue { get; }

        /// <summary>
        /// Returns a 64-bit representation of this pointer
        /// </summary>
        long LongValue { get; }
        
        /// <summary>
        /// Returns the size (in-memory) of this pointer
        /// </summary>
        int PtrSize { get; }

        /// <summary>
        /// Adds two pointers together, simple right?
        /// </summary>
        /// <param name="other">The other pointer to add to this one</param>
        /// <returns>A new pointer of the two values added together</returns>
        TPointer Add(TPointer other);

        /// <summary>
        /// Offsets this pointer by the specified number of bytes
        /// </summary>
        /// <param name="offset">the number of bytes to offset this pointer as</param>
        /// <returns>the new pointer offset by the specified number of bytes</returns>
        TPointer Add(int offset);

        /// <summary>
        /// Offsets this pointer by the specified number of bytes
        /// </summary>
        /// <param name="offset">the number of bytes to offset this pointer as</param>
        /// <returns>the new pointer offset by the specified number of bytes</returns>
        TPointer Add(long offset);

        /// <summary>
        /// Returns a new pointer decreased by the pointer size
        /// </summary>
        /// <returns>a new pointer</returns>
        TPointer Decrement();

        /// <summary>
        /// Returns a new pointer increased by the pointer size
        /// </summary>
        /// <returns>a new pointer</returns>
        TPointer Increment();

        /// <summary>
        /// Multiplies the pointer by the specified amount
        /// </summary>
        /// <param name="multiplier">the multiplier</param>
        /// <returns>a new pointer with the multiplier applied</returns>
        TPointer Multiply(int multiplier);

        /// <summary>
        /// Multiplies the pointer by the specified amount
        /// </summary>
        /// <param name="multiplier">the multiplier</param>
        /// <returns>a new pointer with the multiplier applied</returns>
        TPointer Multiply(long multiplier);

        /// <summary>
        /// Subtracts one pointer from another
        /// </summary>
        /// <param name="other">The pointer to subtract from this</param>
        /// <returns>a new pointer with the operation applied</returns>
        TPointer Subtract(TPointer other);

        /// <summary>
        /// Subtracts a specified amount (in bytes) from this pointer
        /// </summary>
        /// <param name="offset">the amount to move this pointer back by</param>
        /// <returns>a new pointer moved back by the specified amount</returns>
        TPointer Subtract(int offset);

        /// <summary>
        /// Subtracts a specified amount (in bytes) from this pointer
        /// </summary>
        /// <param name="offset">the amount to move this pointer back by</param>
        /// <returns>a new pointer moved back by the specified amount</returns>
        TPointer Subtract(long offset);
    }
}
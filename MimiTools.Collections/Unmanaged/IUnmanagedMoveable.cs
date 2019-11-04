namespace MimiTools.Collections.Unmanaged
{
    public interface IUnmanagedMoveable<T> where T : unmanaged
    {
        /// <summary>
        /// Whether or not to consider this struct as a valid member of a collection
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Called when all items in a collection are moved, order and relative offsets are preserved.
        /// </summary>
        /// <param name="offset_in_bytes">How far it was moved, in bytes</param>
        unsafe void MassMoved(long offset_in_bytes);

        /// <summary>
        /// Called when the item is moved within the collection
        /// </summary>
        /// <param name="before">The position it was at before moving</param>
        /// <param name="after">The position it is at now</param>
        unsafe void Moved(T* before, T* after);
    }
}

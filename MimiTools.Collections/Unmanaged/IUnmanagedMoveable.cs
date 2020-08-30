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
        /// Called after the item is moved within the collection
        /// </summary>
        /// <param name="src">The position it was at before moving</param>
        unsafe void OnMoved(T* src);

        /// <summary>
        /// Called when the item is about to be moved within the collection
        /// </summary>
        /// <param name="before">The position it is being moved to</param>
        unsafe void OnMoving(T* dst);
    }
}

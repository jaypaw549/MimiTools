namespace MimiTools.Collections.Unmanaged
{
    public interface IUnmanagedHashable<T> where T : unmanaged
    {
        int Hash { get; }
        unsafe T* Next { get; set; }
    }
}

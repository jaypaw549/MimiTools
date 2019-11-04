namespace MimiTools.Collections.Unmanaged
{
    public interface IUnmanagedHashItem<T> where T : unmanaged
    {
        int Hash { get; }
        unsafe T* Next { get; set; }
    }
}

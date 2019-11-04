using System.Collections.Generic;

namespace MimiTools.Collections
{
    public class TouchableHeap<T> : Heap<T>
    {
        private readonly Dictionary<T, int> IndexMap = new Dictionary<T, int>();

        public TouchableHeap(IComparer<T> comparer) : base(comparer)
        {
            Cleared += Wipe;
            ItemAdded += Map;
            ItemRemoved += Unmap;
            SwapPerformed += Remap;
        }

        protected override int GetIndex(T item)
        {
            if (IndexMap.TryGetValue(item, out int index))
                return index;
            return -1;
        }

        private void Map(T obj)
        {
            if (!IndexMap.ContainsKey(obj))
                IndexMap[obj] = base.GetIndex(obj);
        }

        private void Remap(int x, int y)
        {
            T obj_x = Data[x];
            T obj_y = Data[y];
            IndexMap[obj_x] = x;
            IndexMap[obj_y] = y;
        }

        public void Touch(T item)
        {
            if (!IndexMap.ContainsKey(item))
                return;

            int index = GetIndex(item);

            if (Swim(index) == index)
                Sink(index);
        }

        private void Unmap(T obj)
        {
            int index = base.GetIndex(obj);
            if (index == -1)
                IndexMap.Remove(obj);
            else
                IndexMap[obj] = index;
        }

        private void Wipe()
            => IndexMap.Clear();
    }
}

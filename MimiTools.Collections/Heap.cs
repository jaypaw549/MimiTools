using MimiTools.Extensions.IComparer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MimiTools.Collections
{
    public static class Heap
    {
        public static Heap<T> CreateMaxHeap<T>()
            => Heap<T>.CreateHeap(Comparer<T>.Default);

        public static Heap<T> CreateMaxHeap<T>(IComparer<T> comparer)
            => Heap<T>.CreateHeap(comparer);

        public static Heap<T> CreateMaxHeap<T>(ICollection<T> collection)
            => Heap<T>.CreateHeap(collection, Comparer<T>.Default);

        public static Heap<T> CreateMaxHeap<T>(ICollection<T> collection, IComparer<T> comparer)
            => Heap<T>.CreateHeap(collection, comparer);

        public static Heap<T> CreateMinHeap<T>()
            => Heap<T>.CreateHeap(Comparer<T>.Default.Invert());

        public static Heap<T> CreateMinHeap<T>(IComparer<T> comparer)
            => Heap<T>.CreateHeap(comparer.Invert());

        public static Heap<T> CreateMinHeap<T>(ICollection<T> collection)
            => Heap<T>.CreateHeap(collection, Comparer<T>.Default.Invert());

        public static Heap<T> CreateMinHeap<T>(ICollection<T> collection, IComparer<T> comparer)
            => Heap<T>.CreateHeap(collection, comparer.Invert());
    }

    public static class HeapExtensions
    {
        public static Heap<T> ToMaxHeap<T>(this ICollection<T> collection)
            => Heap.CreateMaxHeap(collection);

        public static Heap<T> ToMaxHeap<T>(this ICollection<T> collection, IComparer<T> comparer)
            => Heap.CreateMaxHeap(collection, comparer);

        public static Heap<T> ToMaxHeap<T>(this IEnumerable<T> enumerable)
            => CopyToHeap(enumerable, Heap.CreateMaxHeap<T>());

        public static Heap<T> ToMaxHeap<T>(this IEnumerable<T> enumerable, IComparer<T> comparer)
            => CopyToHeap(enumerable, Heap.CreateMaxHeap(comparer));

        public static Heap<T> ToMinHeap<T>(this ICollection<T> collection)
            => Heap.CreateMinHeap(collection);

        public static Heap<T> ToMinHeap<T>(this ICollection<T> collection, IComparer<T> comparer)
            => Heap.CreateMinHeap(collection, comparer);

        public static Heap<T> ToMinHeap<T>(this IEnumerable<T> enumerable)
            => CopyToHeap(enumerable, Heap.CreateMinHeap<T>());

        public static Heap<T> ToMinHeap<T>(this IEnumerable<T> enumerable, IComparer<T> comparer)
            => CopyToHeap(enumerable, Heap.CreateMinHeap(comparer));

        private static Heap<T> CopyToHeap<T>(IEnumerable<T> enumerable, Heap<T> heap)
        {
            foreach (T item in enumerable)
                heap.Add(item);
            return heap;
        }
    }

    public class Heap<T> : ICollection<T>, ICloneable
    {
        internal static Heap<T> CreateHeap(IComparer<T> comparer)
            => new Heap<T>(comparer);

        internal static Heap<T> CreateHeap(ICollection<T> collection, IComparer<T> comparer)
            => new Heap<T>(collection, comparer);

        protected Heap(IComparer<T> comparer)
        {
            Backend = new T[3];
            Count = 0;
            Comparer = comparer;
            Data = new ReadOnlyCollection<T>(Backend);
        }

        protected Heap(ICollection<T> collection, IComparer<T> comparer)
        {
            Count = collection.Count;
            Backend = new T[Count * 2];
            collection.CopyTo(Backend, 0);
            Comparer = comparer;
            Data = new ReadOnlyCollection<T>(Backend);

            //There's a couple of ways to do this, but Swim is faster due to only having to look up one node at a time, not two
            for (int i = 1; i < Count; i++)
                Swim(i);
        }

        protected event Action Cleared;
        protected event Action<T> ItemAdded;
        protected event Action<T> ItemRemoved;
        protected event Action<int, int> SwapPerformed;

        private Heap(Heap<T> heap)
        {
            Count = heap.Count;
            Backend = Backend.Clone() as T[];
            Comparer = heap.Comparer;
            Data = new ReadOnlyCollection<T>(Backend);
        }

        public int Count { get; private set; }

        bool ICollection<T>.IsReadOnly => false;

        private T[] Backend;

        protected IReadOnlyList<T> Data { get; private set; }

        protected readonly IComparer<T> Comparer;

        public void Add(T item)
        {
            int node = Count++;

            Backend[node] = item;

            node = Swim(node);

            if (Count == Backend.Length)
            {
                T[] container = new T[Backend.Length * 2];
                Backend.CopyTo(container, 0);
                Backend = container;
                Data = new ReadOnlyCollection<T>(Backend);
            }

            ItemAdded?.Invoke(item);
        }

        public void Clear()
        {
            Backend = new T[3];
            Cleared?.Invoke();
        }

        public virtual object Clone()
            => new Heap<T>(this);

        public bool Contains(T item)
            => GetIndex(item) != -1;

        public void CopyTo(T[] array, int arrayIndex)
            => System.Array.Copy(Backend, 0, array, arrayIndex, Count);

        public virtual IEnumerator<T> GetEnumerator()
            => Backend.Take(Count).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        protected virtual int GetIndex(T item)
        {
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int node = queue.Dequeue();

                if (node >= Count)
                    continue;

                int result = Comparer.Compare(Backend[node], item);

                if (result == 0) // if they're equal, we have our value
                    return node;

                else if (result < 0) // if our node was less, then we're not going to find it in any of the child nodes, so just continue
                    continue;

                node = FirstChildNode(node);
                if (node < Count)
                    queue.Enqueue(node);

                if (++node < Count)
                    queue.Enqueue(node);
            }
            return -1;
        }

        public T Peek()
        {
            if (Count <= 0)
                throw new InvalidOperationException("Heap is empty!");
            return Backend[0];
        }

        public bool Remove(T item)
            => RemoveIndex(GetIndex(item));

        protected bool RemoveIndex(int index)
        {
            if (index < 0 || index >= Count)
                return false;

            T item = Backend[index];

            Backend[index] = Backend[--Count];
            Backend[Count] = default;

            Sink(index);

            if (Count <= Backend.Length / 4)
            {
                T[] storage = new T[Backend.Length / 2];
                Array.Copy(Backend, storage, Count);
                Backend = storage;
                Data = new ReadOnlyCollection<T>(Backend);
            }

            ItemRemoved?.Invoke(item);

            return true;
        }

        public T RemovePriority()
        {
            T result = Backend[0];

            if (!RemoveIndex(0))
                throw new InvalidOperationException("Heap is empty!");

            return result;
        }

        protected int Sink(int node)
        {
            while (true)
            {
                int left = FirstChildNode(node);
                int right = left + 1;
                int compared;

                if (right < Count)
                    compared = Comparer.Compare(Backend[left], Backend[right]) < 0 ? right : left;
                else if (left < Count)
                    compared = left;
                else
                    break;

                if (Comparer.Compare(Backend[compared], Backend[node]) <= 0)
                    break;

                Swap(node, compared);

                node = compared;
            }
            return node;
        }

        private void Swap(int i, int j)
        {
            T value = Backend[i];
            Backend[i] = Backend[j];
            Backend[j] = value;
            SwapPerformed?.Invoke(i, j);
        }

        protected int Swim(int node)
        {
            int parent = ParentNode(node);
            while (parent != -1 && Comparer.Compare(Backend[node], Backend[parent]) > 0)
            {
                Swap(node, parent);

                node = parent;
                parent = ParentNode(node);
            }
            return node;
        }

        protected static int FirstChildNode(int node)
            => (node * 2) + 1;

        protected static int ParentNode(int node)
            => ((node + 1) / 2) - 1;
    }
}

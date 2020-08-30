using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Tools
{
    public struct MarkableEnumerator<TEnumerator, T> : IMarkableEnumerator<T> where TEnumerator : IEnumerator<T>
    {
        public MarkableEnumerator(TEnumerator enumerator)
        {
            backlog = 1;
            this.enumerator = enumerator;
            queue = new Queue<Maybe<T>>();

            //Capture the initial state
            CaptureCurrent();
        }

        public readonly T Current => backlog > 0 ? PeekOrThrow() : enumerator.Current;

        readonly object IEnumerator.Current => Current;

        private int backlog;


        //Don't make readonly in case this is a modifiable struct
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "This may be a struct, therefore we will try to prevent defensive copies")]
        private TEnumerator enumerator;

        private readonly Queue<Maybe<T>> queue;

        private readonly void CaptureCurrent()
        {
            try
            {
                queue.Enqueue(enumerator.Current);
            }
            catch (Exception e)
            {
                queue.Enqueue(new Maybe<T>(e));
            }
        }

        public void Dispose()
        {
            enumerator.Dispose();
            queue.Clear();
        }

        public readonly void Mark()
        {
            if (backlog == queue.Count)
                return;

            if (backlog > 0)
            {
                int count = queue.Count;
                for (int i = 0; i < backlog; i++)
                    queue.Enqueue(queue.Dequeue());

                for (int i = backlog; i < count; i++)
                    queue.Dequeue();
            }
            else
                queue.Clear();
        }

        public bool MoveNext()
        {
            if (backlog > 0)
            {
                queue.Enqueue(queue.Dequeue());
                if (--backlog > 0)
                    return true;
            }

            if (enumerator.MoveNext())
            {
                CaptureCurrent();
                return true;
            }
            return false;
        }

        private readonly T PeekOrThrow()
        {
            Maybe<T> peek = queue.Peek();
            if (peek.IsMaybe)
                return peek.MaybeValue;

            throw (Exception)peek.MaybeNotValue;
        }

        public void Reset()
        {
            enumerator.Reset();
            queue.Clear();
        }

        public void Rewind()
        {
            //Cycle out the current backlog to restore order.
            for (int i = 0; i < backlog; i++)
                queue.Enqueue(queue.Dequeue());

            //mark all items in the queue as backlog
            backlog = queue.Count;
        }
    }

    public interface IMarkableEnumerator<T> : IEnumerator<T>
    {
        public void Mark();

        public void Rewind();
    }
}

using System;
using System.Collections.Generic;

public class PriorityQueue<T>
{
    private readonly List<Entry> heap = new List<Entry>();

    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add(new Entry(item, priority));
        SiftUp(heap.Count - 1);
    }

    public T Dequeue()
    {
        if (heap.Count == 0) throw new InvalidOperationException("Priority queue is empty.");

        T item = heap[0].Item;
        int last = heap.Count - 1;
        heap[0] = heap[last];
        heap.RemoveAt(last);
        if (heap.Count > 0) SiftDown(0);

        return item;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (heap[parent].Priority <= heap[index].Priority) return;

            Swap(parent, index);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            int left = index * 2 + 1;
            int right = left + 1;
            int smallest = index;

            if (left < heap.Count && heap[left].Priority < heap[smallest].Priority) smallest = left;
            if (right < heap.Count && heap[right].Priority < heap[smallest].Priority) smallest = right;
            if (smallest == index) break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (heap[a], heap[b]) = (heap[b], heap[a]);
    }

    private readonly struct Entry
    {
        public readonly T Item;
        public readonly float Priority;

        public Entry(T item, float priority)
        {
            Item = item;
            Priority = priority;
        }
    }
}

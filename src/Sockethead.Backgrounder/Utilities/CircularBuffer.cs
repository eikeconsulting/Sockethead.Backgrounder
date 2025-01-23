using System.Collections;

namespace Sockethead.Backgrounder.Utilities;

public class CircularBuffer<T> : IReadOnlyList<T>
{
    private readonly List<T> _buffer = [];
    private int _head;

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        
        Capacity = capacity;
    }

    public int Capacity { get; }

    public int Count => _buffer.Count;

    public void Add(T item)
    {
        if (_buffer.Count < Capacity)
        {
            // Add directly if the buffer has space
            _buffer.Add(item);
        }
        else
        {
            // Overwrite the oldest element when full
            _buffer[_head] = item;
        }

        // Update the head to point to the next insertion point
        _head = (_head + 1) % Capacity;
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

            // Map logical index to physical index
            int physicalIndex = (_head - Count + index + Capacity) % Capacity;
            return _buffer[physicalIndex];
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        return string.Join(", ", this);
    }
}
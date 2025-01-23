namespace Sockethead.Backgrounder.Utilities;

public class BlockingCircularBuffer<T>(int capacity)
{
    private CircularBuffer<T> Buffer = new(capacity);
    
    private readonly object Mutex = new();

    public void Add(T t)
    {
        lock (Mutex)
            Buffer.Add(t);
    }
    
    public IReadOnlyList<T> Get()
    {
        lock (Mutex)
            return Buffer.ToArray();        
    }

    public T? Find(Func<T, bool> predicate)
    {
        lock (Mutex)
            return Buffer.FirstOrDefault(predicate);
    }

    public bool Update(Func<T, bool> predicate, Action<T> update)
    {
        lock (Mutex)
        {
            T? item = Buffer.FirstOrDefault(predicate);
            if (item is null)
                return false;
            update(item);
            return true;
        }
    }
    
    public void Clear()
    {
        lock (Mutex)
            Buffer = new CircularBuffer<T>(capacity);
    }    
}
namespace ContractCheck.Samples.Stack;

public class NonNullStack<T>(int capacity) : INonNullStack<T>
    where T : class
{
    protected T[] Items { get; private set; } = new T[Math.Max(0, capacity)];

    private int nextFree;

    public bool IsEmpty => nextFree == 0;

    public int Count => nextFree;

    public void Push(T value)
    {
        if (nextFree == Items.Length)
        {
            var grown = new T[Items.Length * 2 + 1];
            Array.Copy(Items, grown, nextFree);
            Items = grown;
        }

        Items[nextFree++] = value;
    }

    public T Pop()
    {
        T value = Items[--nextFree];
        Items[nextFree] = null!;
        return value;
    }
}

public sealed class NonNullStackWithUpdates<T>(int capacity) : NonNullStack<T>(capacity), INonNullStackWithUpdates<T>
    where T : class
{
    public void UpdateAt(int index, T value) => Items[index] = value;
}

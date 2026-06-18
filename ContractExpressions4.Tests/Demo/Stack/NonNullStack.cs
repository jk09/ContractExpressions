namespace ContractExpressions4.Tests.Demo.Stack;

internal class NonNullStack(int len) : INonNullStack
{
    private string[] arr = new string[len >= 0 ? len : 0];
    private int nextFree;

    public bool IsEmpty => nextFree == 0;
    public int Count => nextFree;

    public void Push(string value)
    {
        if (nextFree == arr.Length)
        {
            string[] newArr = new string[arr.Length * 2 + 1];
            for (int i = 0; i < nextFree; i++)
            {
                newArr[i] = arr[i];
            }

            arr = newArr;
        }

        arr[nextFree++] = value;
    }

    public string Pop()
    {
        string value = arr[--nextFree];
        arr[nextFree] = null!;
        return value;
    }
}

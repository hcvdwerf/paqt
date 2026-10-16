namespace Collections;

public static class CollectionPartitioner
{
    public static IReadOnlyList<IReadOnlyList<T>> Partition<T>(IEnumerable<T> values, int size)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "Groepsgrootte moet een positief geheel getal zijn.");
        return values.Chunk(size).Select(chunk => (IReadOnlyList<T>)chunk).ToArray();
    }
}

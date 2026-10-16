namespace Collections.Tests;

public class PartitionTests
{
    [Theory]
    [InlineData(1, 10)]
    [InlineData(3, 4)]
    [InlineData(20, 1)]
    public void Preserves_values_and_input(int size, int count)
    {
        var input = Enumerable.Range(1, 10).ToList();
        var groups = CollectionPartitioner.Partition(input, size);
        Assert.Equal(count, groups.Count);
        Assert.All(groups, g => Assert.InRange(g.Count, 1, size));
        Assert.Equal(Enumerable.Range(1, 10), groups.SelectMany(g => g));
        Assert.Equal(Enumerable.Range(1, 10), input);
        if (size == 3) Assert.Equal([10], groups[^1]);
    }

    [Fact]
    public void Empty_input_has_no_groups() => Assert.Empty(CollectionPartitioner.Partition(Array.Empty<int>(), 3));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_invalid_size(int size) => Assert.Throws<ArgumentOutOfRangeException>(() => CollectionPartitioner.Partition(new[] { 1 }, size));
}

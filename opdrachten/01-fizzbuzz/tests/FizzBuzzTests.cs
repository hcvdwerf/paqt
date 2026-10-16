namespace FizzBuzz.Tests;

public class FizzBuzzTests
{
    [Fact]
    public void Preserves_order_and_all_replacements() => Assert.Equal(
        ["1", "2", "Fizz", "4", "Buzz", "Fizz", "7", "8", "Fizz", "Buzz", "11", "Fizz", "13", "14", "FizzBuzz"],
        FizzBuzzSequence.Generate(1, 15));

    [Fact]
    public void Supports_int_boundary() => Assert.Equal([int.MaxValue.ToString()], FizzBuzzSequence.Generate(int.MaxValue, int.MaxValue));

    [Fact]
    public void Rejects_reversed_range() => Assert.Throws<ArgumentException>(() => FizzBuzzSequence.Generate(2, 1));
}

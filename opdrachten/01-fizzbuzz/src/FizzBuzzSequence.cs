using System.Globalization;

namespace FizzBuzz;

public static class FizzBuzzSequence
{
    public static IEnumerable<string> Generate(int start, int end)
    {
        if (end < start) throw new ArgumentException("Eindwaarde ligt voor startwaarde.");
        return Enumerate();

        IEnumerable<string> Enumerate()
        {
            // Een long als teller voorkomt overflow bij int.MaxValue.
            for (long number = start; number <= end; number++)
                yield return (number % 3, number % 5) switch
                {
                    (0, 0) => "FizzBuzz",
                    (0, _) => "Fizz",
                    (_, 0) => "Buzz",
                    _ => number.ToString(CultureInfo.InvariantCulture)
                };
        }
    }
}

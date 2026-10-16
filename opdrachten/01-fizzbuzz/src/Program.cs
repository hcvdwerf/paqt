using System.Globalization;
using FizzBuzz;

try
{
    if (args.Length != 0 && args.Length != 2) throw new ArgumentException("Gebruik: FizzBuzz [start einde]");
    var start = args.Length == 0 ? 1 : int.Parse(args[0], CultureInfo.InvariantCulture);
    var end = args.Length == 0 ? 100 : int.Parse(args[1], CultureInfo.InvariantCulture);
    foreach (var value in FizzBuzzSequence.Generate(start, end)) Console.WriteLine(value);
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

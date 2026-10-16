using System.Globalization;
using FullWeeks;

try
{
    if (args.Length != 2) throw new ArgumentException("Gebruik: FullWeeks startdatum einddatum (yyyy-MM-dd)");
    var start = DateOnly.ParseExact(args[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
    var end = DateOnly.ParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture);
    foreach (var monday in FullWeekCalculator.Mondays(start, end)) Console.WriteLine(monday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or FormatException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

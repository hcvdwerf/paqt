using System.Globalization;
using System.Text.Json;
using Collections;

try
{
    if (args.Length != 2) throw new ArgumentException("Gebruik: Collections '1,2,3,4' groepsgrootte");
    var numbers = string.IsNullOrWhiteSpace(args[0]) ? [] : args[0].Split(',').Select(n => decimal.Parse(n, CultureInfo.InvariantCulture)).ToArray();
    Console.WriteLine(JsonSerializer.Serialize(CollectionPartitioner.Partition(numbers, int.Parse(args[1], CultureInfo.InvariantCulture))));
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

namespace FullWeeks;

public static class FullWeekCalculator
{
    public static IReadOnlyList<DateOnly> Mondays(DateOnly start, DateOnly end)
    {
        if (end < start) throw new ArgumentException("Einddatum ligt voor startdatum.");
        var result = new List<DateOnly>();
        var daysToMonday = (8 - (int)start.DayOfWeek) % 7;
        // Rekenen met dagnummers voorkomt overflow aan de grenzen van DateOnly.
        for (var day = start.DayNumber + daysToMonday; day + 6 <= end.DayNumber; day += 7)
            result.Add(DateOnly.FromDayNumber(day));
        return result;
    }
}

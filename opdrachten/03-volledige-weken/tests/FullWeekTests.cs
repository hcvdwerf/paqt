namespace FullWeeks.Tests;

public class FullWeekTests
{
    [Theory]
    [InlineData("2026-10-12", "2026-10-18", "2026-10-12")]
    [InlineData("2026-10-13", "2026-10-25", "2026-10-19")]
    [InlineData("2026-10-12", "2026-10-24", "2026-10-12")]
    [InlineData("2025-12-29", "2026-01-04", "2025-12-29")]
    [InlineData("2024-02-26", "2024-03-03", "2024-02-26")]
    public void Returns_only_complete_weeks(string start, string end, string expected) =>
        Assert.Equal([DateOnly.Parse(expected)], FullWeekCalculator.Mondays(DateOnly.Parse(start), DateOnly.Parse(end)));

    [Fact]
    public void Partial_week_has_no_result() => Assert.Empty(FullWeekCalculator.Mondays(new(2026, 10, 13), new(2026, 10, 18)));

    [Fact]
    public void Maximum_date_does_not_overflow() => Assert.Empty(FullWeekCalculator.Mondays(DateOnly.MaxValue, DateOnly.MaxValue));

    [Fact]
    public void Rejects_reversed_range() => Assert.Throws<ArgumentException>(() => FullWeekCalculator.Mondays(new(2026, 2, 1), new(2026, 1, 1)));

    [Fact]
    public void Returns_chronological_mondays() => Assert.Equal(
        new[] { new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 19) },
        FullWeekCalculator.Mondays(new(2026, 10, 1), new(2026, 10, 25)));
}

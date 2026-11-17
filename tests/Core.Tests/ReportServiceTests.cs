using Core.Reports;

namespace Core.Tests;

// Звіти лаб 7 — чисті функції: на вході фіксований список, на виході передбачуваний результат.
public class ReportServiceTests
{
    [Theory] // 6) звіт на кількох наборах параметрів
    [InlineData(10, 2)]  // 3 і 7; 10 не менше за 10
    [InlineData(11, 3)]  // межа: SKU-EDGE = 10 уже потрапляє
    [InlineData(1, 0)]
    public void LowStock_CountsOnlyBelowThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.LowStock(TestData.Items(), threshold).Count);

    [Fact] // 7) детермінований топ-N: SKU-MID і SKU-MIX мають по 50, перемагає менший SKU
    public void TopByQuantity_IsDeterministic() =>
        Assert.Equal(new[] { "SKU-BIG", "SKU-MID" },
                     ReportService.TopByQuantity(TestData.Items(), 2).Select(l => l.Sku));

    [Fact] // 8) агрегати на порожньому джерелі не кидають InvalidOperationException
    public void Totals_OnEmptySource_ReturnsZeros() =>
        Assert.Equal((0, 0, 0d, 0), ReportService.Totals([]));
}

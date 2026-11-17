using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    [Fact] // 9) скасування: токен скасовано ще до виклику — файл навіть не відкривається
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCatalogImporter(), "any.json", cts.Token));
    }

    [Fact] // 10) імпорт додає нові, пропускає наявні (ідемпотентність) і піднімає подію на кожен доданий
    public async Task ImportAsync_AddsOnlyNew_AndRaisesEventPerAdded()
    {
        var items = TestData.Items();
        var store = new InMemoryCatalogStore(items.Take(2)); // T-1 і T-2 уже є
        var service = new CatalogService(store);
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        var added = await service.ImportAsync(new FakeImporter(items), "fake");
        var again = await service.ImportAsync(new FakeImporter(items), "fake");

        Assert.Equal(4, added);
        Assert.Equal(0, again);
        Assert.Equal(6, store.GetAll().Count);
        Assert.Equal(4, events.Count);
        Assert.All(events, e => Assert.Equal(CatalogChangeKind.Added, e.Kind));
    }

    [Theory] // 11) єдиний тест із диском: тимчасовий файл із поганим записом, видаляється у finally
    [InlineData("""{ "id": "X-2", "sku": "B", "name": "Нуль", "unit": "шт", "quantity": 0 }""", "кількість не може бути 0")]
    [InlineData("""{ "id": "X-2", "sku": "B", "name": "Мінус", "unit": "шт", "quantity": -5 }""", "від'ємною")]
    [InlineData("""{ "id": "X-2", "sku": " ", "name": "Без SKU", "unit": "шт", "quantity": 5 }""", "SKU")]
    public async Task LoadAsync_WithBadRecord_ThrowsDomainException_WithRecordNumber(string badRecord, string expectedText)
    {
        var path = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}.json");
        var json = $$"""[ { "id": "X-1", "sku": "A", "name": "Добрий", "unit": "шт", "quantity": 1 }, {{badRecord}} ]""";
        try
        {
            await File.WriteAllTextAsync(path, json);

            var ex = await Assert.ThrowsAsync<DomainException>(() => new JsonCatalogImporter().LoadAsync(path));

            Assert.StartsWith("Запис 2", ex.Message);
            Assert.Contains(expectedText, ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // 12) дод. 2: зв'язані токени — спрацював саме таймаут
    public async Task ImportAsync_WithTimeout_WhenImporterHangs_ThrowsTimeoutException()
    {
        var service = new CatalogService(new InMemoryCatalogStore());

        var ex = await Assert.ThrowsAsync<TimeoutException>(
            () => service.ImportAsync(new HangingImporter(), "fake", TimeSpan.FromMilliseconds(50)));

        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
    }

    [Fact] // 13) дод. 2: скасував користувач — це НЕ таймаут, OperationCanceledException проходить як є
    public async Task ImportAsync_WithTimeout_WhenUserCancels_ThrowsOperationCanceled()
    {
        using var userCts = new CancellationTokenSource();
        userCts.CancelAfter(TimeSpan.FromMilliseconds(50));
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new HangingImporter(), "fake", TimeSpan.FromMinutes(1), userCts.Token));
    }

    [Fact] // 14) дод. 1: потоковий імпорт зупиняється посередині, уже додане лишається
    public async Task ImportStreamAsync_CancelledAfterSecondItem_StopsAndKeepsAdded()
    {
        using var cts = new CancellationTokenSource();
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var seen = 0;
        service.Changed += (_, _) => { if (++seen == 2) cts.Cancel(); }; // скасувати після 2-го запису

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportStreamAsync(AsyncStream.Of(TestData.Items()), cts.Token));

        Assert.Equal(2, store.GetAll().Count);
    }

    [Fact] // 15) дод. 3: IProgress отримує зростаючі відсотки і завершується на 100
    public async Task ImportAsync_ReportsProgress_UpTo100()
    {
        var progress = new ListProgress();
        var service = new CatalogService(new InMemoryCatalogStore());

        await service.ImportAsync(new FakeImporter(TestData.Items()), "fake", progress: progress);

        Assert.Equal(new[] { 16, 33, 50, 66, 83, 100 }, progress.Values);
    }
}

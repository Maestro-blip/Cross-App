using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

// Сервіс + InMemoryCatalogStore: ні диска, ні консолі. Кожен тест створює своє сховище.
public class CatalogServiceTests
{
    [Fact] // 1) інваріант: відмова і незмінний стан
    public void Receive_WithNegativeQuantity_Throws_AndKeepsState()
    {
        // Arrange
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var p = service.Add("SKU-1", "Кабель", "м", 10);

        // Act + Assert: некоректний аргумент — ArgumentOutOfRangeException (нащадок ArgumentException)
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Receive(p.Id, -5));
        Assert.Equal(10, store.GetById(p.Id)!.Quantity);
    }

    [Fact] // 2) подія з тижня 6 як спосіб перевірки
    public void Add_RaisesChangedExactlyOnce()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.Add("SKU-2", "Гвинт", "шт", 7);

        Assert.Single(events);
        Assert.Equal(CatalogChangeKind.Added, events[0].Kind);
        Assert.Equal(7, events[0].Delta);
    }

    [Fact] // 3) бізнес-правило → DomainException, стан і події не змінились
    public void Issue_MoreThanInStock_ThrowsDomainException_AndRaisesNoEvent()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var p = service.Add("SKU-3", "Розетка", "шт", 5);
        var events = 0;
        service.Changed += (_, _) => events++;

        var ex = Assert.Throws<DomainException>(() => service.Issue(p.Id, 6));

        Assert.Contains("залишок", ex.Message);
        Assert.Equal(5, store.GetById(p.Id)!.Quantity);
        Assert.Equal(0, events);
    }

    [Theory] // 4) некоректна кількість — це аргумент, а не бізнес-правило
    [InlineData(0)]
    [InlineData(-1)]
    public void Issue_WithNonPositiveAmount_ThrowsArgumentOutOfRange(int amount)
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var p = service.Add("SKU-4", "Лампа", "шт", 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Issue(p.Id, amount));
    }

    [Fact] // 5) неіснуючий товар — бізнес-помилка
    public void Receive_UnknownId_ThrowsDomainException()
    {
        var service = new CatalogService(new InMemoryCatalogStore());

        Assert.Throws<DomainException>(() => service.Receive("NO-SUCH", 1));
    }
}

using Core.Abstraction;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// Операції складу. Знає лише ICatalogStore і Product: про файли, DTO і спосіб виводу — нічого.
// Правила товару (кількість > 0, не видати більше залишку) перевіряє Product.
// Лаб 6: після кожної успішної зміни сервіс повідомляє про неї подією Changed
// (і, якщо передано, через INotificationSink — додаткове завдання 1). Як обробити повідомлення,
// вирішує зовнішній код.
public sealed class CatalogService(ICatalogStore store, INotificationSink? sink = null)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink? _sink = sink;

    // event, а не публічне поле: ззовні доступні лише += і -=, викликати може тільки сервіс.
    public event EventHandler<CatalogChangedEventArgs>? Changed;

    private void OnChanged(Product p, CatalogChangeKind kind, int delta) =>
        Raise(new CatalogChangedEventArgs(p.Id, kind, delta, p.Quantity));

    private void Raise(CatalogChangedEventArgs e)
    {
        Changed?.Invoke(this, e); // ?. — підписників може не бути, тоді Changed == null
        _sink?.Notify(e);
    }

    public Product Add(string sku, string name, string unit, int quantity)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product); // інваріанти вже перевірив Product.Create
        OnChanged(product, CatalogChangeKind.Added, quantity); // після зміни, не до
        return product;
    }

    public void Receive(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає товару з id={id}");
        product.RegisterArrival(quantity); // якщо кине виняток — до OnChanged не дійде
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public void Issue(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає товару з id={id}");
        product.Issue(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Issued, -quantity);
    }

    public void Remove(string id)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає товару з id={id}");
        if (!_store.Delete(id))
            throw new InvalidOperationException($"Не вдалося видалити товар id={id}");
        // Запису більше немає: залишок після операції — 0, дельта — весь колишній залишок.
        Raise(new CatalogChangedEventArgs(product.Id, CatalogChangeKind.Removed, -product.Quantity, 0));
    }

    public IReadOnlyList<Product> All() => _store.GetAll();

    public Product? Find(string id) => _store.GetById(id);

    // Поруч із Find(id): тут критерій задає той, хто викликає. Потрібна відповідь (список),
    // тому це Func<Product, bool>, а не подія.
    public IReadOnlyList<Product> FindAll(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.GetAll().Where(predicate).ToList();
    }
}

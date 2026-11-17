using Core.Abstraction;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// Операції складу. Знає лише ICatalogStore і Product: про файли, DTO і спосіб виводу — нічого.
// Правила товару (кількість > 0, не видати більше залишку) перевіряє Product.
// Лаб 8: бізнес-помилки (немає товару, видача понад залишок) — DomainException; ImportAsync — async з CancellationToken.
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
            ?? throw new DomainException($"Немає товару з id={id}");
        product.RegisterArrival(quantity); // якщо кине виняток — до OnChanged не дійде
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public void Issue(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new DomainException($"Немає товару з id={id}");
        product.Issue(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Issued, -quantity);
    }

    public void Remove(string id)
    {
        var product = _store.GetById(id)
            ?? throw new DomainException($"Немає товару з id={id}");
        if (!_store.Delete(id))
            throw new DomainException($"Не вдалося видалити товар id={id}");
        // Запису більше немає: залишок після операції — 0, дельта — весь колишній залишок.
        Raise(new CatalogChangedEventArgs(product.Id, CatalogChangeKind.Removed, -product.Quantity, 0));
    }

    // Лаб 8: асинхронний імпорт постачань. Імпортер приходить параметром (ручний DI, як сховище в лаб 5),
    // тому в тесті замість файлу можна передати імпортер у пам'яті.
    // ct передається в LoadAsync і перевіряється в циклі: Cli → ImportAsync → LoadAsync.
    // progress (додаткове завдання 3) — необов'язковий, отримує відсоток опрацьованих записів.
    public async Task<int> ImportAsync(ICatalogImporter importer, string path,
                                       CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);

        var items = await importer.LoadAsync(path, ct);
        var added = 0;
        for (var i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (TryAddImported(items[i]))
                added++;
            progress?.Report((i + 1) * 100 / items.Count);
        }
        return added;
    }

    // Додаткове завдання 2: зв'язані токени. Імпорт зупиняється, коли спрацює БУДЬ-ЯКИЙ із двох сигналів:
    // токен користувача (Ctrl+C) або власний таймаут. Після скасування перевіряємо, хто саме його ініціював:
    // таймаут перетворюємо на TimeoutException, скасування користувачем пропускаємо як є.
    public async Task<int> ImportAsync(ICatalogImporter importer, string path, TimeSpan timeout,
                                       CancellationToken ct = default, IProgress<int>? progress = null)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        try
        {
            return await ImportAsync(importer, path, linked.Token, progress);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Імпорт не вклався в {timeout.TotalMilliseconds:F0} мс.", ex);
        }
    }

    // Додаткове завдання 1: потоковий імпорт — записи додаються по одному, щойно їх прочитано.
    // WithCancellation передає ct в ітератор (там параметр позначено [EnumeratorCancellation]).
    public async Task<int> ImportStreamAsync(IAsyncEnumerable<Product> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var added = 0;
        await foreach (var item in items.WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            if (TryAddImported(item))
                added++;
        }
        return added;
    }

    // Ідемпотентність: повторний імпорт того самого файлу нічого не дублює.
    // Подія Changed (лаб 6) — для кожного реально доданого запису.
    private bool TryAddImported(Product item)
    {
        if (_store.GetById(item.Id) is not null)
            return false;
        _store.Add(item);
        OnChanged(item, CatalogChangeKind.Added, item.Quantity);
        return true;
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

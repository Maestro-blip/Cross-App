using Core.Abstraction;
using Core.Domain;

namespace Core.Storage;

// Додаткове завдання 1: декоратор. Той самий контракт ICatalogStore, нова поведінка —
// кешує читання з будь-якого іншого сховища. Яке саме сховище всередині, декоратор не знає.
public sealed class CachingCatalogStore(ICatalogStore inner) : ICatalogStore
{
    private readonly ICatalogStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private Dictionary<string, Product>? _snapshot;

    // Скільки разів довелося читати внутрішнє сховище та скільки запитів обслужив кеш.
    public int InnerReads { get; private set; }
    public int CacheHits { get; private set; }

    private Dictionary<string, Product> Snapshot()
    {
        if (_snapshot is not null)
        {
            CacheHits++;
            return _snapshot;
        }

        InnerReads++;
        _snapshot = _inner.GetAll().ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        return _snapshot;
    }

    public IReadOnlyList<Product> GetAll() => Snapshot().Values.ToList();

    public Product? GetById(string id) => Snapshot().GetValueOrDefault(id);

    // Зміни йдуть у внутрішнє сховище, а кеш скидається: наступне читання візьме свіжі дані.
    public void Add(Product item)
    {
        _inner.Add(item);
        _snapshot = null;
    }

    public void Update(Product item)
    {
        _inner.Update(item);
        _snapshot = null;
    }

    public bool Delete(string id)
    {
        bool removed = _inner.Delete(id);
        if (removed)
            _snapshot = null;
        return removed;
    }
}

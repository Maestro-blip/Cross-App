using Core.Abstraction;
using Core.Domain;

namespace Core.Storage;

// Сховище в пам'яті: словник «id → товар». Дані живуть, поки працює програма.
public sealed class InMemoryCatalogStore : ICatalogStore
{
    private readonly Dictionary<string, Product> _items;

    public InMemoryCatalogStore(IEnumerable<Product>? seed = null)
    {
        if (seed is not null)
            _items = seed.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        else
            _items = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<Product> GetAll() => _items.Select(p => p.Value).ToList();

    public Product? GetById(string id) => _items.GetValueOrDefault(id);

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Товар з Id={item.Id} уже є");

        _items.Add(item.Id, item);
    }

    public void Update(Product item) => _items[item.Id] = item;

    public bool Delete(string id) => _items.Remove(id);
}

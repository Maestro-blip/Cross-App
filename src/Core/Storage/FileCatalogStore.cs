using Core.Abstraction;
using Core.Domain;

namespace Core.Storage;

// Сховище у файлі JSON: кеш у пам'яті, дозавантаження при першому зверненні,
// запис на диск після кожної зміни. На диск ідуть DTO, назовні — Product.
// Лаб 9: шлях приходить ззовні (з конфігурації, через Cli), формат і атомарний запис — у CatalogJsonFile.
// Контракт ICatalogStore лишається синхронним, як і раніше.
public sealed class FileCatalogStore(string path) : ICatalogStore
{
    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    public string FilePath => _path;

    private void EnsureLoaded()
    {
        if (_loaded) return;
        if (!File.Exists(_path)) { _loaded = true; return; } // файлу ще немає — порожній каталог
        _cache.Clear();
        foreach (var product in CatalogJsonFile.Read(_path)) // повторно перевіряє інваріанти
            _cache[product.Id] = product;
        _loaded = true;
    }

    private void Flush() => CatalogJsonFile.Write(_path, _cache.Values); // .tmp + File.Move

    public IReadOnlyList<Product> GetAll()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Product? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Товар з id={item.Id} уже є");
        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Product item)
    {
        EnsureLoaded();
        _cache[item.Id] = item;
        Flush();
    }

    public bool Delete(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
}

using System.Text.Json;
using Core.Abstraction;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Сховище у файлі JSON: кеш у пам'яті, дозавантаження при першому зверненні,
// запис на диск після кожної зміни. На диск ідуть DTO, назовні — Product.
public sealed class FileCatalogStore(string path) : ICatalogStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;
        if (File.Exists(_path))
        {
            var dtos = JsonSerializer.Deserialize<List<ProductDto>>(File.ReadAllText(_path)) ?? [];
            foreach (var dto in dtos)
            {
                var p = Product.FromDto(dto);
                _cache[p.Id] = p;
            }
        }
        _loaded = true;
    }

    private void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path,
            JsonSerializer.Serialize(_cache.Values.Select(p => p.ToDto()).ToList(), Options));
    }

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

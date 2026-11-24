using Core.Configuration;
using Core.Domain;
using Core.Storage;

namespace Core.Tests;

// Лаб 9: шляхи з конфігурації і JSON-файл каталогу. Усі файли — у тимчасовому каталозі, який видаляється в Dispose.
public sealed class StorageTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"crossapp-lab9-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact] // 1) відносний DataPath рахується від базового каталогу, а не від cwd; каталог створюється
    public void ResolveFullPath_Relative_CombinesWithBaseDir_AndCreatesDirectory()
    {
        var options = new StorageOptions { DataPath = "data", FileName = "items.json" };

        var full = options.ResolveFullPath(_dir);

        Assert.Equal(Path.Combine(_dir, "data", "items.json"), full);
        Assert.True(Directory.Exists(Path.Combine(_dir, "data")));
    }

    [Fact] // 2) абсолютний DataPath (як із CROSSAPP_DATA_PATH) використовується як є
    public void ResolveFullPath_Rooted_IgnoresBaseDir()
    {
        var absolute = Path.Combine(_dir, "custom");
        var options = new StorageOptions { DataPath = absolute, FileName = "x.json" };

        var full = options.ResolveFullPath("/somewhere/else");

        Assert.Equal(Path.Combine(absolute, "x.json"), full);
    }

    [Fact] // 3) експорт → імпорт: дані ті самі, кирилиця не екранована, .tmp не лишається
    public void Write_ThenRead_RoundTrips_WithReadableCyrillic()
    {
        var path = Path.Combine(_dir, "items.json");
        Product[] items =
        [
            Product.Create("a1b2c3d4", "A-100", "Цемент М400", "мішок", 40),
            Product.Create("b2c3d4e5", "A-101", "Пісок річковий", "т", 12)
        ];

        CatalogJsonFile.Write(path, items);
        var text = File.ReadAllText(path);
        var loaded = CatalogJsonFile.Read(path);

        Assert.Contains("\"name\": \"Цемент М400\"", text); // camelCase + кирилиця як є
        Assert.DoesNotContain("\\u04", text);
        Assert.False(File.Exists(path + ".tmp"));
        Assert.Equal(items.Select(p => p.ToDto()), loaded.Select(p => p.ToDto()));
    }

    [Fact] // 4) зіпсований JSON → StorageException зі шляхом і номером рядка
    public void Read_BrokenJson_ThrowsStorageException()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "broken.json");
        File.WriteAllText(path, """[ { "id": "a", "sku": "A", "name": "N", "unit": "шт", "quantity": 1 }""");

        var ex = Assert.Throws<StorageException>(() => CatalogJsonFile.Read(path));

        Assert.Contains("пошкоджено", ex.Message);
        Assert.Contains(path, ex.Message);
    }

    [Fact] // 5) запис, що порушує інваріант, теж відхиляється з номером запису
    public void Read_InvalidRecord_ThrowsStorageException_WithRecordNumber()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "bad.json");
        File.WriteAllText(path, """[ { "id": "a", "sku": "A", "name": "N", "unit": "шт", "quantity": 1 }, { "id": "b", "sku": "B", "name": "N", "unit": "шт", "quantity": -3 } ]""");

        var ex = Assert.Throws<StorageException>(() => CatalogJsonFile.Read(path));

        Assert.Contains("запис 2", ex.Message);
    }

    [Fact] // 6) FileCatalogStore: каталогу ще немає — створюється при першому записі, дані читає новий екземпляр
    public void FileCatalogStore_CreatesMissingDirectory_AndPersists()
    {
        var path = Path.Combine(_dir, "nested", "items.json");
        var store = new FileCatalogStore(path);

        store.Add(Product.Create("id-1", "SKU-1", "Кабель", "м", 5));
        var reopened = new FileCatalogStore(path);

        Assert.True(File.Exists(path));
        Assert.Equal("Кабель", reopened.GetById("id-1")?.Name);
    }
}

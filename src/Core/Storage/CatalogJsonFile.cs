using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Лаб 9: один формат JSON для всієї колекції — його використовують і FileCatalogStore, і команди export/import.
// На диск ідуть ProductDto (id, sku, name, unit, quantity), назовні — Product.
public static class CatalogJsonFile
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,        // "quantity", а не "Quantity"
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Note = null у файл не пишемо
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping      // кирилиця як є, а не Ц
    };

    // Атомарний запис: спершу повний вміст у .tmp, потім File.Move замінює старий файл одним кроком.
    // Якщо програма впаде посеред запису — пошкодиться лише .tmp, а items.json лишиться попереднім.
    public static void Write(string path, IEnumerable<Product> products)
    {
        var dtos = products.Select(p => p.ToDto()).ToList();
        var tmp = path + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(tmp, JsonSerializer.Serialize(dtos, Options));
        File.Move(tmp, path, overwrite: true);
    }

    // Читання з повторною перевіркою інваріантів (Product.FromDto).
    // Зіпсований JSON або поганий запис → StorageException із зрозумілим текстом замість stack trace.
    // fixDto дає змогу підправити запис перед перевіркою (наприклад, підставити одиницю за замовчуванням).
    public static IReadOnlyList<Product> Read(string path, Func<ProductDto, ProductDto>? fixDto = null)
    {
        List<ProductDto> dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<ProductDto>>(File.ReadAllText(path), Options) ?? [];
        }
        catch (JsonException ex)
        {
            var line = ex.LineNumber is long n ? $", рядок {n + 1}" : "";
            throw new StorageException($"Файл {path} пошкоджено{line}: очікувався масив товарів у форматі JSON.", ex);
        }

        var products = new List<Product>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            try
            {
                var dto = fixDto is null ? dtos[i] : fixDto(dtos[i]);
                products.Add(Product.FromDto(dto));
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new StorageException($"Файл {path}, запис {i + 1}: {ex.Message}", ex);
            }
        }
        return products;
    }
}

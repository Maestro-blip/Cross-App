using Core.Dto;
using Core.Import;

if (args is ["--catalog", ..])
{
    string catalogPath = args.Length > 1 ? args[1] : Path.Combine("data", "catalog.csv");

    if (!File.Exists(catalogPath))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(catalogPath)}");
        return 1;
    }

    CatalogImportResult catalog = ProductCsvImporter.LoadCatalog(catalogPath);

    Console.WriteLine($"Завантажено товарів: {catalog.Products.Count}");
    foreach (ProductDto p in catalog.Products.Take(5))
        Console.WriteLine($"  {p.Id,-6} {p.Sku,-10} {p.Name,-26} {p.Quantity,5} {p.Unit}");

    Console.WriteLine($"Завантажено складів: {catalog.Warehouses.Count}");
    foreach (WarehouseDto w in catalog.Warehouses.Take(5))
        Console.WriteLine($"  {w.Id,-6} {w.Name,-26} {w.City}");

    if (catalog.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {catalog.Errors.Count}");
        foreach (string e in catalog.Errors)
            Console.WriteLine($"  ! {e}");
    }

    return 0;
}

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

ImportResult<ProductDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".json" => ProductJsonImporter.Load(path),
    _ => ProductCsvImporter.Load(path)
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
foreach (ProductDto p in result.Items.Take(5))
    Console.WriteLine($"  {p.Id,-6} {p.Sku,-10} {p.Name,-26} {p.Quantity,5} {p.Unit}");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($"  ! {e}");
}

int total = result.Items.Count + result.Errors.Count;
double errorPercent = total == 0 ? 0 : 100.0 * result.Errors.Count / total;
Console.WriteLine($"Усього: {total} / прийнято: {result.Items.Count} / пропущено: {result.Errors.Count} / помилок: {errorPercent:F1}%");

return 0;

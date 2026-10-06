using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    // Роздільник — крапка з комою: не конфліктує з комою в назвах товарів.
    private const char Separator = ';';

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;
            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue; // рядок заголовків

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    // Додаткове завдання 2: різнорідні рядки "P;..." – товари, "W;..." – склади.
    public static CatalogImportResult LoadCatalog(string path)
    {
        var products = new List<ProductDto>();
        var warehouses = new List<WarehouseDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseCatalogLine(line))
            {
                case ParseOk ok:
                    products.Add(ok.Value);
                    break;
                case WarehouseOk w:
                    warehouses.Add(w.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new CatalogImportResult(products, warehouses, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 5 } => new ParseFailed($"очікую 5 колонок, отримав {parts.Length}"),
            [_, "", _, _, _] or [_, _, "", _, _]
                => new ParseFailed("SKU або назва порожні"),
            [_, _, _, _, var qty] when !int.TryParse(qty, CultureInfo.InvariantCulture, out int q) || q < 0
                => new ParseFailed($"кількість '{qty}' не є невід'ємним числом"),
            [var id, var sku, var name, var unit, var qty]
                => new ParseOk(new ProductDto(id, sku, name, unit, int.Parse(qty, CultureInfo.InvariantCulture))),
            _ => new ParseFailed($"занадто багато колонок: {parts.Length}")
        };
    }

    private static ParseOutcome ParseCatalogLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", .. var rest] => ParseLine(string.Join(Separator, rest)),
            ["W", _, "", _] => new ParseFailed("назва складу порожня"),
            ["W", var id, var name, var city] => new WarehouseOk(new WarehouseDto(id, name, city)),
            ["W", ..] => new ParseFailed($"склад: очікую 3 колонки, отримав {parts.Length - 1}"),
            [var type, ..] => new ParseFailed($"невідомий тип запису '{type}'"),
            _ => new ParseFailed("порожній рядок")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(ProductDto Value) : ParseOutcome;
    private sealed record WarehouseOk(WarehouseDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}

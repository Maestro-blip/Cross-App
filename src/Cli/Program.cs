using System.Text.Json;
using Core;
using Core.Domain;
using Core.Dto;
using Core.Import;
using Core.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

return args switch
{
    ["--env", ..] => RunEnvironment(json: false),
    ["--json", ..] => RunEnvironment(json: true),
    ["--domain", ..] => RunDomain(),
    ["--domain-import", ..] => RunDomainImport(PathArg(Path.Combine("data", "domain.json"))),
    ["--warehouse", ..] => RunWarehouse(),
    ["--movement", ..] => RunMovement(),
    ["--catalog", ..] => RunCatalog(PathArg(Path.Combine("data", "catalog.csv"))),
    [var path, ..] => RunImport(path),
    [] => RunImport(Path.Combine("data", "sample.csv"))
};

string PathArg(string fallback) => args.Length > 1 ? args[1] : fallback;

// Лаб 1-2: інформація про середовище.
static int RunEnvironment(bool json)
{
    EnvironmentReport report = EnvironmentInfo.Collect();
    Console.WriteLine(json
        ? JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
        : report.ToString());
    return 0;
}

// Лаб 3: імпорт CSV / JSON, імпортер обирається за розширенням.
static int RunImport(string path)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
        return 1;
    }

    ImportResult<ProductDto> result = LoadDtos(path);

    Console.WriteLine($"Завантажено записів: {result.Items.Count}");
    foreach (ProductDto p in result.Items.Take(5))
        Console.WriteLine($"  {p.Id,-6} {p.Sku,-10} {p.Name,-26} {p.Quantity,5} {p.Unit}");

    PrintErrors(result.Errors);
    PrintStats(result.Items.Count, result.Errors.Count);
    return 0;
}

// Лаб 3, додаткове завдання 2: різнорідні рядки.
static int RunCatalog(string path)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
        return 1;
    }

    CatalogImportResult catalog = ProductCsvImporter.LoadCatalog(path);

    Console.WriteLine($"Завантажено товарів: {catalog.Products.Count}");
    foreach (ProductDto p in catalog.Products.Take(5))
        Console.WriteLine($"  {p.Id,-6} {p.Sku,-10} {p.Name,-26} {p.Quantity,5} {p.Unit}");

    Console.WriteLine($"Завантажено складів: {catalog.Warehouses.Count}");
    foreach (WarehouseDto w in catalog.Warehouses.Take(5))
        Console.WriteLine($"  {w.Id,-6} {w.Name,-26} {w.City}");

    PrintErrors(catalog.Errors);
    return 0;
}

// Лаб 4: успішний сценарій і порушення інваріантів.
static int RunDomain()
{
    Console.WriteLine("=== Сценарій 1: успіх ===");
    Product product = Product.Create("P-001", "sku-001", "Цемент М400 25кг", "шт", 100);
    Console.WriteLine(product);
    product.RegisterArrival(50);
    product.Issue(30);
    Console.WriteLine(product);

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
    TryDo("видача більша за залишок", () => product.Issue(1000));
    TryDo("нульовий прихід", () => product.RegisterArrival(0));
    TryDo("порожній SKU", () => Product.Create("P-002", " ", "Пісок", "т", 10));
    TryDo("від'ємний залишок", () => Product.Create("P-003", "SKU-003", "Цегла", "шт", -5));
    Console.WriteLine($"Стан після всіх відмов: {product}");

    Console.WriteLine();
    Console.WriteLine("=== ToDto / FromDto ===");
    ProductDto dto = product.ToDto();
    Console.WriteLine($"DTO: {dto}");
    Console.WriteLine($"Відновлено: {Product.FromDto(dto)}");
    TryDo("FromDto з від'ємною кількістю", () => Product.FromDto(dto with { Quantity = -1 }));
    return 0;
}

// Лаб 4, додаткове завдання 1: ImportResult -> сутності + помилки інваріантів.
static int RunDomainImport(string path)
{
    if (!File.Exists(path))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
        return 1;
    }

    ImportResult<ProductDto> dtos = LoadDtos(path);
    ImportResult<Product> domain = ProductDomainImporter.Convert(dtos);

    Console.WriteLine($"DTO прочитано: {dtos.Items.Count}, помилок формату: {dtos.Errors.Count}");
    Console.WriteLine($"Сутностей створено: {domain.Items.Count}");
    foreach (Product p in domain.Items)
        Console.WriteLine($"  {p}");

    PrintErrors(domain.Errors);
    PrintStats(domain.Items.Count, domain.Errors.Count);
    return 0;
}

// Лаб 4, додаткове завдання 2: інваріант двох сутностей (Product + Warehouse) у сервісі.
static int RunWarehouse()
{
    Warehouse kyiv = Warehouse.Create("w-01", "Центральний склад", "Київ", 500);
    Warehouse lviv = Warehouse.Create("w-02", "Склад сипучих матеріалів", "Львів", 100);
    Product cement = Product.Create("P-001", "SKU-001", "Цемент М400 25кг", "шт", 300);
    Product sand = Product.Create("P-002", "SKU-002", "Пісок будівельний", "т", 80);
    Product brick = Product.Create("P-003", "SKU-003", "Цегла червона", "шт", 150);

    var stock = new StockService();

    Console.WriteLine("=== Сценарій 1: розміщення в межах місткості ===");
    stock.Place(cement, kyiv);
    stock.Place(brick, kyiv);
    stock.Place(sand, lviv);
    stock.RegisterArrival(cement, 40);
    PrintWarehouse(stock, kyiv);
    PrintWarehouse(stock, lviv);

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: порушення правила місткості ===");
    TryDo("прихід понад місткість", () => stock.RegisterArrival(brick, 100));
    TryDo("розміщення понад місткість", () =>
        stock.Place(Product.Create("P-004", "SKU-004", "Щебінь", "т", 50), lviv));
    TryDo("повторне розміщення товару", () => stock.Place(sand, kyiv));
    TryDo("прихід товару без складу", () =>
        stock.RegisterArrival(Product.Create("P-005", "SKU-005", "Шпаклівка", "кг", 10), 5));
    TryDo("нульовий прихід через сервіс", () => stock.RegisterArrival(cement, 0));
    TryDo("склад з нульовою місткістю", () => Warehouse.Create("W-09", "Порожній", "Одеса", 0));

    Console.WriteLine("Стан після всіх відмов:");
    PrintWarehouse(stock, kyiv);
    PrintWarehouse(stock, lviv);
    return 0;
}

// Лаб 4, додаткове завдання 3: enum-стан і допустимі переходи через switch expression.
static int RunMovement()
{
    MovementStatus[] statuses = Enum.GetValues<MovementStatus>();
    Console.WriteLine("=== Таблиця переходів (рядок -> стовпець) ===");
    Console.WriteLine($"{"",-10}" + string.Concat(statuses.Select(s => $"{s,-11}")));
    foreach (MovementStatus from in statuses)
        Console.WriteLine($"{from,-10}" + string.Concat(statuses.Select(to => $"{(Movement.CanTransition(from, to) ? "+" : "-"),-11}")));

    Product cement = Product.Create("P-001", "SKU-001", "Цемент М400 25кг", "шт", 100);
    Product sand = Product.Create("P-002", "SKU-002", "Пісок будівельний", "т", 20);

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 1: Draft -> Posted -> Cancelled ===");
    Movement issue = Movement.Create("vn-001", MovementType.Issue);
    issue.AddLine(cement, 30);
    issue.AddLine(sand, 5);
    Console.WriteLine(issue);
    issue.Post();
    Console.WriteLine($"{issue}; {cement.Sku} = {cement.Quantity}, {sand.Sku} = {sand.Quantity}");
    issue.Cancel();
    Console.WriteLine($"{issue}; {cement.Sku} = {cement.Quantity}, {sand.Sku} = {sand.Quantity}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: заборонені переходи та дії ===");
    TryDo("повторне скасування", issue.Cancel);
    TryDo("проведення скасованої", issue.Post);
    TryDo("рядок у скасовану", () => issue.AddLine(cement, 1));

    Movement posted = Movement.Create("pn-002", MovementType.Arrival);
    posted.AddLine(cement, 10);
    posted.Post();
    TryDo("повторне проведення", posted.Post);
    TryDo("рядок у проведену", () => posted.AddLine(sand, 1));

    Movement empty = Movement.Create("vn-003", MovementType.Issue);
    TryDo("проведення порожньої", empty.Post);
    TryDo("рядок з нульовою кількістю", () => empty.AddLine(cement, 0));

    Movement tooMuch = Movement.Create("vn-004", MovementType.Issue);
    tooMuch.AddLine(cement, 5);
    tooMuch.AddLine(sand, 999);
    TryDo("видача понад залишок", tooMuch.Post);
    Console.WriteLine($"{tooMuch}; {cement.Sku} = {cement.Quantity} (перший рядок теж не списано)");
    return 0;
}

static ImportResult<ProductDto> LoadDtos(string path) => Path.GetExtension(path).ToLowerInvariant() switch
{
    ".json" => ProductJsonImporter.Load(path),
    _ => ProductCsvImporter.Load(path)
};

static void PrintWarehouse(StockService stock, Warehouse warehouse)
{
    Console.WriteLine($"  {warehouse}: зайнято {stock.Load(warehouse)}, вільно {stock.FreeSpace(warehouse)}");
    foreach (Product p in stock.ProductsIn(warehouse))
        Console.WriteLine($"    {p}");
}

static void PrintErrors(IReadOnlyList<string> errors)
{
    if (errors.Count == 0)
        return;

    Console.WriteLine($"Пропущено рядків: {errors.Count}");
    foreach (string e in errors)
        Console.WriteLine($"  ! {e}");
}

static void PrintStats(int accepted, int skipped)
{
    int total = accepted + skipped;
    double errorPercent = total == 0 ? 0 : 100.0 * skipped / total;
    Console.WriteLine($"Усього: {total} / прийнято: {accepted} / пропущено: {skipped} / помилок: {errorPercent:F1}%");
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}

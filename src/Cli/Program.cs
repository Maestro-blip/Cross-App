using System.Text.Json;
using Core;
using Core.Domain;
using Core.Dto;
using Core.Import;
using Core.Services;
using Core.Abstraction;
using Core.Storage;
using Core.Events;
using Core.Reports;
using System.Diagnostics;

Console.OutputEncoding = System.Text.Encoding.UTF8;

return args switch
{
    // Лаб 7: підкоманди звітів. Перевіряється будь-яка позиція, тож "-- --file report-top" теж працює.
    _ when args.Any(a => a.StartsWith("report", StringComparison.Ordinal)) => RunReports(args),
    ["--env", ..] => RunEnvironment(json: false),
    ["--json", ..] => RunEnvironment(json: true),
    ["--domain", ..] => RunDomain(),
    ["--domain-import", ..] => RunDomainImport(PathArg(Path.Combine("data", "domain.json"))),
    ["--warehouse", ..] => RunWarehouse(),
    ["--movement", ..] => RunMovement(),
    ["--catalog", ..] => RunCatalog(PathArg(Path.Combine("data", "catalog.csv"))),
    ["--import", ..] => RunImport(PathArg(Path.Combine("data", "sample.csv"))),
    ["--service", ..] => RunService(args),
    ["--file" or "--cache", ..] => RunService(args),
    ["--events", ..] => RunEvents(args),
    ["--sinks", ..] => RunSinks(args),
    [var path, ..] => RunImport(path),
    [] => RunEvents(args)
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

    // Лаб 6, додаткове завдання 2: звіт про пропущені рядки приходить через Action<string>
    // під час імпорту. Лямбда — замикання над лічильником skipped.
    int skipped = 0;
    Console.WriteLine("Пропущені рядки (повідомляє імпортер через Action<string>):");
    ImportResult<ProductDto> result = LoadDtos(path, reason =>
    {
        skipped++;
        Console.WriteLine($"  ! {reason}");
    });
    if (skipped == 0)
        Console.WriteLine("  немає");

    Console.WriteLine($"Завантажено записів: {result.Items.Count}");
    foreach (ProductDto p in result.Items.Take(5))
        Console.WriteLine($"  {p.Id,-6} {p.Sku,-10} {p.Name,-26} {p.Quantity,5} {p.Unit}");

    PrintStats(result.Items.Count, skipped);
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
    Console.WriteLine("212313-----------------------");
    Console.WriteLine(product);
    TryDo("Перевірка інваріанту", () => product.RegisterArrival(-3));
    Console.WriteLine();
    Console.WriteLine(product);
    Console.WriteLine("323232-----------------------");
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

// Лаб 5: сервісний шар, два сховища, ручний DI.
// dotnet run --project src/Cli                    → InMemoryCatalogStore
// dotnet run --project src/Cli -- --file          → FileCatalogStore
// dotnet run --project src/Cli -- --cache [--file] → те саме, загорнуте в CachingCatalogStore
static int RunService(string[] options)
{
    // Composition root: конкретні класи обирає StoreFactory (додаткове завдання 3),
    // сервіс отримує лише інтерфейс.
    ICatalogStore store = StoreFactory.Create(options);
    var service = new CatalogService(store);

    Console.WriteLine($"Сховище: {store.GetType().Name}");
    if (options.Contains("--file"))
        Console.WriteLine($"Файл: {StoreFactory.DataPath}");

    var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
    service.Receive(created.Id, 25);
    service.Issue(created.Id, 10);
    Console.WriteLine($"Додано {created.Id}, прихід 25, видача 10 → залишок {created.Quantity} {created.Unit}");

    Console.WriteLine($"Список ({service.All().Count}):");
    foreach (Product p in service.All())
        Console.WriteLine($"  {p.Id,-9} {p.Sku,-13} {p.Name,-38} {p.Quantity,5} {p.Unit}");

    Console.WriteLine($"Пошук за id={created.Id}: {service.Find(created.Id)}");

    // Лаб 5, додаткове завдання 2 (у лаб 6 метод перейменовано на FindAll): умова — лямбда Func<Product, bool>.
    var lowStock = service.FindAll(p => p.Quantity < 10);
    Console.WriteLine($"Пошук «залишок < 10»: {string.Join(", ", lowStock.Select(p => $"{p.Sku} ({p.Quantity})"))}");
    var tech = service.FindAll(p => p.Sku.StartsWith("TECH", StringComparison.OrdinalIgnoreCase));
    Console.WriteLine($"Пошук «SKU починається з TECH»: {tech.Count} шт.");

    // Додаткове завдання 1: статистика декоратора, якщо його увімкнено.
    if (store is CachingCatalogStore cache)
        Console.WriteLine($"Кеш: читань внутрішнього сховища {cache.InnerReads}, відповідей з кешу {cache.CacheHits}");

    Console.WriteLine("Сценарій відмови:");
    TryDo("прихід для неіснуючого id", () => service.Receive("NO-SUCH", 5));
    TryDo("видача понад залишок", () => service.Issue(created.Id, 1000));
    return 0;
}

// Лаб 6: доменні події, делегати Action/Func, лямбди.
// dotnet run --project src/Cli -f net10.0 [-- --events [--file]]
static int RunEvents(string[] options)
{
    // Журнал поруч зі зібраною програмою; каталог створюємо кодом (інакше DirectoryNotFoundException).
    string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    ICatalogStore store = StoreFactory.Create(options);
    var service = new CatalogService(store);
    Console.WriteLine($"Сховище: {store.GetType().Name}");
    Console.WriteLine($"Журнал: {logPath}");
    Console.WriteLine();

    // Підписник 1 — локальна функція: має стале ім'я, тому її можна і підписати (+=), і відписати (-=).
    // Не static: захоплює logPath із RunEvents (замикання).
    void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
    {
        string line = $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}";
        Console.WriteLine(line);
        try
        {
            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch (IOException ex) // збій журналу не повинен ламати вже виконану операцію
        {
            Console.WriteLine($"  [журнал недоступний: {ex.Message}]");
        }
    }

    // Підписник 2 — лямбда із замиканням над локальною змінною changes.
    // Збережена у змінну, щоб наприкінці її теж можна було відписати.
    int changes = 0;
    EventHandler<CatalogChangedEventArgs> counter = (_, _) =>
    {
        changes++;
        Console.WriteLine($"         └ підписник 2: changes = {changes}");
    };

    service.Changed += OnCatalogChanged; // викликається першим
    service.Changed += counter;          // викликається другим — у порядку підписки

    Console.WriteLine("=== Крок 5: операції з двома підписниками ===");
    var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
    service.Receive(created.Id, 25);
    service.Issue(created.Id, 10);
    Console.WriteLine($"Подій отримано: {changes}");

    Console.WriteLine();
    Console.WriteLine("=== Після service.Changed -= OnCatalogChanged ===");
    service.Changed -= OnCatalogChanged;
    service.Receive(created.Id, 5); // тиха для консолі/журналу: працює лише лічильник
    Console.WriteLine($"Подій отримано: {changes}; залишок {created.Quantity} {created.Unit} (дані змінились, рядка журналу немає)");

    Console.WriteLine();
    Console.WriteLine("=== Крок 6: сценарій відмови — винятки є, подій немає ===");
    int before = changes;
    TryDo("від'ємний прихід", () => service.Receive(created.Id, -5));
    TryDo("неіснуючий id", () => service.Receive("NO-SUCH", 5));
    TryDo("видача понад залишок", () => service.Issue(created.Id, 1000));
    Console.WriteLine($"Подій отримано: {changes} (було {before}) — невдалі операції не дали повідомлень");

    Console.WriteLine();
    Console.WriteLine("=== Відписка лямбдою «в один рядок» не працює ===");
    service.Changed -= (_, _) => changes++; // новий екземпляр делегата — у списку його немає
    service.Issue(created.Id, 1);
    Console.WriteLine($"Подій отримано: {changes} — лічильник досі підписаний");
    service.Changed -= counter;             // той самий екземпляр — відписка спрацьовує
    service.Issue(created.Id, 1);
    Console.WriteLine($"Подій отримано: {changes} — після -= counter підписників немає, Changed == null");

    Console.WriteLine();
    Console.WriteLine("=== Крок 7: Func замість події, коли потрібна відповідь ===");
    IReadOnlyList<Product> low = service.FindAll(p => p.Quantity < 10);
    Console.WriteLine($"FindAll(p => p.Quantity < 10): {low.Count} шт.");
    foreach (Product p in low)
        Console.WriteLine($"  {p.Id,-9} {p.Sku,-13} {p.Name,-30} {p.Quantity,4} {p.Unit}");
    return 0;
}

// Лаб 6, додаткове завдання 1: ті самі повідомлення через INotificationSink, переданий у конструктор
// (ручний DI). Подія Changed тут не використовується.
// dotnet run --project src/Cli -f net10.0 -- --sinks [--file]
static int RunSinks(string[] options)
{
    ChangeLog.EnsureDirectory();
    ICatalogStore store = StoreFactory.Create(options);
    INotificationSink sink = new CompositeSink(new ConsoleSink(), new FileSink(ChangeLog.Path));
    var service = new CatalogService(store, sink);

    Console.WriteLine($"Сховище: {store.GetType().Name}; одержувач: {sink.GetType().Name}(ConsoleSink, FileSink)");
    var created = service.Add("SKU-202", "Патч-корд 2м", "шт", 20);
    service.Receive(created.Id, 10);
    service.Issue(created.Id, 25);
    service.Remove(created.Id);
    TryDo("видача видаленого товару", () => service.Issue(created.Id, 1));
    Console.WriteLine($"Журнал: {ChangeLog.Path}");
    return 0;
}

// Лаб 7: звіти через LINQ. Уся логіка запитів — у Core (ReportService), тут лише вибір підкоманди і вивід.
// dotnet run --project src/Cli -f net10.0 -- report-top|report-low|report-groups|report-sum [--file]
//   report-deferred | report-query | report-join | report-empty | report-bench
static int RunReports(string[] options)
{
    ICatalogStore store = StoreFactory.Create(options);
    var service = new CatalogService(store);
    IReadOnlyList<Product> items = service.All(); // один знімок на всі звіти команди

    // Перший аргумент, що не є прапорцем: --file / --cache з лаб 5 не стають назвою звіту.
    string command = options.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";

    switch (command)
    {
        case "report-top":
            Console.WriteLine("Топ-5 за залишком (за спаданням, при рівності — за SKU):");
            PrintLines(ReportService.TopByQuantity(items, 5));
            break;

        case "report-low":
            Console.WriteLine("Дефіцит, залишок < 10 (за зростанням залишку, далі за SKU):");
            PrintLines(ReportService.LowStock(items, 10));
            break;

        case "report-groups":
            Console.WriteLine("Групи за одиницею виміру (позицій і сумарний залишок):");
            PrintGroups(ReportService.ByUnit(items));
            break;

        case "report-sum":
            PrintTotals(ReportService.Totals(items));
            break;

        case "report-deferred":
            RunDeferredDemo(items);
            break;

        case "report-query":
            RunQuerySyntax(items);
            break;

        case "report-join":
            RunJoin(service);
            break;

        case "report-empty":
            RunEmpty();
            break;

        case "report-bench":
            RunBenchmark();
            break;

        default:
            Console.WriteLine("report-top | report-low | report-groups | report-sum | report-deferred | report-query | report-join | report-empty | report-bench");
            return 1;
    }
    return 0;
}

// Крок 3: відкладене виконання. service.All() щоразу повертає новий знімок,
// тому джерелом беремо локальний список, який можна змінити.
static void RunDeferredDemo(IReadOnlyList<Product> items)
{
    var source = items.ToList();
    IEnumerable<StockLine> query = ReportService.LowStockDeferred(source, 10); // запит побудовано, не виконано
    Console.WriteLine($"Дефіцитних до змін: {query.Count()}");          // Count() перебирає source зараз

    source.Add(Product.Create("tmp00001", "SKU-999", "Скоба", "шт", 3));
    Console.WriteLine($"Дефіцитних після змін: {query.Count()}");       // запит виконано ще раз — бачить «Скобу»

    List<StockLine> snapshot = query.ToList();                           // результат зафіксовано
    source.Clear();
    Console.WriteLine($"Після source.Clear(): query.Count() = {query.Count()}, snapshot.Count = {snapshot.Count}");
}

// Додаткове завдання 3: той самий звіт у синтаксисі методів і синтаксисі запитів.
static void RunQuerySyntax(IReadOnlyList<Product> items)
{
    IReadOnlyList<GroupSummary> methods = ReportService.ByUnit(items);
    IReadOnlyList<GroupSummary> query = ReportService.ByUnitQuery(items);
    Console.WriteLine("ByUnitQuery — звіт у синтаксисі запитів:");
    PrintGroups(query);
    // record порівнюються за значенням, тож SequenceEqual перевіряє і склад, і порядок груп.
    Console.WriteLine($"Збігається з ByUnit (синтаксис методів): {methods.SequenceEqual(query)}");
}

// Додаткове завдання 1: GroupJoin товарів із журналом подій Changed (лаб 6).
static void RunJoin(CatalogService service)
{
    var journal = new List<CatalogChangedEventArgs>();
    service.Changed += (_, e) => journal.Add(e); // подія наповнює другу колекцію

    var cable = service.Add("SKU-301", "Патч-корд 1м", "шт", 30);
    service.Receive(cable.Id, 20);
    service.Issue(cable.Id, 15);
    service.Issue("P-001", 2);
    service.Receive("P-001", 10);
    service.Receive("P-016", 100);
    service.Issue("P-010", 4);

    IReadOnlyList<Product> products = service.All();
    Console.WriteLine($"Подій у журналі: {journal.Count}; товарів: {products.Count}");
    Console.WriteLine("Найактивніші товари (товари × журнал подій):");
    Console.WriteLine($"  {"SKU",-13} {"Назва",-38} {"Залишок",8} {"Операцій",9} {"Зміна",7}");
    foreach (ProductActivity a in ReportService.MostActive(products, journal, 5))
        Console.WriteLine($"  {a.Sku,-13} {a.Name,-38} {a.Quantity,8} {a.Operations,9} {a.NetDelta,7:+0;-0}");

    int quiet = ReportService.Activity(products, journal).Count(a => a.Operations == 0);
    Console.WriteLine($"Товарів без операцій (GroupJoin лишає їх з Operations = 0): {quiet}");
}

// Самоперевірка 3: звіти на порожньому джерелі не падають.
static void RunEmpty()
{
    Product[] empty = [];
    Console.WriteLine($"LowStock: {ReportService.LowStock(empty, 10).Count} рядків");
    Console.WriteLine($"TopByQuantity: {ReportService.TopByQuantity(empty, 5).Count} рядків");
    Console.WriteLine($"ByUnit: {ReportService.ByUnit(empty).Count} груп");
    PrintTotals(ReportService.Totals(empty));
}

// Додаткове завдання 4: Stopwatch на 100 000 згенерованих записів.
static void RunBenchmark()
{
    const int count = 100_000, threshold = 10, runs = 20;
    IReadOnlyList<Product> data = SampleData.Generate(count);
    Console.WriteLine($"Записів: {count:N0}; поріг дефіциту: {threshold}; повторів: {runs} (після прогріву)");

    Measure("LowStock без проміжного ToList", () => ReportService.LowStockStreaming(data, threshold));
    Measure("LowStock з ToList у середині", () => ReportService.LowStockMaterializedMidway(data, threshold));
    Measure("TopByQuantity(5)", () => ReportService.TopByQuantity(data, 5));
    Measure("ByUnit", () => ReportService.ByUnit(data));

    static void Measure<T>(string title, Func<IReadOnlyList<T>> report)
    {
        int rows = report().Count; // прогрів: JIT і кеші; кількість рядків однакова в кожному повторі
        var times = new List<double>(runs);
        long allocated = 0;
        for (int i = 0; i < runs; i++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            var sw = Stopwatch.StartNew();
            report();
            sw.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - before; // виділено пам'яті за один прогін
            times.Add(sw.Elapsed.TotalMilliseconds);
        }
        Console.WriteLine($"  {title,-32} рядків {rows,6}  медіана {Median(times),6:F2} мс  мін {times.Min(),6:F2} мс  пам'ять {allocated / 1024.0,7:F0} КБ");
    }

    static double Median(List<double> values)
    {
        var sorted = values.Order().ToList();
        return sorted[sorted.Count / 2];
    }
}

static void PrintLines(IReadOnlyList<StockLine> lines)
{
    if (lines.Count == 0)
    {
        Console.WriteLine("  (порожньо)");
        return;
    }
    foreach (StockLine l in lines)
        Console.WriteLine($"  {l.Sku,-13} {l.Name,-38} {l.Quantity,6} {l.Unit}");
}

static void PrintGroups(IReadOnlyList<GroupSummary> groups)
{
    foreach (GroupSummary g in groups)
        Console.WriteLine($"  {g.Key,-9} позицій: {g.Count,3}   сумарно: {g.TotalQuantity,6}");
}

static void PrintTotals((int Items, int Total, double Average, int Max) t) =>
    Console.WriteLine($"  позицій {t.Items}, всього {t.Total}, середнє {t.Average:F2}, макс {t.Max}");

static ImportResult<ProductDto> LoadDtos(string path, Action<string>? onSkipped = null) => Path.GetExtension(path).ToLowerInvariant() switch
{
    ".json" => ProductJsonImporter.Load(path, onSkipped),
    _ => ProductCsvImporter.Load(path, onSkipped)
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

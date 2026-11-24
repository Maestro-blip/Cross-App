# CrossApp
Наскрізний проєкт з крос-платформного програмування.

Предметна область: Склад. Сутності: Product, StockBatch, Warehouse, Movement. 

Призначення: облік залишків товарів по партіях.
## Запуск
dotnet build

dotnet run --project src/Cli
## Середовище
.NET SDK 10.0, Mac OS X arm64 

## Додаткові завдання 

### Лаб1
1. ### Розміри self-contained збірок
    osx-arm64: 83M

    win-x64: 77M

2. 
    ### Запуск для виводу в json-форматі 
    dotnet run --project src/Cli -- --json 

### Лаб 2

## Запуск

dotnet build


dotnet run --project src/Cli



dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true

dotnet publish src/Cli -c Release -r osx-arm64 --self-contained false

dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true

dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -p:PublishTrimmed=true

```
└── CrossApp
    ├── CrossApp.slnx
    ├── README.md
    └── src
        ├── Cli
        │   ├── Cli.csproj
        │   └── Program.cs
        └── Core
            ├── Core.csproj
            ├── Domain
            ├── Dto
            ├── EnvironmentInfo.cs
            └── Storage
```

| RID       | Режим               | Розмір | Потрібен Runtime | Кількість файлів |
| --------- | ------------------- | ------ | ---------------- | ---------------- |
| osx-arm64 | self-contained      | 83 Mb  | ні               | 193              |
| osx-arm64 | framework-dependent | 172 Kb | так              | 7                |
| osx-arm64 | PublishSingleFile   | 76 Mb  | ні               | 3                |
| osx-arm64 | PublishTrimmed      | 20 Mb  | ні               | 31               |

### Лаб 3

## Запуск

dotnet run --project src/Cli -f net10.0

dotnet run --project src/Cli -f net10.0 -- data/sample.csv

dotnet run --project src/Cli -f net10.0 -- data/sample.json

dotnet run --project src/Cli -f net10.0 -- --catalog data/catalog.csv

## Формат файлу data/sample.csv

- Роздільник: `;` (константа `Separator` у `ProductCsvImporter`)
- Колонки: `id;sku;name;unit;quantity`
- Заголовок у першому рядку необов'язковий
- Кодування: UTF-8
- Рядки 12-14 пошкоджені навмисно (тестові дані): мало колонок, нечислова кількість, порожній SKU

## Додаткові завдання

1. `data/sample.json` – JSON-імпортер `ProductJsonImporter`, Cli обирає імпортер за розширенням файлу.
2. `data/catalog.csv` – різнорідні рядки: `P;id;sku;name;unit;quantity` – товари, `W;id;name;city` – склади.
3. Статистика імпорту одним рядком: усього / прийнято / пропущено / % помилок.

### Лаб 4

Доменна модель: сутності, інваріанти, інкапсуляція.

## Запуск

dotnet run --project src/Cli -f net10.0 -- --domain

dotnet run --project src/Cli -f net10.0 -- --domain-import data/domain.json

dotnet run --project src/Cli -f net10.0 -- --warehouse

dotnet run --project src/Cli -f net10.0 -- --movement

Інші режими: `--env` / `--json` (лаб 1-2), `--catalog [файл]` і `[файл.csv|файл.json]` (лаб 3).

```
src/Core/
├── Dto/                     ProductDto, WarehouseDto, ImportResult – формат даних
├── Domain/
│   ├── Product.cs           товар із залишком: Create, RegisterArrival, Issue, ToDto/FromDto
│   ├── Warehouse.cs         склад із місткістю
│   └── Movement.cs          накладна: MovementStatus, MovementLine, переходи станів
├── Import/
│   └── ProductDomainImporter.cs   ImportResult<ProductDto> -> ImportResult<Product>
└── Services/
    └── StockService.cs      правило, що охоплює Product + Warehouse
```

## Сутності

- `Product` – товар із залишком. Залишок у приватному полі `_quantity`, змінюється лише через `RegisterArrival` і `Issue`.
- `Warehouse` – склад з місткістю `Capacity` (максимальна сумарна кількість одиниць).
- `Movement` – накладна приходу/видачі з рядками `MovementLine(Product, Amount)` і станом `MovementStatus`.
- `StockService` – знає, який товар на якому складі, і перевіряє місткість.

## Інваріанти

| Правило | Тип винятку | Де перевіряється |
| --- | --- | --- |
| Id товару не порожній | ArgumentException | `Product.Create` |
| SKU не порожній (зберігається у верхньому регістрі) | ArgumentException | `Product.Create` |
| Назва та одиниця виміру не порожні | ArgumentException | `Product.Create` |
| Початковий залишок не від'ємний | ArgumentOutOfRangeException | `Product.Create` |
| Кількість приходу / видачі > 0 | ArgumentOutOfRangeException | `Product.RegisterArrival`, `Product.Issue` |
| Не можна видати більше, ніж є на залишку | DomainException | `Product.Issue` |
| `FromDto` проходить ті самі перевірки | як у `Create` | `Product.FromDto` -> `Create` |
| Id і назва складу не порожні, місткість > 0 | ArgumentException / ArgumentOutOfRangeException | `Warehouse.Create` |
| Сумарний залишок на складі ≤ місткості складу | DomainException | `StockService.Place`, `StockService.RegisterArrival` |
| Товар розміщено лише на одному складі | DomainException | `StockService.Place` |
| Прихід через сервіс лише для розміщеного товару | DomainException | `StockService.RegisterArrival` |
| Номер накладної не порожній | ArgumentException | `Movement.Create` |
| Кількість у рядку > 0, товар у накладній не повторюється | ArgumentOutOfRangeException / DomainException | `Movement.AddLine` |
| Рядки додаються лише в стані Draft | DomainException | `Movement.AddLine` |
| Порожню накладну провести не можна | DomainException | `Movement.Post` |
| Дозволені лише переходи Draft->Posted, Draft->Cancelled, Posted->Cancelled | DomainException | `Movement.CanTransition` (switch expression) |
| Проведення «все або нічого»: перевитрата за будь-яким рядком блокує всю накладну | DomainException | `Movement.Post` / `Movement.Cancel` |

Перевірка: у `src/Core/Domain` і `src/Core/Services` немає `Console.` і `File.`; `product.Quantity = -5` не компілюється (CS0200).

## Додаткові завдання

1. `ProductDomainImporter.Convert` – бере `ImportResult<ProductDto>` і повертає `ImportResult<Product>`: сутності + рядки, що не пройшли інваріанти (`--domain-import`, дані `data/domain.json`).
2. Інваріант двох сутностей: «сумарний залишок товарів на складі не перевищує місткість складу» (`StockService`, `--warehouse`). Ні `Product`, ні `Warehouse` окремо не мають даних для перевірки: товар не знає про інші товари складу, склад не зберігає залишки. Тому правило винесено в сервіс, який бачить обидві сутності; на тижні 5 він отримуватиме їх зі сховищ.
3. `enum MovementStatus { Draft, Posted, Cancelled }` і таблиця допустимих переходів через switch expression по кортежу `(from, to)` у `Movement.CanTransition` (`--movement`). Скасування проведеної накладної повертає залишки назад.

### Лаб 5: сервісний шар, два сховища, ручний DI

- Інтерфейс сховища: `ICatalogStore` (`src/Core/Abstraction`): `GetAll`, `GetById`, `Add`, `Update`, `Delete`.
- Реалізації: `InMemoryCatalogStore` (словник у пам'яті, стартові дані з `SampleData`) і `FileCatalogStore` (JSON-файл `data/catalog.json` поруч зі зібраною програмою, кеш + запис після кожної зміни).
- Сервіс: `CatalogService` (`Add`, `Receive`, `Issue`, `All`, `Find`), отримує `ICatalogStore` через конструктор.
- Composition root: `src/Cli/Program.cs` (`RunService`) + `src/Cli/StoreFactory.cs`.
- Додатково: декоратор `CachingCatalogStore` (`--cache`), пошук `CatalogService.Search(Func<Product, bool>)`, фабрика `StoreFactory.Create(args)`.

```
dotnet run --project src/Cli --framework net10.0            # InMemoryCatalogStore
dotnet run --project src/Cli --framework net10.0 -- --file  # FileCatalogStore
dotnet run --project src/Cli --framework net10.0 -- --cache --file  # FileCatalogStore у декораторі-кеші
dotnet run --project src/Cli --framework net10.0 -- --import # імпорт з лаби 3
```

### Лаб 6: доменні події, делегати Action/Func, лямбди

- Подія: `CatalogService.Changed` типу `EventHandler<CatalogChangedEventArgs>`; піднімається приватним `OnChanged` лише після успішної зміни стану (`Add`, `Receive`, `Issue`, `Remove`).
- Дані події: `src/Core/Events/CatalogChangeKind.cs` (`Added`, `Received`, `Issued`, `Removed`) і `CatalogChangedEventArgs.cs` (`Id`, `Kind`, `Delta`, `QuantityAfter`, `At`).
- Підписники в `src/Cli/Program.cs` (`RunEvents`): локальна функція `OnCatalogChanged` (консоль + `logs/app.log`) і лямбда-лічильник із замиканням над `changes`; показано `-=` і невдалу відписку «новою» лямбдою.
- Журнал: `Path.Combine(AppContext.BaseDirectory, "logs", "app.log")` → `src/Cli/bin/Debug/net10.0/logs/app.log`, каталог створюється кодом.
- `Func<Product, bool>`: `CatalogService.FindAll` (у лаб 5 — `Search`).
- Core не пише в консоль і у файл журналу (`grep -rn "Console\|logs" src/Core --include="*.cs"` — порожньо).
- Додатково: `INotificationSink` (Core) + `ConsoleSink`, `FileSink`, `CompositeSink` (`src/Cli/Notifications`), передаються в `CatalogService` через конструктор (`--sinks`); `Action<string> onSkipped` в `ProductCsvImporter.Load` / `ProductJsonImporter.Load` для звіту про пропущені рядки.

```
dotnet run --project src/Cli -f net10.0                    # події (за замовчуванням), InMemory
dotnet run --project src/Cli -f net10.0 -- --events --file # події, FileCatalogStore
dotnet run --project src/Cli -f net10.0 -- --sinks         # INotificationSink замість події
dotnet run --project src/Cli -f net10.0 -- data/sample.csv # імпорт з Action<string>
dotnet run --project src/Cli -f net10.0 -- --service       # сценарій лаб 5
```

### Лаб 7: звіти через LINQ

- DTO: `src/Core/Reports/StockLine.cs`, `GroupSummary.cs`, `ProductActivity.cs` (record).
- Запити: `src/Core/Reports/ReportService.cs` — приймають `IEnumerable<Product>`, повертають `IReadOnlyList<DTO>`; у `Program.cs` немає жодного `Where`/`GroupBy`, лише вибір підкоманди і вивід.
- `SampleData` розширено до 20 записів: 6 одиниць виміру, 7 позицій із залишком < 10.

| Метод | Питання | Оператори |
| --- | --- | --- |
| `LowStock(source, threshold)` | яких товарів менше за поріг? | Where, OrderBy, ThenBy, Select, ToList |
| `TopByQuantity(source, n)` | які n позицій мають найбільший залишок? | TopBy (OrderByDescending, ThenBy, Take), Select |
| `ByUnit(source)` | скільки позицій і одиниць у кожній одиниці виміру? | GroupBy, Count, Sum, Select, OrderByDescending |
| `Totals(source)` | скільки всього, середній і максимальний залишок? | ToList, Count, Sum, Average, Max |
| `LowStockDeferred(source, threshold)` | демо відкладеного виконання | Where, Select (без ToList) |
| `Activity` / `MostActive` | скільки операцій було по кожному товару? | GroupJoin, Count, Sum, TopBy |

```
dotnet run --project src/Cli -f net10.0 -- report-top       # топ-5 за залишком
dotnet run --project src/Cli -f net10.0 -- report-low       # дефіцит < 10
dotnet run --project src/Cli -f net10.0 -- report-groups    # групи за одиницею
dotnet run --project src/Cli -f net10.0 -- report-sum       # агрегати
dotnet run --project src/Cli -f net10.0 -- report-deferred  # демо відкладеного виконання
dotnet run --project src/Cli -f net10.0 -- report-empty     # звіти на порожньому джерелі
dotnet run --project src/Cli -f net10.0 -- report-query     # дод. 3: синтаксис запитів
dotnet run --project src/Cli -f net10.0 -- report-join      # дод. 1: GroupJoin товарів і журналу подій
dotnet run --project src/Cli -f net10.0 -- report-bench     # дод. 4: Stopwatch на 100 000 записів
dotnet run --project src/Cli -f net10.0 -- report-top --file  # прапорець не стає назвою звіту
```

**Коли виконується запит.** `ReportService.LowStockDeferred` лише будує ланцюжок `Where(...).Select(...)` і повертає його, не перебираючи джерело. Запит виконується щоразу, коли результат перебирають: у `foreach`, `Count()`, `ToList()`. У демо `report-deferred` перший `query.Count()` проходить список із 20 товарів і дає 7; після `source.Add(Скоба, 3)` другий `query.Count()` проходить той самий, уже змінений список ще раз і дає 8. `query.ToList()` фіксує результат: після `source.Clear()` `query.Count()` = 0, а `snapshot.Count` лишається 8. Решта методів `ReportService` закінчуються `ToList()`, тому звіт виконується один раз у момент виклику, і повторне читання результату в Cli запит не перезапускає.

**Синтаксис запитів vs синтаксис методів** (`ByUnitQuery` vs `ByUnit`, `report-query` перевіряє, що результати однакові):

```csharp
// методи
source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
      .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
      .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
// запит
from product in source
group product by product.Unit into g
let total = g.Sum(p => p.Quantity)
orderby total descending, g.Key
select new GroupSummary(g.Key, g.Count(), total)
```

Для групування синтаксис запитів читається природніше: `group ... by ... into g` і `let` дають ім'я проміжному значенню (сума рахується один раз і використовується і в `orderby`, і в `select`). Але в ньому не можна передати `StringComparer` у `group by` і `orderby` (без `IgnoreCase` "шт" і "ШТ" стали б різними групами), а `Take`, `Count`, `ToList` однаково доводиться дописувати методами. Тому в основних звітах лишено ланцюжок методів: він читається зліва направо і дає повний контроль над порівнянням рядків.

**Додатково:** метод-розширення `TopBy<T>(this IEnumerable<T>, Func<T,int>, int, Func<T,string>)` у `src/Core/Reports/EnumerableExtensions.cs` використовується у `TopByQuantity` і `MostActive`. Бенчмарк `report-bench` (100 000 записів, медіана з 20 прогонів): `LowStock` без проміжного `ToList` ≈ 0,57 мс / 156 КБ, з `ToList` після кожного кроку ≈ 0,68 мс / 969 КБ — час майже той самий, але пам'яті в 6 разів більше через повні проміжні списки.

### Лаб 8: async/await, скасування, доменні винятки, unit-тести Core

- `src/Core/DomainException.cs` — порушення бізнес-правила (видача понад залишок, немає товару, кількість у постачанні = 0, заборонений перехід накладної). Замінив `InvalidOperationException` у `Product`, `Movement`, `CatalogService`, `StockService`. Перевірки аргументів лишились `ArgumentException` / `ArgumentOutOfRangeException`.
- `src/Core/StorageException.cs` — технічна помилка формату (зіпсований JSON), початковий `JsonException` у `InnerException`.
- `src/Core/Abstraction/ICatalogImporter.cs` — `Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct)`.
- `src/Core/Storage/JsonCatalogImporter.cs` — `FileStream(useAsync: true)` + `JsonSerializer.DeserializeAsync`, `ct.ThrowIfCancellationRequested()` у циклі, поганий запис → `DomainException("Запис N: ...")`.
- `CatalogService.ImportAsync(importer, path, ct, progress)` — ідемпотентний (наявні id пропускаються), подія `Changed` на кожен доданий запис.
- Cli: `--import-async` — top-level `await`, Ctrl+C через `Console.CancelKeyPress`, окремі `catch` для скасування / таймауту / доменної / файлової помилки.
- Тести: `tests/Core.Tests` (xUnit, посилається лише на Core) — 15 методів / 20 запусків.
- Дані: `data/supplies.json` (17 постачань), `data/supplies-bad.json` (запис 3 з кількістю 0), `data/supplies-broken.json` (кількість — рядок, не JSON-число).

**Запуск тестів**

```
dotnet test
dotnet test --logger "console;verbosity=normal"     # список тестів
dotnet test --collect:"XPlat Code Coverage"          # покриття (дод. 4)
```

**Імпорт і перевірка скасування**

```
dotnet run --project src/Cli -f net10.0 -- --import-async                            # Імпортовано записів: 17
dotnet run --project src/Cli -f net10.0 -- --import-async data/supplies-bad.json     # Дані відхилено: Запис 3 ...
dotnet run --project src/Cli -f net10.0 -- --import-async data/supplies-broken.json  # Помилка файлу: ... не є коректним JSON
dotnet run --project src/Cli -f net10.0 -- --import-async --file                     # другий запуск — 0 (ідемпотентність)

dotnet run --project src/Cli -f net10.0 -- --gen-big 500000                          # data/big.json, ~66 МБ (у .gitignore)
dotnet run --project src/Cli -f net10.0 -- --import-async data/big.json              # натиснути Ctrl+C → Імпорт скасовано.
dotnet run --project src/Cli -f net10.0 -- --import-async data/big.json --cancel-after 200   # cts.CancelAfter → Імпорт скасовано.
dotnet run --project src/Cli -f net10.0 -- --import-async data/big.json --timeout 200        # дод. 2: Таймаут: ...
dotnet run --project src/Cli -f net10.0 -- --import-async data/big.json --stream --cancel-after 200  # дод. 1
```

**Додатково:** потоковий імпорт `JsonCatalogImporter.StreamAsync` (`IAsyncEnumerable<Product>`, `[EnumeratorCancellation]`) + `CatalogService.ImportStreamAsync` (`--stream`); зв'язані токени `CreateLinkedTokenSource(ct, timeout)` у перевантаженні `ImportAsync(..., TimeSpan timeout, ...)` — таймаут стає `TimeoutException`, скасування користувачем лишається `OperationCanceledException`; `IProgress<int>` + `ConsoleProgress` у Cli; покриття — 26,5 % рядків Core загалом, `CatalogService` 73 %, `JsonCatalogImporter` 49 %.

Самоперевірка: `grep -rnE '\.Result\b|\.Wait\(\)|async void' src tests` — порожньо; тести не читають `data/`; єдиний тест із диском пише в `Path.GetTempPath()` і видаляє файл у `finally`.

### Лаб 9: шляхи, JSON, конфігурація, secrets

- `src/Core/Configuration/AppOptions.cs` — `AppOptions` (`EnvironmentName`, `Storage`, `Logging`, `Warehouse`), `StorageOptions.ResolveFullPath(baseDir)` через `Path.Combine` + `Path.GetFullPath`, каталог створюється автоматично.
- `src/Core/Storage/CatalogJsonFile.cs` — спільний формат JSON (camelCase, кирилиця без `\uXXXX`), атомарний запис `.tmp` + `File.Move`, зіпсований файл → `StorageException` з номером рядка / запису.
- `FileCatalogStore` — шлях приходить із конфігурації, читання/запис через `CatalogJsonFile`; `ICatalogStore` лишився синхронним.
- `src/Cli/AppConfig.cs` — `ConfigurationBuilder` з `AppContext.BaseDirectory`; `config.Get<AppOptions>()`; `CROSSAPP_DATA_PATH`; secret `config["Integration:ApiKey"]` (друкується лише довжина).
- `src/Cli/AppLog.cs` — консольний журнал із рівнем `Logging:MinimumLevel`.
- Пакети `Microsoft.Extensions.Configuration.*` 10.x — лише в `src/Cli`, Core про них не знає.

**Профілі** (обирається змінною `CROSSAPP_ENVIRONMENT`, за замовчуванням `Development`)

| Профіль | Файл | Лог | Де дані |
|---|---|---|---|
| Development | `appsettings.Development.json` | `Debug` | `<bin>/data/items.json` (поруч зі збіркою) |
| Production | `appsettings.Production.json` | `Warning` | `LocalApplicationData/CrossApp/items.json` (macOS: `~/Library/Application Support`, Windows: `%LOCALAPPDATA%`, Linux: `~/.local/share`) |

**Порядок джерел** (пізніше перекриває раніше): `appsettings.json` → `appsettings.{Environment}.json` → user-secrets → змінні `CROSSAPP_*` → `--Ключ=значення` → окремо `CROSSAPP_DATA_PATH` (перекриває `Storage:DataPath`).

**Ключі**

| Ключ | За замовчуванням | Змінна середовища | Призначення |
|---|---|---|---|
| `EnvironmentName` | `Development` | `CROSSAPP_EnvironmentName` | назва профілю (для виводу) |
| `Logging:MinimumLevel` | `Information` | `CROSSAPP_Logging__MinimumLevel` | `Debug` / `Information` / `Warning` / `Error` |
| `Storage:Root` | `App` | `CROSSAPP_Storage__Root` | `App` = `AppContext.BaseDirectory`, `LocalAppData` = системний каталог даних |
| `Storage:DataPath` | `data` | `CROSSAPP_Storage__DataPath` або `CROSSAPP_DATA_PATH` | відносний (від Root) або абсолютний каталог |
| `Storage:FileName` | `items.json` | `CROSSAPP_Storage__FileName` | ім'я файлу каталогу |
| `Warehouse:DefaultUnit` | `шт` | `CROSSAPP_Warehouse__DefaultUnit` | одиниця для порожнього `unit` під час імпорту |
| `Integration:ApiKey` | — (secret) | `CROSSAPP_Integration__ApiKey` | лише user-secrets / env, **не** в appsettings |

**Secret**

```
dotnet user-secrets init --project src/Cli                                  # уже зроблено: UserSecretsId у Cli.csproj
dotnet user-secrets set "Integration:ApiKey" "local-dev-key" --project src/Cli
dotnet user-secrets list --project src/Cli
```

Файл: macOS / Linux `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`, Windows `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` — поза репозиторієм.

**Запуск**

```
dotnet run --project src/Cli -f net10.0 -- config                    # ефективна конфігурація
dotnet run --project src/Cli -f net10.0 -- export                    # 20 товарів → data/items.json
dotnet run --project src/Cli -f net10.0 -- export --out copy.json    # у інший файл поруч
dotnet run --project src/Cli -f net10.0 -- import                    # прочитати власний файл даних
dotnet run --project src/Cli -f net10.0 -- import data/items-import.json   # додати 3 товари (порожній unit → шт)
dotnet run --project src/Cli -f net10.0 -- import data/items-broken.json   # [err] ... пошкоджено, рядок 3
dotnet run --project src/Cli -f net10.0 -- export --Storage:DataPath=tmp   # перевизначення аргументом
cd src/Cli && dotnet run -f net10.0 -- export                         # той самий файл, що й з кореня
```

bash / zsh:

```
CROSSAPP_DATA_PATH=/tmp/crossapp-data dotnet run --project src/Cli -f net10.0 -- export
CROSSAPP_ENVIRONMENT=Production dotnet run --project src/Cli -f net10.0 -- config
CROSSAPP_Integration__ApiKey=from-env dotnet run --project src/Cli -f net10.0 -- export
export CROSSAPP_Storage__FileName=stock.json   # до кінця сесії терміналу
```

PowerShell:

```
$env:CROSSAPP_DATA_PATH = "C:\Users\st\data"; dotnet run --project src/Cli -f net10.0 -- export
$env:CROSSAPP_ENVIRONMENT = "Production"
$env:CROSSAPP_Integration__ApiKey = "from-env"
Remove-Item Env:CROSSAPP_DATA_PATH
```

Самоперевірка: `git grep -nF 'C:\' -- src` і `git grep local-dev-key` — порожньо; видалений `bin/.../data` створюється заново; `git status` після `user-secrets set` чистий; тести — 21 метод / 26 запусків.

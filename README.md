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
| Не можна видати більше, ніж є на залишку | InvalidOperationException | `Product.Issue` |
| `FromDto` проходить ті самі перевірки | як у `Create` | `Product.FromDto` -> `Create` |
| Id і назва складу не порожні, місткість > 0 | ArgumentException / ArgumentOutOfRangeException | `Warehouse.Create` |
| Сумарний залишок на складі ≤ місткості складу | InvalidOperationException | `StockService.Place`, `StockService.RegisterArrival` |
| Товар розміщено лише на одному складі | InvalidOperationException | `StockService.Place` |
| Прихід через сервіс лише для розміщеного товару | InvalidOperationException | `StockService.RegisterArrival` |
| Номер накладної не порожній | ArgumentException | `Movement.Create` |
| Кількість у рядку > 0, товар у накладній не повторюється | ArgumentOutOfRangeException / InvalidOperationException | `Movement.AddLine` |
| Рядки додаються лише в стані Draft | InvalidOperationException | `Movement.AddLine` |
| Порожню накладну провести не можна | InvalidOperationException | `Movement.Post` |
| Дозволені лише переходи Draft->Posted, Draft->Cancelled, Posted->Cancelled | InvalidOperationException | `Movement.CanTransition` (switch expression) |
| Проведення «все або нічого»: перевитрата за будь-яким рядком блокує всю накладну | InvalidOperationException | `Movement.Post` / `Movement.Cancel` |

Перевірка: у `src/Core/Domain` і `src/Core/Services` немає `Console.` і `File.`; `product.Quantity = -5` не компілюється (CS0200).

## Додаткові завдання

1. `ProductDomainImporter.Convert` – бере `ImportResult<ProductDto>` і повертає `ImportResult<Product>`: сутності + рядки, що не пройшли інваріанти (`--domain-import`, дані `data/domain.json`).
2. Інваріант двох сутностей: «сумарний залишок товарів на складі не перевищує місткість складу» (`StockService`, `--warehouse`). Ні `Product`, ні `Warehouse` окремо не мають даних для перевірки: товар не знає про інші товари складу, склад не зберігає залишки. Тому правило винесено в сервіс, який бачить обидві сутності; на тижні 5 він отримуватиме їх зі сховищ.
3. `enum MovementStatus { Draft, Posted, Cancelled }` і таблиця допустимих переходів через switch expression по кортежу `(from, to)` у `Movement.CanTransition` (`--movement`). Скасування проведеної накладної повертає залишки назад.

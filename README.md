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


| RID       | Режим               | Розмір | Потрібен Runtime | Кількість файлів |
| --------- | ------------------- | ------ | ---------------- | ---------------- |
| osx-arm64 | self-contained      | 83 Mb  | ні               | 193              |
| osx-arm64 | framework-dependent | 172 Kb | так              | 7                |
| osx-arm64 | PublishSingleFile   | 76 Mb  | ні               | 3                |
| osx-arm64 | PublishTrimmed      | 20 Mb  | ні               | 31               |
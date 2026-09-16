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


1. ### Розміри self-contained збірок
    osx-arm64: 83M

    win-x64: 77M

2. 
    ### Запуск для виводу в json-форматі 
    dotnet run --project src/Cli -- --json 
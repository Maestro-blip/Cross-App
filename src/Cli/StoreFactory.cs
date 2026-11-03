using Core;
using Core.Abstraction;
using Core.Storage;

// Додаткове завдання 3: вибір сховища за аргументами винесено з Program.cs.
// Фабрика лежить у Cli, бо вона частина composition root: лише тут відомі конкретні класи.
//   (без аргументів)  → InMemoryCatalogStore
//   --file            → FileCatalogStore
//   --cache           → будь-яке з них, загорнуте в CachingCatalogStore
public static class StoreFactory
{
    public static string DataPath { get; } = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

    public static ICatalogStore Create(string[] args)
    {
        ICatalogStore store = args.Contains("--file")
            ? new FileCatalogStore(DataPath)
            : new InMemoryCatalogStore(SampleData.Products());

        return args.Contains("--cache") ? new CachingCatalogStore(store) : store;
    }
}

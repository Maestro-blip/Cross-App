using Core;
using Core.Abstraction;
using Core.Storage;

// Додаткове завдання 3 (лаб 5): вибір сховища за аргументами винесено з Program.cs.
// Фабрика лежить у Cli, бо вона частина composition root: лише тут відомі конкретні класи.
//   (без аргументів)  → InMemoryCatalogStore
//   --file            → FileCatalogStore
//   --cache           → будь-яке з них, загорнуте в CachingCatalogStore
// Лаб 9: шлях до файлу більше не зашитий у код — Program передає його з конфігурації (UseDataPath).
public static class StoreFactory
{
    public static string DataPath { get; private set; } =
        Path.Combine(AppContext.BaseDirectory, "data", "items.json");

    public static void UseDataPath(string fullPath) => DataPath = fullPath;

    public static ICatalogStore Create(string[] args)
    {
        ICatalogStore store = args.Contains("--file")
            ? new FileCatalogStore(DataPath)
            : new InMemoryCatalogStore(SampleData.Products());

        return args.Contains("--cache") ? new CachingCatalogStore(store) : store;
    }
}

namespace Core.Configuration;

// Лаб 9: налаштування застосунку. Звичайні класи без жодного знання про JSON-файли чи змінні середовища:
// Cli збирає IConfiguration і розкладає ключі в ці класи через config.Get<AppOptions>().
// Назва властивості = назва ключа: Storage:DataPath → AppOptions.Storage.DataPath.
public sealed class AppOptions
{
    public string EnvironmentName { get; init; } = "Development";
    public StorageOptions Storage { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public WarehouseOptions Warehouse { get; init; } = new();
}

public sealed class StorageOptions
{
    // Відносний шлях рахується від базового каталогу (див. Root), абсолютний — використовується як є.
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "items.json";

    // Від чого рахувати відносний DataPath:
    //   "App"          — AppContext.BaseDirectory (каталог зібраної програми, bin/...);
    //   "LocalAppData" — системний каталог локальних даних користувача.
    public string Root { get; init; } = "App";

    public string ResolveBaseDirectory(string appBaseDir) =>
        string.Equals(Root, "LocalAppData", StringComparison.OrdinalIgnoreCase)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : appBaseDir;

    // Повний шлях до файлу даних. Каталог створюється тут, тож видалений data/ не дає винятку.
    public string ResolveFullPath(string baseDir)
    {
        var root = Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);
        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}

public sealed class LoggingOptions
{
    // Debug < Information < Warning < Error
    public string MinimumLevel { get; init; } = "Information";
}

// Ключ варіанта «Склад»: одиниця виміру, якою підставляється порожнє поле unit під час імпорту.
public sealed class WarehouseOptions
{
    public string DefaultUnit { get; init; } = "шт";
}

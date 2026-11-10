using Core.Events;

// Спільне для всіх одержувачів у Cli: шлях до журналу і формат рядка події.
public static class ChangeLog
{
    // logs/app.log поруч зі зібраною програмою; каталог створює EnsureDirectory.
    public static string Path { get; } = System.IO.Path.Combine(AppContext.BaseDirectory, "logs", "app.log");

    public static void EnsureDirectory() =>
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!); // якщо каталог є — нічого не робить

    // 14:20:31 Added    9f2c1ab4  +50 → залишок 50
    public static string Format(CatalogChangedEventArgs e) =>
        $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}";
}

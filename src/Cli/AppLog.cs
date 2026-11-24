// Лаб 9: найпростіший журнал у консоль із рівнем із конфігурації (Logging:MinimumLevel).
// Development — Debug (видно все), Production — Warning (лише попередження й помилки).
public enum LogLevel { Debug, Information, Warning, Error }

public static class AppLog
{
    public static LogLevel MinimumLevel { get; private set; } = LogLevel.Information;

    public static void Configure(string level) =>
        MinimumLevel = Enum.TryParse(level, ignoreCase: true, out LogLevel parsed) ? parsed : LogLevel.Information;

    public static void Debug(string message) => Write(LogLevel.Debug, "dbg", message);
    public static void Info(string message) => Write(LogLevel.Information, "inf", message);
    public static void Warn(string message) => Write(LogLevel.Warning, "wrn", message);
    public static void Error(string message) => Write(LogLevel.Error, "err", message);

    private static void Write(LogLevel level, string tag, string message)
    {
        if (level >= MinimumLevel)
            Console.WriteLine($"[{tag}] {message}");
    }
}

using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

// Лаб 9: збирання конфігурації. Пізніше додане джерело перекриває раніше додане:
//   appsettings.json → appsettings.{Environment}.json → user-secrets → змінні CROSSAPP_* → --Ключ=значення
// Файли шукаються в AppContext.BaseDirectory (bin/...), а не в поточному каталозі,
// тому запуск із кореня solution і з src/Cli дає однаковий результат.
public sealed record AppSettings(AppOptions Options, string DataFile, string? ApiKey);

public static class AppConfig
{
    public const string Prefix = "CROSSAPP_";

    public static AppSettings Load(string[] args)
    {
        var envName = Environment.GetEnvironmentVariable(Prefix + "ENVIRONMENT") ?? "Development";

        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: Prefix)       // CROSSAPP_Storage__DataPath → Storage:DataPath
            .AddCommandLine(OverrideArgs(args))             // --Storage:DataPath=tmp
            .Build();

        var options = config.Get<AppOptions>() ?? new AppOptions();

        // Проста змінна CROSSAPP_DATA_PATH перекриває розділ Storage (у тому числі CROSSAPP_Storage__DataPath).
        var simplePath = Environment.GetEnvironmentVariable(Prefix + "DATA_PATH");
        var storage = new StorageOptions
        {
            DataPath = string.IsNullOrWhiteSpace(simplePath) ? options.Storage.DataPath : simplePath,
            FileName = options.Storage.FileName,
            Root = options.Storage.Root
        };

        var baseDir = storage.ResolveBaseDirectory(AppContext.BaseDirectory);
        var dataFile = storage.ResolveFullPath(baseDir);

        // Secret читаємо як звичайний ключ. Звідки він прийшов (user-secrets чи env) — коду байдуже.
        var apiKey = config["Integration:ApiKey"];

        return new AppSettings(options, dataFile, apiKey);
    }

    // У провайдер командного рядка передаємо лише явні перевизначення виду --Ключ=значення,
    // щоб команди на кшталт "--file" чи шляхи до файлів не ставали ключами конфігурації.
    private static string[] OverrideArgs(string[] args) =>
        args.Where(a => a.StartsWith("--", StringComparison.Ordinal) && a.Contains('=')).ToArray();
}

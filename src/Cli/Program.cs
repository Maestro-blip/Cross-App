using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.Json;

if (args.Length > 0 && args[0] == "--json")
{
    var appInfo = new Dictionary<string, object?>
    {
        ["title"] = "CrossApp – практикум з крос-платформного програмування",
        ["student"] = "Студент: Кучак Володимир, ФЕІ-33",
        ["chapter"] = new string('-', 52),
        ["ОС (OSDescription)"] = RuntimeInformation.OSDescription,
        ["ОС (Environment)"] = Environment.OSVersion,
        ["Архітектура процесу"] = RuntimeInformation.ProcessArchitecture,
        ["Версія.NET(CLR)"] = Environment.Version,
        ["Runtime"] = RuntimeInformation.FrameworkDescription,
        ["Каталог застосунку"] = AppContext.BaseDirectory,
        ["Поточний каталог"] = Environment.CurrentDirectory,
        ["chapter2"] = new string('-', 52),
        ["Предметна область"] = "Склад (товари, партії, залишки, переміщення)"
    };

    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    string jsonOut = JsonSerializer.Serialize(appInfo, options);
    Console.WriteLine(jsonOut);
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Кучак Володимир, ФЕІ-33");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription) : {RuntimeInformation.OSDescription}");
    Console.WriteLine($"ОС (Environment) : {Environment.OSVersion}");
    Console.WriteLine($"Архітектура процесу : {RuntimeInformation.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR) : {Environment.Version}");
    Console.WriteLine($"Runtime : {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"Каталог застосунку : {AppContext.BaseDirectory}");
    Console.WriteLine($"Поточний каталог : {Environment.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine("Предметна область: Склад (товари, партії, залишки, переміщення)");
}

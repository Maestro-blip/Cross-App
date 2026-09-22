using Core;

EnvironmentReport data = EnvironmentInfo.Collect();

Console.WriteLine("CrossApp - інформація про середовище");
Console.WriteLine(new string('_', 52));
Console.WriteLine($"ОС {data.OsDescription}");
Console.WriteLine($"Runtime {data.FrameworkDescription}");
Console.WriteLine($"Архітектура {data.ProcessArchitecture}");
Console.WriteLine($"RID(визначено) {data.DetectedRid}");
Console.WriteLine($"RID(.NET) {data.ReportedRid}");
Console.WriteLine($"Каталог {data.BaseDirectory}");
Console.WriteLine(new string('-', 52));
Console.WriteLine(data.TMF);

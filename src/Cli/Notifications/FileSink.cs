using Core.Abstraction;
using Core.Events;

// Додаткове завдання 1: одержувач повідомлень, що дописує рядок у файл журналу.
public sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    public void Notify(CatalogChangedEventArgs change)
    {
        // Збій запису журналу не повинен ламати вже виконану бізнес-операцію.
        try
        {
            File.AppendAllText(_path, $"[sink] {ChangeLog.Format(change)}{Environment.NewLine}");
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Журнал недоступний: {ex.Message}");
        }
    }
}

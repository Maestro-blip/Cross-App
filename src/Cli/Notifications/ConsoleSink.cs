using Core.Abstraction;
using Core.Events;

// Додаткове завдання 1: одержувач повідомлень, що пише в консоль.
public sealed class ConsoleSink : INotificationSink
{
    public void Notify(CatalogChangedEventArgs change) =>
        Console.WriteLine($"[sink] {ChangeLog.Format(change)}");
}

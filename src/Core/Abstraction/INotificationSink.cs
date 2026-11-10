using Core.Events;

namespace Core.Abstraction;

// Додаткове завдання 1 (лаб 6): альтернатива події — одержувач повідомлень як залежність сервісу.
// Реалізації (консоль, файл) живуть у Cli; Core знає лише цей контракт.
public interface INotificationSink
{
    void Notify(CatalogChangedEventArgs change);
}

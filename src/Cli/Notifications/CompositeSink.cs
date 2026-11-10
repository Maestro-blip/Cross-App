using Core.Abstraction;
using Core.Events;

// Сервіс приймає один INotificationSink; щоб писати і в консоль, і у файл, Cli передає компонувальник.
public sealed class CompositeSink(params INotificationSink[] sinks) : INotificationSink
{
    public void Notify(CatalogChangedEventArgs change)
    {
        foreach (INotificationSink sink in sinks)
            sink.Notify(change);
    }
}

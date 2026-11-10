namespace Core.Events;

// Дані події: що змінилось і який стан після зміни. Властивості лише для читання —
// усі підписники отримують той самий об'єкт, і жоден не може змінити його для наступних.
public sealed class CatalogChangedEventArgs(string id, CatalogChangeKind kind, int delta, int quantityAfter) : EventArgs
{
    public string Id { get; } = id;
    public CatalogChangeKind Kind { get; } = kind;
    public int Delta { get; } = delta;               // на скільки змінилась кількість
    public int QuantityAfter { get; } = quantityAfter;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}

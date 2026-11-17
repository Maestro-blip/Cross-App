namespace Core;

// Лаб 8: порушення бізнес-правила складу (видати більше за залишок, кількість у постачанні = 0,
// заборонений перехід накладної). Відрізняється від:
//   ArgumentException      — некоректний аргумент методу (від'ємна кількість, порожній SKU);
//   StorageException / IO  — збій інфраструктури (файл не знайдено, зіпсований JSON).
// sealed — від нього не наслідуються, тип помилки однозначний.
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    // inner — початкова причина (наприклад, ArgumentException з Product.Create), щоб не загубити деталі.
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

namespace Core;

// Лаб 8: технічна помилка сховища/файлу (не JSON, пошкоджена структура).
// Це НЕ доменна помилка: бізнес-правила не порушено, зламався формат даних.
// Початковий виняток (JsonException) зберігається в InnerException — там номер рядка і позиція.
public sealed class StorageException(string message, Exception inner) : Exception(message, inner);

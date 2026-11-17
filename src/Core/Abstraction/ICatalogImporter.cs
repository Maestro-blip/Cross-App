using Core.Domain;

namespace Core.Abstraction;

// Лаб 8: асинхронний контракт імпорту постачань.
// Task<...> — результат буде пізніше (після читання файлу); CancellationToken — можна перервати.
// CatalogService знає лише цей інтерфейс, тому в тестах замість файлу можна передати імпортер у пам'яті.
public interface ICatalogImporter
{
    Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default);
}

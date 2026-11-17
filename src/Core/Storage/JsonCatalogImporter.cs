using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstraction;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Лаб 8: асинхронний імпорт постачань із JSON-файлу.
// Справжній асинхронний ввід-вивід: FileStream(useAsync: true) + DeserializeAsync.
// Поки ОС читає диск, потік не блокується (на відміну від Task.Run(() => File.ReadAllText(...))).
//
// Три типи помилок:
//   OperationCanceledException — операцію скасовано (не помилка, проходить наскрізь без обгортання);
//   DomainException            — дані порушують правило складу (кількість 0, порожній SKU) + номер запису;
//   StorageException           — файл не є коректним JSON (технічна помилка, JsonException усередині).
public sealed class JsonCatalogImporter : ICatalogImporter
{
    // Web-налаштування: ключі "id", "sku" у файлі відповідають властивостям Id, Sku у ProductDto.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); // уже скасовано — файл навіть не відкриваємо

        await using var stream = OpenRead(path);

        List<ProductDto?>? dtos;
        try
        {
            // ct передається далі: серіалізатор перевіряє його між читаннями блоків файлу.
            dtos = await JsonSerializer.DeserializeAsync<List<ProductDto?>>(stream, Options, ct);
        }
        catch (JsonException ex)
        {
            throw new StorageException($"Файл {Path.GetFileName(path)} не є коректним JSON: {ex.Message}", ex);
        }

        if (dtos is null)
            throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        var result = new List<Product>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested(); // довгий цикл — перевіряємо токен на кожному записі
            result.Add(ToProduct(dtos[i], i + 1));
        }
        return result;
    }

    // Додаткове завдання 1: потоковий імпорт. Записи віддаються по одному, щойно прочитано їхні байти,
    // тому весь список не тримається в пам'яті. [EnumeratorCancellation] з'єднує цей параметр
    // з токеном, який передає викликач через items.WithCancellation(ct).
    public async IAsyncEnumerable<Product> StreamAsync(string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = OpenRead(path);
        await using var e = JsonSerializer.DeserializeAsyncEnumerable<ProductDto?>(stream, Options, ct)
                                          .GetAsyncEnumerator(ct);
        var number = 0;
        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = await e.MoveNextAsync();
            }
            catch (JsonException ex) // yield return не можна писати всередині try з catch, тому лише MoveNextAsync тут
            {
                throw new StorageException($"Файл {Path.GetFileName(path)} не є коректним JSON: {ex.Message}", ex);
            }

            if (!hasNext)
                yield break;

            ct.ThrowIfCancellationRequested();
            yield return ToProduct(e.Current, ++number);
        }
    }

    // bufferSize 4096, useAsync: true — асинхронний режим файлу (overlapped I/O).
    private static FileStream OpenRead(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

    // Перетворення одного запису. ArgumentException з Product.Create загортається в DomainException
    // з номером запису: для користувача це «погані дані у файлі», а не «програміст передав поганий аргумент».
    private static Product ToProduct(ProductDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожній запис (null).");

        // Варіант «Склад»: постачання з нульовою кількістю не має сенсу.
        if (dto.Quantity == 0)
            throw new DomainException($"Запис {number} ({dto.Sku}): кількість не може бути 0.");

        try
        {
            return Product.FromDto(dto);
        }
        catch (ArgumentException ex)
        {
            throw new DomainException($"Запис {number}: {FirstLine(ex.Message)}", ex);
        }
    }

    // Повідомлення ArgumentOutOfRangeException має другий рядок "Actual value was ...".
    private static string FirstLine(string message) => message.Split('\n')[0].Trim();
}

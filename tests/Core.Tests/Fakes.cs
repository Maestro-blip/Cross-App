using System.Runtime.CompilerServices;
using Core.Abstraction;
using Core.Domain;

namespace Core.Tests;

// Імпортер у пам'яті: CatalogService отримує ICatalogImporter, тож файл для тесту не потрібен.
internal sealed class FakeImporter(IReadOnlyList<Product> items) : ICatalogImporter
{
    public Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(items);
    }
}

// «Зависає», доки його не скасують: Task.Delay з нескінченним часом, але з токеном.
internal sealed class HangingImporter : ICatalogImporter
{
    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        await Task.Delay(Timeout.Infinite, ct);
        return [];
    }
}

// Синхронний IProgress: значення записуються одразу, без пулу потоків (на відміну від Progress<T>).
internal sealed class ListProgress : IProgress<int>
{
    public List<int> Values { get; } = [];
    public void Report(int value) => Values.Add(value);
}

internal static class AsyncStream
{
    public static async IAsyncEnumerable<Product> Of(IEnumerable<Product> items,
                                                     [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            await Task.Yield(); // справді асинхронна послідовність
            ct.ThrowIfCancellationRequested();
            yield return item;
        }
    }
}

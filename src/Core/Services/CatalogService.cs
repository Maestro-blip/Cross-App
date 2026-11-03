using Core.Abstraction;
using Core.Domain;

namespace Core.Services;

// Операції складу. Знає лише ICatalogStore і Product: про файли, DTO і Console — нічого.
// Правила товару (кількість > 0, не видати більше залишку) перевіряє Product.
public sealed class CatalogService(ICatalogStore store)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Product Add(string sku, string name, string unit, int quantity)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product); // інваріанти вже перевірив Product.Create
        return product;
    }

    public void Receive(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає товару з id={id}");
        product.RegisterArrival(quantity);
        _store.Update(product);
    }

    public void Issue(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає товару з id={id}");
        product.Issue(quantity);
        _store.Update(product);
    }

    public IReadOnlyList<Product> All() => _store.GetAll();

    public Product? Find(string id) => _store.GetById(id);
}

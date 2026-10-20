namespace Core.Services;

using Core.Domain;

// Додаткове завдання 2: інваріант, що охоплює ДВІ сутності.
// «Сумарний залишок товарів на складі не може перевищувати місткість складу».
// Product не знає про склад і про інші товари, Warehouse не знає своїх залишків,
// тому правило перевіряє сервіс, який бачить обидві сутності одночасно.
public sealed class StockService
{
    private readonly Dictionary<string, Warehouse> _warehouseBySku = new();
    private readonly List<Product> _products = [];

    public IReadOnlyList<Product> Products => _products.AsReadOnly();

    public IReadOnlyList<Product> ProductsIn(Warehouse warehouse) =>
        _products.Where(p => _warehouseBySku[p.Sku] == warehouse).ToList().AsReadOnly();

    public int Load(Warehouse warehouse) => ProductsIn(warehouse).Sum(p => p.Quantity);

    public int FreeSpace(Warehouse warehouse) => warehouse.Capacity - Load(warehouse);

    // Розмістити товар на складі разом з його поточним залишком.
    public void Place(Product product, Warehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(warehouse);

        if (_warehouseBySku.TryGetValue(product.Sku, out Warehouse? current))
            throw new InvalidOperationException($"Товар {product.Sku} вже розміщено на складі {current.Id}");

        EnsureFits(warehouse, product.Quantity);

        _warehouseBySku[product.Sku] = warehouse;
        _products.Add(product);
    }

    // Прихід товару через сервіс: спочатку правило складу, потім правило самого товару.
    public void RegisterArrival(Product product, int amount)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!_warehouseBySku.TryGetValue(product.Sku, out Warehouse? warehouse))
            throw new InvalidOperationException($"Товар {product.Sku} не розміщено на жодному складі");

        if (amount > 0)
            EnsureFits(warehouse, amount);

        product.RegisterArrival(amount);
    }

    private void EnsureFits(Warehouse warehouse, int amount)
    {
        int free = FreeSpace(warehouse);
        if (amount > free)
            throw new InvalidOperationException(
                $"Склад {warehouse.Id} переповнено: потрібно місця на {amount}, вільно {free} з {warehouse.Capacity}");
    }
}

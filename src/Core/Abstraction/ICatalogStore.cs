using Core.Domain;

namespace Core.Abstraction;

// Контракт сховища товарів: лише доменні операції, без деталей зберігання.
public interface ICatalogStore
{
    IReadOnlyList<Product> GetAll();   // IReadOnlyList — щоб ззовні не мутували сховище в обхід Add/Update/Delete
    Product? GetById(string id);       // Product? — запису може не бути
    void Add(Product item);
    void Update(Product item);
    bool Delete(string id);
}

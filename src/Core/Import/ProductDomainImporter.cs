namespace Core.Import;

using Core.Domain;
using Core.Dto;

// Додаткове завдання 1: ImportResult<ProductDto> -> ImportResult<Product>.
// Помилки парсингу переносяться як є, до них додаються записи, що не пройшли інваріанти.
public static class ProductDomainImporter
{
    public static ImportResult<Product> Convert(ImportResult<ProductDto> dto)
    {
        List<Product> products = [];
        List<string> errors = [.. dto.Errors];

        foreach (ProductDto item in dto.Items)
        {
            try
            {
                products.Add(Product.FromDto(item));
            }
            catch (ArgumentException err)
            {
                errors.Add($"запис '{item.Id}': {err.Message}");
            }
        }
        return new ImportResult<Product>(products, errors);
    }
}

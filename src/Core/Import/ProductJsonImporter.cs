using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<ProductDto> Load(string path, Action<string>? onSkipped = null)
    {
        string json = File.ReadAllText(path);

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<ProductDto>>(json, options) ?? [];
            return new ImportResult<ProductDto>(items, []);
        }
        catch (JsonException ex)
        {
            string message = $"некоректний JSON: {ex.Message}";
            onSkipped?.Invoke(message);
            return new ImportResult<ProductDto>([], [message]);
        }
    }
}

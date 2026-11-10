namespace Core.Reports;

// Рядок звіту. record — незмінний і порівнюється за значенням (зручно для тестів тижня 8).
// Звіт повертає DTO, а не Product: контракт звіту не залежить від внутрішньої будови сутності.
public sealed record StockLine(string Sku, string Name, int Quantity, string Unit);

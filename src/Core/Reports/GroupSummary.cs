namespace Core.Reports;

// Підсумок однієї групи: ключ (одиниця виміру), кількість позицій і сумарний залишок.
public sealed record GroupSummary(string Key, int Count, int TotalQuantity);

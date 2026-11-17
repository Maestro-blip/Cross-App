using Core.Domain;

namespace Core.Tests;

// Маленький фіксований набір саме для тестів (не SampleData): тест не зламається, якщо хтось змінить демо-дані.
// Кожен виклик створює НОВІ об'єкти — тести не ділять спільний стан.
//   SKU-MID і SKU-MIX мають однаковий залишок 50 — перевірка другого ключа сортування;
//   SKU-EDGE = 10 — межа порогу (10 < 10 хибне, у дефіцит не потрапляє).
internal static class TestData
{
    public static IReadOnlyList<Product> Items() =>
    [
        Product.Create("T-1", "SKU-MIX", "Дюбель", "шт", 50),
        Product.Create("T-2", "SKU-BIG", "Кабель", "м", 100),
        Product.Create("T-3", "SKU-LOW", "Розетка", "шт", 3),
        Product.Create("T-4", "SKU-MID", "Гвинт", "шт", 50),
        Product.Create("T-5", "SKU-EDGE", "Скотч", "шт", 10),
        Product.Create("T-6", "SKU-FEW", "Лампа", "шт", 7),
    ];
}

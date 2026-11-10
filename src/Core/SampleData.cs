using Core.Domain;

namespace Core;

// Початкові товари (seed) для режиму в пам'яті.
// Лаб 7: 20 записів, 6 одиниць виміру (шт, м, пачка, кг, л, упаковка) і 7 позицій із залишком < 10,
// щоб GroupBy показував кілька груп, а report-low не був порожнім.
public static class SampleData
{
    public static IReadOnlyList<Product> Products() =>
    [
        Product.Create("P-001", "TECH-LAP-01", "Ноутбук ASUS ZenBook 14", "шт", 3),
        Product.Create("P-002", "TECH-MBL-02", "Смартфон Apple iPhone 15", "шт", 4),
        Product.Create("P-003", "TECH-ACC-03", "Бездротові навушники Sony WH-1000XM5", "шт", 14),
        Product.Create("P-004", "HOME-COF-04", "Кавомашина DeLonghi Magnifica", "шт", 18),
        Product.Create("P-005", "HOME-MIX-05", "Кухонний комбайн Bosch", "шт", 5),
        Product.Create("P-006", "DRY-COF-06", "Кава зернова Arabica 1кг", "пачка", 65),
        Product.Create("P-007", "DRY-TEA-07", "Чай чорний байховий 200г", "упаковка", 20),
        Product.Create("P-008", "FOOD-CHO-08", "Шоколад чорний 85%", "шт", 45),
        Product.Create("P-009", "OFF-DESK-09", "Стіл письмовий регульований", "шт", 8),
        Product.Create("P-010", "OFF-CHAIR-10", "Крісло офісне ергономічне", "шт", 4),
        Product.Create("P-011", "TECH-MON-11", "Монітор 27\" Dell IPS", "шт", 11),
        Product.Create("P-012", "TECH-KEY-12", "Клавіатура механічна Logitech", "шт", 34),
        Product.Create("P-013", "TECH-MOU-13", "Миша бездротова Razer", "шт", 21),
        Product.Create("P-014", "HOME-LAM-14", "Настільна LED лампа", "шт", 95),
        Product.Create("P-015", "HOME-BOT-15", "Термочашка Stanley 0.5л", "шт", 145),
        Product.Create("P-016", "TECH-CAB-16", "Кабель UTP cat6 (бухта)", "м", 305),
        Product.Create("P-017", "DRY-SUG-17", "Цукор білий", "кг", 40),
        Product.Create("P-018", "HOME-CLN-18", "Засіб для миття посуду", "л", 7),
        Product.Create("P-019", "DRY-COF-19", "Кава мелена 250г", "пачка", 9),
        Product.Create("P-020", "TECH-CAB-20", "Кабель живлення ПВС 3x1.5", "м", 120),
    ];

    private static readonly string[] Units = ["шт", "м", "пачка", "кг", "л", "упаковка"];

    // Лаб 7, додаткове завдання 4: великий набір для вимірювання часу звітів.
    // Фіксований seed — однакові дані при кожному запуску, тож заміри можна порівнювати.
    public static IReadOnlyList<Product> Generate(int count, int seed = 42)
    {
        var random = new Random(seed);
        var items = new List<Product>(count);
        for (int i = 1; i <= count; i++)
            items.Add(Product.Create($"G-{i:D6}", $"GEN-{i:D6}", $"Товар {i}", Units[random.Next(Units.Length)], random.Next(0, 500)));
        return items;
    }
}

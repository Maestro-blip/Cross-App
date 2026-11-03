using Core.Domain;

namespace Core;

// Початкові товари (seed) для режиму в пам'яті; знадобляться також у лабі 7.
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
    ];
}

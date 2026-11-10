using Core.Domain;
using Core.Events;

namespace Core.Reports;

// Лаб 7: усі звіти — чисті функції над IEnumerable<Product> (LINQ to Objects).
// Приймають послідовність, а не сховище: їх можна викликати з будь-якого джерела і легко перевірити тестом.
// Повертають IReadOnlyList<DTO>: результат уже матеріалізовано (ToList), тож повторне використання
// у Cli не перезапускає запит. Про спосіб виводу звіти нічого не знають.
public static class ReportService
{
    // 1) Дефіцит: фільтр + сортування + проєкція в DTO.
    // Where стоїть першим — сортуються лише відібрані елементи, а не весь склад.
    public static IReadOnlyList<StockLine> LowStock(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
              .OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Select(ToLine)
              .ToList();

    // 2) Топ-N за залишком. Другий ключ (Sku) робить порядок стабільним між запусками.
    // Сортування винесено в метод-розширення TopBy (додаткове завдання 2).
    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<Product> source, int n) =>
        source.TopBy(p => p.Quantity, n, p => p.Sku)
              .Select(ToLine)
              .ToList();

    // 3) Групування за одиницею виміру з підрахунком і сумою (синтаксис методів).
    // OrdinalIgnoreCase зливає "шт" і "ШТ" в одну групу; ThenBy за ключем — стабільний порядок груп з однаковою сумою.
    public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source) =>
        source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
              .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
              .ToList();

    // 3a) Той самий звіт у синтаксисі запитів (додаткове завдання 3). Компілятор перекладає
    // from/group/into/orderby/select у ті самі виклики GroupBy/OrderByDescending/Select.
    public static IReadOnlyList<GroupSummary> ByUnitQuery(IEnumerable<Product> source) =>
        (from product in source
         group product by product.Unit into g
         let total = g.Sum(p => p.Quantity)
         orderby total descending, g.Key
         select new GroupSummary(g.Key, g.Count(), total))
        .ToList();

    // 4) Агрегати. Average і Max на порожній послідовності кидають InvalidOperationException,
    // тому порожнечу перевіряємо до них. ToList — щоб джерело перебрати один раз, а не чотири.
    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Product> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count, list.Sum(p => p.Quantity),
               list.Average(p => p.Quantity), list.Max(p => p.Quantity));
    }

    // 5) Демо відкладеного виконання. Навмисно БЕЗ ToList: метод лише будує запит (Where + Select)
    // і повертає його. Джерело переглядається щоразу, коли викликач перебирає результат
    // (foreach, Count(), ToList()), тому зміни джерела між двома перебираннями потрапляють у результат.
    public static IEnumerable<StockLine> LowStockDeferred(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold).Select(ToLine);

    // 6) Додаткове завдання 1: GroupJoin товарів із журналом змін (події Changed з лаб 6).
    // GroupJoin, на відміну від Join, лишає товар і тоді, коли змін по ньому не було (Operations = 0).
    public static IReadOnlyList<ProductActivity> Activity(IEnumerable<Product> products, IEnumerable<CatalogChangedEventArgs> changes) =>
        products.GroupJoin(changes,
                           product => product.Id,
                           change => change.Id,
                           (product, its) => new ProductActivity(
                               product.Sku, product.Name, product.Quantity, its.Count(), its.Sum(c => c.Delta)))
                .OrderByDescending(a => a.Operations).ThenBy(a => a.Sku, StringComparer.Ordinal)
                .ToList();

    // 7) Найактивніші товари: друге використання TopBy (додаткове завдання 2).
    public static IReadOnlyList<ProductActivity> MostActive(IEnumerable<Product> products, IEnumerable<CatalogChangedEventArgs> changes, int n) =>
        Activity(products, changes)
            .Where(a => a.Operations > 0)
            .TopBy(a => a.Operations, n, a => a.Sku)
            .ToList();

    // 8) Додаткове завдання 4: два варіанти одного звіту для заміру часу.
    // Без проміжного ToList: Where передає елементи далі по одному, у пам'ять збирається лише відібране.
    public static IReadOnlyList<StockLine> LowStockStreaming(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
              .Select(ToLine)
              .OrderBy(l => l.Quantity).ThenBy(l => l.Sku, StringComparer.Ordinal)
              .ToList();

    // З ToList у середині ланцюжка: кожен етап створює повний проміжний список.
    public static IReadOnlyList<StockLine> LowStockMaterializedMidway(IEnumerable<Product> source, int threshold) =>
        source.ToList()
              .Where(p => p.Quantity < threshold).ToList()
              .Select(ToLine).ToList()
              .OrderBy(l => l.Quantity).ThenBy(l => l.Sku, StringComparer.Ordinal)
              .ToList();

    private static StockLine ToLine(Product p) => new(p.Sku, p.Name, p.Quantity, p.Unit);
}

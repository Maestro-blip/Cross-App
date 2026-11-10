namespace Core.Reports;

// Додаткове завдання 2: власний метод-розширення для топ-N.
// this перед першим параметром дозволяє викликати його як source.TopBy(...), поруч зі стандартними LINQ-операторами.
public static class EnumerableExtensions
{
    // Перші n елементів за спаданням ключа; при рівних ключах — за зростанням tieBreaker (ordinal),
    // тому порядок однаковий за кожного запуску незалежно від порядку джерела.
    // Як і OrderBy/Take, виконується відкладено: результат обчислюється під час перебирання.
    public static IEnumerable<T> TopBy<T>(this IEnumerable<T> source, Func<T, int> key, int n, Func<T, string> tieBreaker)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(tieBreaker);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return source.OrderByDescending(key).ThenBy(tieBreaker, StringComparer.Ordinal).Take(n);
    }
}

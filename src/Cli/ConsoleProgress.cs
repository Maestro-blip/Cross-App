// Лаб 8, додаткове завдання 3: підписник IProgress<int>, що друкує хід імпорту кроками по step відсотків.
// Свій клас, а не стандартний Progress<T>: Progress<T> викликає обробник через пул потоків,
// тож рядок «100%» міг би з'явитися вже ПІСЛЯ «Імпортовано записів: ...». Тут Report друкує одразу.
public sealed class ConsoleProgress(int step) : IProgress<int>
{
    private readonly int _step = step;
    private int _next = step;

    public void Report(int percent)
    {
        if (percent < _next)
            return;

        int filled = percent / 10;
        Console.WriteLine($"  [{new string('#', filled)}{new string('.', 10 - filled)}] {percent,3}%");
        _next = (percent / _step + 1) * _step;
    }
}

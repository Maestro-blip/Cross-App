namespace Core.Domain;

public enum MovementType
{
    Arrival,
    Issue
}

// Додаткове завдання 3: явний стан накладної.
public enum MovementStatus
{
    Draft,
    Posted,
    Cancelled
}

public sealed record MovementLine(Product Product, int Amount);

// Накладна руху товару (прихід або видача). Змінює залишки лише під час проведення.
public sealed class Movement
{
    private readonly List<MovementLine> _lines = [];

    public string Number { get; }
    public MovementType Type { get; }
    public MovementStatus Status { get; private set; }
    public IReadOnlyList<MovementLine> Lines => _lines.AsReadOnly();
    public int TotalAmount => _lines.Sum(l => l.Amount);

    private Movement(string number, MovementType type)
    {
        Number = number;
        Type = type;
        Status = MovementStatus.Draft;
    }

    public static Movement Create(string number, MovementType type)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Номер накладної обов'язковий", nameof(number));

        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Невідомий тип накладної");

        return new Movement(number.Trim().ToUpperInvariant(), type);
    }

    // Таблиця допустимих переходів: усе, що не перелічено, заборонено.
    public static bool CanTransition(MovementStatus from, MovementStatus to) => (from, to) switch
    {
        (MovementStatus.Draft, MovementStatus.Posted) => true,
        (MovementStatus.Draft, MovementStatus.Cancelled) => true,
        (MovementStatus.Posted, MovementStatus.Cancelled) => true, // сторнування проведеної
        _ => false
    };

    public void AddLine(Product product, int amount)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (Status != MovementStatus.Draft)
            throw new InvalidOperationException($"Накладна {Number} у стані {Status}: рядки можна додавати лише в Draft");

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Кількість у рядку має бути більшою за нуль");

        if (_lines.Any(l => l.Product.Sku == product.Sku))
            throw new InvalidOperationException($"Товар {product.Sku} вже є в накладній {Number}");

        _lines.Add(new MovementLine(product, amount));
    }

    // Draft -> Posted: застосувати рух до залишків.
    public void Post()
    {
        EnsureTransition(MovementStatus.Posted);

        if (_lines.Count == 0)
            throw new InvalidOperationException($"Накладна {Number} порожня, проводити нічого");

        Apply(Type);
        Status = MovementStatus.Posted;
    }

    // Draft -> Cancelled: просто скасувати; Posted -> Cancelled: повернути залишки назад.
    public void Cancel()
    {
        EnsureTransition(MovementStatus.Cancelled);

        if (Status == MovementStatus.Posted)
            Apply(Type == MovementType.Arrival ? MovementType.Issue : MovementType.Arrival);

        Status = MovementStatus.Cancelled;
    }

    private void EnsureTransition(MovementStatus target)
    {
        if (!CanTransition(Status, target))
            throw new InvalidOperationException($"Накладна {Number}: перехід {Status} -> {target} заборонено");
    }

    // Усі рядки перевіряються ДО зміни першого товару, щоб проведення було «все або нічого».
    private void Apply(MovementType direction)
    {
        if (direction == MovementType.Issue)
        {
            MovementLine? lacking = _lines.FirstOrDefault(l => l.Amount > l.Product.Quantity);
            if (lacking is not null)
                throw new InvalidOperationException(
                    $"Накладна {Number}: не можна видати {lacking.Amount}, залишок {lacking.Product.Sku} = {lacking.Product.Quantity}");
        }

        foreach (MovementLine line in _lines)
        {
            if (direction == MovementType.Arrival)
                line.Product.RegisterArrival(line.Amount);
            else
                line.Product.Issue(line.Amount);
        }
    }

    public override string ToString() => $"Накладна {Number} ({Type}) [{Status}], рядків {_lines.Count}, одиниць {TotalAmount}";
}

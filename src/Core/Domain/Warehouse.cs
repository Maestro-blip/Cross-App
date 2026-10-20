namespace Core.Domain;

using Core.Dto;

public sealed class Warehouse
{
    public string Id { get; }
    public string Name { get; }
    public string City { get; }

    // Максимальна сумарна кількість одиниць товару, яку вміщує склад.
    public int Capacity { get; }

    private Warehouse(string id, string name, string city, int capacity)
    {
        Id = id;
        Name = name;
        City = city;
        Capacity = capacity;
    }

    public static Warehouse Create(string id, string name, string city, int capacity)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва складу не може бути порожньою", nameof(name));

        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Місткість складу має бути більшою за нуль");

        return new Warehouse(id.Trim().ToUpperInvariant(), name.Trim(), city?.Trim() ?? "", capacity);
    }

    public WarehouseDto ToDto() => new(Id, Name, City);

    // Місткості у форматі тижня 3 немає, тому її передають окремо.
    public static Warehouse FromDto(WarehouseDto dto, int capacity) => Create(dto.Id, dto.Name, dto.City, capacity);

    public override string ToString() => $"{Id} {Name} ({City}), місткість {Capacity}";
}

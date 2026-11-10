namespace Core.Reports;

// Додаткове завдання 1: зведений рядок «товар × журнал змін» (GroupJoin).
// Operations — скільки подій Changed було по товару, NetDelta — сумарна зміна залишку.
public sealed record ProductActivity(string Sku, string Name, int Quantity, int Operations, int NetDelta);

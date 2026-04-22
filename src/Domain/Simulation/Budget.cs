namespace TowerFluffy.Domain.Simulation;

public readonly record struct Budget(int Value)
{
    public static Budget Zero => new(0);

    public Budget Add(Budget other) => new(Value + other.Value);

    public Budget Subtract(Budget other) => new(Math.Max(0, Value - other.Value));

    public bool CanAfford(Budget cost) => Value >= cost.Value;
}

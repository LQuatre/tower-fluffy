namespace TowerFluffy.Domain.Simulation;

public readonly record struct Gold(int Value)
{
    public static Gold Zero => new(0);

    public Gold Add(Gold other) => new(Value + other.Value);

    public Gold Subtract(Gold other) => new(Math.Max(0, Value - other.Value));

    public bool CanAfford(Gold cost) => Value >= cost.Value;
}

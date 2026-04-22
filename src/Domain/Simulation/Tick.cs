namespace TowerFluffy.Domain.Simulation;

public readonly record struct Tick(int Value)
{
    public Tick Next() => new(Value + 1);
}

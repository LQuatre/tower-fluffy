namespace TowerFluffy.Domain.Shared;

public readonly record struct Tick(int Value)
{
    public Tick Next() => new(Value + 1);
}

using TowerFluffy.Domain.Combat;

namespace TowerFluffy.Domain.Shared;

public readonly record struct Health(int Value)
{
    public Health Subtract(Damage damage) => new(Math.Max(0, Value - damage.Value));
}

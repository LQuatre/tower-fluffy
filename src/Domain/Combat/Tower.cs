using TowerFluffy.Domain.Match;

using TowerFluffy.Domain.Shared;

namespace TowerFluffy.Domain.Combat;

public sealed record Tower(int Id, TowerType Type, GridPosition Position, TowerStats Stats, Health Health, int CooldownTicksRemaining)
{
    public Tower DecrementCooldown() => this with { CooldownTicksRemaining = Math.Max(0, CooldownTicksRemaining - 1) };

    public Tower ResetCooldownAfterShot() => this with { CooldownTicksRemaining = Stats.CooldownTicksBetweenShots };

    public Tower ApplyDamage(Damage damage) => this with { Health = Health.Subtract(damage) };
}

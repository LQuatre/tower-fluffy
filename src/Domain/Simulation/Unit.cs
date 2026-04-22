namespace TowerFluffy.Domain.Simulation;

public sealed record Unit(
    int Id,
    int DistanceAlongPath,
    int SpeedPerTick,
    Damage DamageToBase,
    Damage DamageToTower,
    int AttackRange,
    int AttackCooldownTicksBetweenAttacks,
    int AttackCooldownTicksRemaining,
    int? TargetTowerId,
    Health Health,
    Gold LootGold)
{
    public Unit AdvanceOneTick() => this with { DistanceAlongPath = DistanceAlongPath + SpeedPerTick };

    public Unit ApplyDamage(Damage damage) => this with { Health = Health.Subtract(damage) };

    public bool CanAttackTowers => DamageToTower.Value > 0 && AttackRange > 0;

    public Unit DecrementAttackCooldown() =>
        this with { AttackCooldownTicksRemaining = Math.Max(0, AttackCooldownTicksRemaining - 1) };

    public Unit ResetAttackCooldownAfterAttack() => this with { AttackCooldownTicksRemaining = AttackCooldownTicksBetweenAttacks };

    public Unit WithTargetTower(int towerId) => this with { TargetTowerId = towerId };

    public Unit ClearTargetTower() => this with { TargetTowerId = null };
}

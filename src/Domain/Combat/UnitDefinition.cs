using TowerFluffy.Domain.Shared;

namespace TowerFluffy.Domain.Combat;

public readonly record struct UnitDefinition(
    UnitType Type,
    Budget Cost,
    Health Health,
    int SpeedPerTick,
    Damage DamageToBase,
    Damage DamageToTower,
    int AttackRange,
    int AttackCooldownTicksBetweenAttacks,
    Gold LootGold);

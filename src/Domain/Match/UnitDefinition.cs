using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Domain.Match;

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

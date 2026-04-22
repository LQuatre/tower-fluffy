namespace TowerFluffy.Domain.Simulation;

public enum CombatEventKind
{
    TowerShot = 0,
    UnitAttackTower = 1,
    UnitHitBase = 2,
}

public readonly record struct CombatEvent(
    Tick Tick,
    CombatEventKind Kind,
    int SourceId,
    int? TargetId,
    WorldPosition From,
    WorldPosition To,
    Damage Damage,
    bool TargetDestroyed);

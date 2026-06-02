using TowerFluffy.Application.Game.Dtos.Environment;

namespace TowerFluffy.Application.Game.Dtos.Combat;

public sealed record CombatEventDto(
    int Tick,
    CombatEventKindDto Kind,
    int SourceId,
    TowerTypeDto? SourceTowerType,
    int? TargetId,
    WorldPositionDto From,
    WorldPositionDto To,
    int Damage,
    bool TargetDestroyed);

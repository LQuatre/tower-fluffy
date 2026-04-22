using System.Collections.Generic;

namespace TowerFluffy.Application.Game.Dtos;

public sealed record GameSnapshotDto(
    MapDto Map,
    HudDto Hud,
    IReadOnlyList<UnitDto> Units,
    IReadOnlyList<TowerDto> Towers,
    IReadOnlyList<CombatEventDto> CombatEvents);

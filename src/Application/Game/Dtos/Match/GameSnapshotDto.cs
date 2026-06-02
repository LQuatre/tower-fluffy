using System.Collections.Generic;

using TowerFluffy.Application.Game.Dtos.Combat;
using TowerFluffy.Application.Game.Dtos.Environment;

namespace TowerFluffy.Application.Game.Dtos.Match;

public sealed record GameSnapshotDto(
    MapDto Map,
    HudDto Hud,
    IReadOnlyList<UnitDto> Units,
    IReadOnlyList<TowerDto> Towers,
    IReadOnlyList<CombatEventDto> CombatEvents);

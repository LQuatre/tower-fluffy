using System.Collections.Generic;

namespace TowerFluffy.Application.Game.Dtos;

public sealed record MapDto(
    int Width,
    int Height,
    int CellSize,
    IReadOnlyList<GridPositionDto> BlockedCells,
    WorldPositionDto PathStart,
    WorldPositionDto PathEnd);

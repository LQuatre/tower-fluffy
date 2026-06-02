using TowerFluffy.Application.Game.Dtos.Environment;

namespace TowerFluffy.Application.Game.Dtos.Combat;

public sealed record TowerDto(int Id, TowerTypeDto Type, GridPositionDto Cell, int Health, int CooldownTicksRemaining);

using TowerFluffy.Application.Game.Dtos.Environment;

namespace TowerFluffy.Application.Game.Dtos.Combat;

public sealed record UnitDto(int Id, WorldPositionDto Position, WorldPositionDto Direction, int Health, int DistanceAlongPath);

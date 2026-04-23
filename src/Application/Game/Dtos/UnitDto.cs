namespace TowerFluffy.Application.Game.Dtos;

public sealed record UnitDto(int Id, WorldPositionDto Position, WorldPositionDto Direction, int Health, int DistanceAlongPath);

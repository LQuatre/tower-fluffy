namespace TowerFluffy.Application.Game.Dtos;

public sealed record UnitDto(int Id, WorldPositionDto Position, int Health, int DistanceAlongPath);

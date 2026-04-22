namespace TowerFluffy.Application.Game.Dtos;

public sealed record TowerDto(int Id, GridPositionDto Cell, int Health, int CooldownTicksRemaining);

namespace TowerFluffy.Application.Game.Dtos;

public sealed record TowerDto(int Id, TowerTypeDto Type, GridPositionDto Cell, int Health, int CooldownTicksRemaining);

namespace TowerFluffy.Domain.Simulation;

public readonly record struct TowerStats(Gold Cost, Damage DamagePerShot, int Range, int CooldownTicksBetweenShots);

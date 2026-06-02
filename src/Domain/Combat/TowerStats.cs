using TowerFluffy.Domain.Shared;

namespace TowerFluffy.Domain.Combat;

public readonly record struct TowerStats(Gold Cost, Damage DamagePerShot, int Range, int CooldownTicksBetweenShots);

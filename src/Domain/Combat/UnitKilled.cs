using TowerFluffy.Domain.Shared;

namespace TowerFluffy.Domain.Combat;

public readonly record struct UnitKilled(int UnitId, Gold LootGold);

using TowerFluffy.Domain.Shared;

namespace TowerFluffy.Domain.Combat;

public readonly record struct TowerDefinition(TowerType Type, TowerStats Stats, Health Health);

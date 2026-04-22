using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Domain.Match;

public readonly record struct TowerDefinition(TowerType Type, TowerStats Stats, Health Health);

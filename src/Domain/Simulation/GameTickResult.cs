using System.Collections.Generic;

namespace TowerFluffy.Domain.Simulation;

public sealed record GameTickResult(
	GameState State,
	Damage BaseDamageDealt,
	IReadOnlyList<UnitKilled> UnitsKilled,
	IReadOnlyList<TowerDestroyed> TowersDestroyed,
	IReadOnlyList<CombatEvent> CombatEvents);

using System.Collections.Generic;

using TowerFluffy.Domain.Combat;

namespace TowerFluffy.Domain.Engine;

public sealed record GameTickResult(
	GameState State,
	Damage BaseDamageDealt,
	IReadOnlyList<UnitKilled> UnitsKilled,
	IReadOnlyList<TowerDestroyed> TowersDestroyed,
	IReadOnlyList<CombatEvent> CombatEvents);

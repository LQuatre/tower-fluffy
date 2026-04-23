using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerFluffy.Domain.Simulation;

public sealed class GameState
{
    private readonly Unit[] _units;
    private readonly Tower[] _towers;

    public GameState(Tick tick, Health baseHealth, IEnumerable<Unit> units, IEnumerable<Tower> towers)
    {
        Tick = tick;
        BaseHealth = baseHealth;
        _units = units?.ToArray() ?? throw new ArgumentNullException(nameof(units));
        _towers = towers?.ToArray() ?? throw new ArgumentNullException(nameof(towers));
    }

    public Tick Tick { get; }
    public Health BaseHealth { get; }
    public IReadOnlyList<Unit> Units => _units;
    public IReadOnlyList<Tower> Towers => _towers;

    public GameTickResult AdvanceOneTick(Map map)
    {
        if (map is null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        var nextTick = Tick.Next();

        var unitPhase = ApplyUnitPhase(nextTick, map, _units, _towers);
        var towerPhase = ApplyTowerAttacks(nextTick, map, unitPhase.UnitsAfterActions, unitPhase.TowersAfterActions);
        var nextHealth = BaseHealth.Subtract(unitPhase.BaseDamage);

        var combatEvents = CombineCombatEvents(unitPhase.CombatEvents, towerPhase.CombatEvents);

        var nextState = new GameState(nextTick, nextHealth, towerPhase.UnitsAfterAttacks, towerPhase.TowersAfterAttacks);
        return new GameTickResult(
            nextState,
            unitPhase.BaseDamage,
            towerPhase.KilledUnits,
            unitPhase.DestroyedTowers,
            combatEvents);
    }

    public GameState WithAddedUnit(Unit unit)
    {
        if (unit is null)
        {
            throw new ArgumentNullException(nameof(unit));
        }

        return new GameState(Tick, BaseHealth, _units.Concat(new[] { unit }), _towers);
    }

    public GameState WithAddedTower(Tower tower)
    {
        if (tower is null)
        {
            throw new ArgumentNullException(nameof(tower));
        }

        return new GameState(Tick, BaseHealth, _units, _towers.Concat(new[] { tower }));
    }

    public GameState WithMovedTower(GridPosition oldPosition, GridPosition newPosition)
    {
        var towerIndex = Array.FindIndex(_towers, t => t.Position == oldPosition);
        if (towerIndex < 0) return this;

        var updatedTowers = _towers.ToArray();
        updatedTowers[towerIndex] = updatedTowers[towerIndex] with { Position = newPosition };
        
        return new GameState(Tick, BaseHealth, _units, updatedTowers);
    }

    private static UnitPhaseResult ApplyUnitPhase(Tick tick, Map map, Unit[] units, Tower[] towers)
    {
        if (units.Length == 0)
        {
            return new UnitPhaseResult(
                UnitsAfterActions: Array.Empty<Unit>(),
                TowersAfterActions: towers,
                BaseDamage: new Damage(0),
                DestroyedTowers: Array.Empty<TowerDestroyed>(),
                CombatEvents: Array.Empty<CombatEvent>());
        }

        var unitsAfter = new List<Unit>(units.Length);
        var towersAfter = towers.ToList();
        var baseDamage = 0;
        var destroyedTowers = new List<TowerDestroyed>();
        var combatEvents = new List<CombatEvent>();

        foreach (var unit in units.OrderByDescending(static u => u.DistanceAlongPath).ThenBy(static u => u.Id))
        {
            var unitAfterCooldown = unit.DecrementAttackCooldown();
            var unitPosition = map.Path.GetPositionAtDistance(unitAfterCooldown.DistanceAlongPath);

            var targetIndex = FindTowerIndexToAttack(map, unitPosition, unitAfterCooldown, towersAfter);
            if (targetIndex >= 0)
            {
                var towerPosition = map.GetCellCenter(towersAfter[targetIndex].Position);
                var targeted = unitAfterCooldown.WithTargetTower(towersAfter[targetIndex].Id);
                var afterAttack = TryAttackTower(
                    tick,
                    targeted,
                    unitPosition,
                    towerPosition,
                    towersAfter,
                    targetIndex,
                    destroyedTowers,
                    combatEvents);
                unitsAfter.Add(afterAttack);
                continue;
            }

            var advanced = unitAfterCooldown.ClearTargetTower().AdvanceOneTick();
            if (advanced.DistanceAlongPath >= map.Path.TotalLength)
            {
                baseDamage += advanced.DamageToBase.Value;
                combatEvents.Add(new CombatEvent(
                    Tick: tick,
                    Kind: CombatEventKind.UnitHitBase,
                    SourceId: advanced.Id,
                    TargetId: null,
                    From: unitPosition,
                    To: map.Path.End,
                    Damage: advanced.DamageToBase,
                    TargetDestroyed: false));
                continue;
            }

            unitsAfter.Add(advanced);
        }

        return new UnitPhaseResult(
            UnitsAfterActions: unitsAfter.OrderBy(static u => u.Id).ToArray(),
            TowersAfterActions: towersAfter.OrderBy(static t => t.Id).ToArray(),
            BaseDamage: new Damage(baseDamage),
            DestroyedTowers: destroyedTowers.ToArray(),
            CombatEvents: combatEvents.ToArray());
    }

    private static int FindTowerIndexToAttack(Map map, WorldPosition unitPosition, Unit unit, IReadOnlyList<Tower> towers)
    {
        if (!unit.CanAttackTowers || towers.Count == 0)
        {
            return -1;
        }

        if (unit.TargetTowerId is int targetTowerId)
        {
            for (var index = 0; index < towers.Count; index++)
            {
                var tower = towers[index];
                if (tower.Id != targetTowerId)
                {
                    continue;
                }

                if (IsInRange(map, unitPosition, unit, tower))
                {
                    return index;
                }
            }
        }

        var bestIndex = -1;
        var bestDistance = int.MaxValue;
        var bestTowerId = int.MaxValue;

        for (var index = 0; index < towers.Count; index++)
        {
            var tower = towers[index];
            var distance = map.GetCellCenter(tower.Position).ManhattanDistanceTo(unitPosition);
            if (distance > unit.AttackRange)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestIndex = index;
                bestDistance = distance;
                bestTowerId = tower.Id;
                continue;
            }

            if (distance == bestDistance && tower.Id < bestTowerId)
            {
                bestIndex = index;
                bestTowerId = tower.Id;
            }
        }

        return bestIndex;
    }

    private static bool IsInRange(Map map, WorldPosition unitPosition, Unit unit, Tower tower)
    {
        var towerPosition = map.GetCellCenter(tower.Position);
        return towerPosition.ManhattanDistanceTo(unitPosition) <= unit.AttackRange;
    }

    private static Unit TryAttackTower(
        Tick tick,
        Unit unit,
        WorldPosition unitPosition,
        WorldPosition towerPosition,
        List<Tower> towers,
        int targetIndex,
        ICollection<TowerDestroyed> destroyedTowers,
        ICollection<CombatEvent> combatEvents)
    {
        if (unit.AttackCooldownTicksRemaining > 0)
        {
            return unit;
        }

        var tower = towers[targetIndex];
        var damagedTower = tower.ApplyDamage(unit.DamageToTower);
        var towerDestroyed = damagedTower.Health.Value == 0;

        combatEvents.Add(new CombatEvent(
            Tick: tick,
            Kind: CombatEventKind.UnitAttackTower,
            SourceId: unit.Id,
            TargetId: tower.Id,
            From: unitPosition,
            To: towerPosition,
            Damage: unit.DamageToTower,
            TargetDestroyed: towerDestroyed));

        if (towerDestroyed)
        {
            towers.RemoveAt(targetIndex);
            destroyedTowers.Add(new TowerDestroyed(tower.Id));
            return unit.ClearTargetTower().ResetAttackCooldownAfterAttack();
        }

        towers[targetIndex] = damagedTower;
        return unit.ResetAttackCooldownAfterAttack();
    }

    private static TowerAttackResult ApplyTowerAttacks(Tick tick, Map map, Unit[] units, Tower[] towers)
    {
        if (towers.Length == 0 || units.Length == 0)
        {
            return new TowerAttackResult(units, towers, Array.Empty<UnitKilled>(), Array.Empty<CombatEvent>());
        }

        var livingUnits = units.ToList();
        var towersAfter = new Tower[towers.Length];
        var killed = new List<UnitKilled>();
        var combatEvents = new List<CombatEvent>();

        for (var towerIndex = 0; towerIndex < towers.Length; towerIndex++)
        {
            var tower = towers[towerIndex].DecrementCooldown();
            if (tower.CooldownTicksRemaining > 0)
            {
                towersAfter[towerIndex] = tower;
                continue;
            }

            var targetIndex = FindTargetIndex(map, tower, livingUnits);
            if (targetIndex < 0)
            {
                towersAfter[towerIndex] = tower;
                continue;
            }

            var target = livingUnits[targetIndex];
            var towerPosition = map.GetCellCenter(tower.Position);
            var targetPosition = map.Path.GetPositionAtDistance(target.DistanceAlongPath);
            var damaged = target.ApplyDamage(tower.Stats.DamagePerShot);
            var unitKilled = damaged.Health.Value == 0;

            combatEvents.Add(new CombatEvent(
                Tick: tick,
                Kind: CombatEventKind.TowerShot,
                SourceId: tower.Id,
                TargetId: target.Id,
                From: towerPosition,
                To: targetPosition,
                Damage: tower.Stats.DamagePerShot,
                TargetDestroyed: unitKilled));

            if (unitKilled)
            {
                livingUnits.RemoveAt(targetIndex);
                killed.Add(new UnitKilled(target.Id, target.LootGold));
            }
            else
            {
                livingUnits[targetIndex] = damaged;
            }

            towersAfter[towerIndex] = tower.ResetCooldownAfterShot();
        }

        return new TowerAttackResult(livingUnits.ToArray(), towersAfter, killed.ToArray(), combatEvents.ToArray());
    }

    private static int FindTargetIndex(Map map, Tower tower, IReadOnlyList<Unit> units)
    {
        var towerPosition = map.GetCellCenter(tower.Position);

        var bestIndex = -1;
        var bestDistanceAlongPath = -1;
        var bestUnitId = int.MaxValue;

        for (var index = 0; index < units.Count; index++)
        {
            var unit = units[index];
            var unitPosition = map.Path.GetPositionAtDistance(unit.DistanceAlongPath);
            if (towerPosition.ManhattanDistanceTo(unitPosition) > tower.Stats.Range)
            {
                continue;
            }

            if (unit.DistanceAlongPath > bestDistanceAlongPath)
            {
                bestIndex = index;
                bestDistanceAlongPath = unit.DistanceAlongPath;
                bestUnitId = unit.Id;
                continue;
            }

            if (unit.DistanceAlongPath == bestDistanceAlongPath && unit.Id < bestUnitId)
            {
                bestIndex = index;
                bestUnitId = unit.Id;
            }
        }

        return bestIndex;
    }

    private static CombatEvent[] CombineCombatEvents(CombatEvent[] unitPhaseEvents, CombatEvent[] towerPhaseEvents)
    {
        if (unitPhaseEvents.Length == 0)
        {
            return towerPhaseEvents;
        }

        if (towerPhaseEvents.Length == 0)
        {
            return unitPhaseEvents;
        }

        var combined = new CombatEvent[unitPhaseEvents.Length + towerPhaseEvents.Length];
        Array.Copy(unitPhaseEvents, combined, unitPhaseEvents.Length);
        Array.Copy(towerPhaseEvents, 0, combined, unitPhaseEvents.Length, towerPhaseEvents.Length);
        return combined;
    }

    private readonly record struct UnitPhaseResult(
        Unit[] UnitsAfterActions,
        Tower[] TowersAfterActions,
        Damage BaseDamage,
        TowerDestroyed[] DestroyedTowers,
        CombatEvent[] CombatEvents);

    private sealed record TowerAttackResult(
        Unit[] UnitsAfterAttacks,
        Tower[] TowersAfterAttacks,
        UnitKilled[] KilledUnits,
        CombatEvent[] CombatEvents);
}

using System;
using TowerFluffy.Domain.Simulation;
using TowerFluffy.Domain.Match;
using Xunit;

namespace TowerFluffy.Domain.Tests;

public sealed class GameStateTests
{
    [Fact]
    public void AdvanceOneTick_IncrementsTick()
    {
        var map = CreateStraightMap(length: 3_000);
        var state = new GameState(new Tick(0), new Health(10), Array.Empty<Unit>(), Array.Empty<Tower>());

        var next = state.AdvanceOneTick(map).State;

        Assert.Equal(1, next.Tick.Value);
    }

    [Fact]
    public void AdvanceOneTick_MovesUnitsForward()
    {
        var map = CreateStraightMap(length: 3_000);
        var unit = new Unit(
            Id: 1,
            DistanceAlongPath: 0,
            SpeedPerTick: 1_000,
            DamageToBase: new Damage(1),
            DamageToTower: new Damage(0),
            AttackRange: 0,
            AttackCooldownTicksBetweenAttacks: 0,
            AttackCooldownTicksRemaining: 0,
            TargetTowerId: null,
            Health: new Health(1),
            LootGold: new Gold(0));
        var state = new GameState(new Tick(0), new Health(10), new[] { unit }, Array.Empty<Tower>());

        var next = state.AdvanceOneTick(map).State;

        Assert.Single(next.Units);
        Assert.Equal(1_000, next.Units[0].DistanceAlongPath);
        Assert.Equal(new WorldPosition(1_000, 0), map.Path.GetPositionAtDistance(next.Units[0].DistanceAlongPath));
    }

    [Fact]
    public void AdvanceOneTick_WhenUnitReachesEnd_DamagesBaseAndRemovesUnit()
    {
        var map = CreateStraightMap(length: 3_000);
        var unit = new Unit(
            Id: 1,
            DistanceAlongPath: 2_500,
            SpeedPerTick: 1_000,
            DamageToBase: new Damage(3),
            DamageToTower: new Damage(0),
            AttackRange: 0,
            AttackCooldownTicksBetweenAttacks: 0,
            AttackCooldownTicksRemaining: 0,
            TargetTowerId: null,
            Health: new Health(1),
            LootGold: new Gold(0));
        var state = new GameState(new Tick(0), new Health(10), new[] { unit }, Array.Empty<Tower>());

        var tick = state.AdvanceOneTick(map);
        var next = tick.State;

        Assert.Empty(next.Units);
        Assert.Equal(7, next.BaseHealth.Value);

        Assert.Single(tick.CombatEvents);
        var e = tick.CombatEvents[0];
        Assert.Equal(new Tick(1), e.Tick);
        Assert.Equal(CombatEventKind.UnitHitBase, e.Kind);
        Assert.Equal(unit.Id, e.SourceId);
        Assert.Null(e.TargetId);
        Assert.Equal(new WorldPosition(2_500, 0), e.From);
        Assert.Equal(map.Path.End, e.To);
        Assert.Equal(unit.DamageToBase, e.Damage);
        Assert.False(e.TargetDestroyed);
    }

    [Fact]
    public void AdvanceOneTick_WhenUnitCanAttackTower_UnitStopsAndDamagesTower()
    {
        var map = CreateStraightMapThroughCellCenters(length: 3_000, cellSize: 100);
        var unit = new Unit(
            Id: 1,
            DistanceAlongPath: 0,
            SpeedPerTick: 1_000,
            DamageToBase: new Damage(1),
            DamageToTower: new Damage(3),
            AttackRange: 100,
            AttackCooldownTicksBetweenAttacks: 1,
            AttackCooldownTicksRemaining: 0,
            TargetTowerId: null,
            Health: new Health(10),
            LootGold: new Gold(0));
        var tower = new Tower(
            Id: 2,
            Type: TowerType.BasicShooter,
            Position: new GridPosition(0, 1),
            Stats: new TowerStats(Cost: new Gold(0), DamagePerShot: new Damage(0), Range: 0, CooldownTicksBetweenShots: 0),
            Health: new Health(10),
            CooldownTicksRemaining: 0);
        var state = new GameState(new Tick(0), new Health(10), new[] { unit }, new[] { tower });

        var tick = state.AdvanceOneTick(map);

        Assert.Single(tick.State.Units);
        Assert.Equal(0, tick.State.Units[0].DistanceAlongPath);
        Assert.Single(tick.State.Towers);
        Assert.Equal(7, tick.State.Towers[0].Health.Value);
        Assert.Empty(tick.TowersDestroyed);

        Assert.Single(tick.CombatEvents);
        var e = tick.CombatEvents[0];
        Assert.Equal(new Tick(1), e.Tick);
        Assert.Equal(CombatEventKind.UnitAttackTower, e.Kind);
        Assert.Equal(unit.Id, e.SourceId);
        Assert.Equal(tower.Id, e.TargetId);
        Assert.Equal(new WorldPosition(50, 50), e.From);
        Assert.Equal(new WorldPosition(50, 150), e.To);
        Assert.Equal(unit.DamageToTower, e.Damage);
        Assert.False(e.TargetDestroyed);
    }

    [Fact]
    public void AdvanceOneTick_WhenTowerDestroyed_RemovesTowerAndReportsDestruction()
    {
        var map = CreateStraightMapThroughCellCenters(length: 3_000, cellSize: 100);
        var unit = new Unit(
            Id: 1,
            DistanceAlongPath: 0,
            SpeedPerTick: 1_000,
            DamageToBase: new Damage(1),
            DamageToTower: new Damage(5),
            AttackRange: 100,
            AttackCooldownTicksBetweenAttacks: 1,
            AttackCooldownTicksRemaining: 0,
            TargetTowerId: null,
            Health: new Health(10),
            LootGold: new Gold(0));
        var tower = new Tower(
            Id: 2,
            Type: TowerType.BasicShooter,
            Position: new GridPosition(0, 1),
            Stats: new TowerStats(Cost: new Gold(0), DamagePerShot: new Damage(0), Range: 0, CooldownTicksBetweenShots: 0),
            Health: new Health(4),
            CooldownTicksRemaining: 0);
        var state = new GameState(new Tick(0), new Health(10), new[] { unit }, new[] { tower });

        var tick = state.AdvanceOneTick(map);

        Assert.Empty(tick.State.Towers);
        Assert.Single(tick.TowersDestroyed);
        Assert.Equal(2, tick.TowersDestroyed[0].TowerId);

        Assert.Single(tick.CombatEvents);
        var e = tick.CombatEvents[0];
        Assert.Equal(new Tick(1), e.Tick);
        Assert.Equal(CombatEventKind.UnitAttackTower, e.Kind);
        Assert.Equal(unit.Id, e.SourceId);
        Assert.Equal(tower.Id, e.TargetId);
        Assert.Equal(new WorldPosition(50, 50), e.From);
        Assert.Equal(new WorldPosition(50, 150), e.To);
        Assert.Equal(unit.DamageToTower, e.Damage);
        Assert.True(e.TargetDestroyed);
    }

    [Fact]
    public void AdvanceOneTick_WhenTowerShootsUnit_ReportsCombatEvent()
    {
        var map = CreateStraightMapThroughCellCenters(length: 3_000, cellSize: 100);
        var unit = new Unit(
            Id: 1,
            DistanceAlongPath: 0,
            SpeedPerTick: 0,
            DamageToBase: new Damage(1),
            DamageToTower: new Damage(0),
            AttackRange: 0,
            AttackCooldownTicksBetweenAttacks: 0,
            AttackCooldownTicksRemaining: 0,
            TargetTowerId: null,
            Health: new Health(10),
            LootGold: new Gold(0));
        var tower = new Tower(
            Id: 2,
            Type: TowerType.BasicShooter,
            Position: new GridPosition(0, 1),
            Stats: new TowerStats(Cost: new Gold(0), DamagePerShot: new Damage(3), Range: 10_000, CooldownTicksBetweenShots: 0),
            Health: new Health(10),
            CooldownTicksRemaining: 0);
        var state = new GameState(new Tick(0), new Health(10), new[] { unit }, new[] { tower });

        var tick = state.AdvanceOneTick(map);

        Assert.Single(tick.CombatEvents);
        var e = tick.CombatEvents[0];
        Assert.Equal(new Tick(1), e.Tick);
        Assert.Equal(CombatEventKind.TowerShot, e.Kind);
        Assert.Equal(tower.Id, e.SourceId);
        Assert.Equal(unit.Id, e.TargetId);
        Assert.Equal(new WorldPosition(50, 150), e.From);
        Assert.Equal(new WorldPosition(50, 50), e.To);
        Assert.Equal(tower.Stats.DamagePerShot, e.Damage);
        Assert.False(e.TargetDestroyed);
    }

    private static Map CreateStraightMap(int length)
    {
        var path = new TowerFluffy.Domain.Simulation.Path(new[] { new WorldPosition(0, 0), new WorldPosition(length, 0) });
        var grid = new Grid(Width: 10, Height: 10, CellSize: 100);
        return new Map(path, grid, blockedCells: Array.Empty<GridPosition>());
    }

    private static Map CreateStraightMapThroughCellCenters(int length, int cellSize)
    {
        var start = new WorldPosition(cellSize / 2, cellSize / 2);
        var end = new WorldPosition((cellSize / 2) + length, cellSize / 2);
        var path = new TowerFluffy.Domain.Simulation.Path(new[] { start, end });
        var grid = new Grid(Width: 10, Height: 10, CellSize: cellSize);
        return new Map(path, grid, blockedCells: Array.Empty<GridPosition>());
    }
}

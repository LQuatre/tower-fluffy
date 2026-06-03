using TowerFluffy.Application.Game;
using TowerFluffy.Domain.Combat;
using TowerFluffy.Domain.Shared;
using TowerFluffy.Domain.Match;
using TowerFluffy.Domain.Engine;
using TowerFluffy.Domain.Environment;
using System;
using Xunit;

namespace TowerFluffy.Application.Tests;

public sealed class GameSessionTests
{
    [Fact]
    public void NewSession_StartsInPreparation()
    {
        var session = new GameSession(CreateTestConfig(), DefaultMapFactory.Create());

        Assert.Equal(MatchPhase.Preparation, session.State.Phase);
        Assert.Equal(0, session.State.WaveNumber);
    }

    [Fact]
    public void SkipPreparation_TransitionsToWave()
    {
        var session = new GameSession(CreateTestConfig(), DefaultMapFactory.Create());

        var result = session.SkipPreparation();

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchPhase.Wave, session.State.Phase);
        Assert.Equal(1, session.State.WaveNumber);
    }

    [Fact]
    public void PlaceTower_AddsTower()
    {
        var session = new GameSession(CreateTestConfig(), DefaultMapFactory.Create());

        var result = session.PlaceTower(TowerType.BasicShooter, new GridPosition(0, 0));

        Assert.True(result.IsSuccess);
        Assert.Single(session.State.Simulation.Towers);
    }

    [Fact]
    public void Tick_WhenTowerShootsUnit_SnapshotIncludesCombatEvent()
    {
        var path = new TowerFluffy.Domain.Environment.Path(new[] { new WorldPosition(20, 220), new WorldPosition(200, 220) });
        var grid = new Grid(Width: 16, Height: 10, CellSize: 40);
        var map = new Map(path, grid, blockedCells: Array.Empty<GridPosition>());
        var session = new GameSession(CreateTestConfig(), map);

        Assert.True(session.PlaceTower(TowerType.BasicShooter, new GridPosition(0, 4)).IsSuccess);
        Assert.True(session.SkipPreparation().IsSuccess);
        Assert.True(session.SendUnit(UnitType.Soldat).IsSuccess);

        session.Tick(1);

        Assert.Equal(1, session.State.Tick.Value);
        Assert.Single(session.State.LastCombatEvents);

        var e = session.State.LastCombatEvents[0];
        Assert.Equal(new Tick(1), e.Tick);
        Assert.Equal(CombatEventKind.TowerShot, e.Kind);
        Assert.Equal(new WorldPosition(20, 180), e.From);
        Assert.Equal(new WorldPosition(20, 220), e.To);
        Assert.Equal(new Damage(1), e.Damage);
        Assert.True(e.TargetDestroyed);
    }

    private static GameConfig CreateTestConfig()
    {
        return new GameConfig(
            TotalWaves: 1,
            PreparationTicks: 3,
            WaveSendWindowTicks: 3,
            BaseWaveBudget: new Budget(10),
            WaveBudgetIncrement: new Budget(0),
            BudgetBonusPerTowerDestroyed: new Budget(0),
            StartingGold: new Gold(100),
            StartingBaseHealth: new Health(10),
            GoldPerBaseDamageTaken: 0,
            Towers: new[]
            {
                new TowerDefinition(
                    TowerType.BasicShooter,
                    new TowerStats(
                        Cost: new Gold(10),
                        DamagePerShot: new Damage(1),
                        Range: 10_000,
                        CooldownTicksBetweenShots: 1),
                    Health: new Health(10)),
            },
            Units: new[]
            {
                new UnitDefinition(
                    UnitType.Soldat,
                    Cost: new Budget(10),
                    Health: new Health(1),
                    SpeedPerTick: 0,
                    DamageToBase: new Damage(1),
                    DamageToTower: new Damage(0),
                    AttackRange: 0,
                    AttackCooldownTicksBetweenAttacks: 1,
                    LootGold: new Gold(0)),
            });
    }
}

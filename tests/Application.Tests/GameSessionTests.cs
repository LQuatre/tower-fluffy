using TowerFluffy.Application.Game;
using TowerFluffy.Application.Game.Dtos;
using TowerFluffy.Domain.Match;
using TowerFluffy.Domain.Simulation;
using Xunit;

namespace TowerFluffy.Application.Tests;

public sealed class GameSessionTests
{
    [Fact]
    public void NewSession_StartsInPreparation()
    {
        var session = new GameSession(CreateTestConfig(), DefaultMapFactory.Create());

        Assert.Equal(MatchPhaseDto.Preparation, session.Snapshot.Hud.Phase);
        Assert.Equal(0, session.Snapshot.Hud.WaveNumber);
    }

    [Fact]
    public void SkipPreparation_TransitionsToWave()
    {
        var session = new GameSession(CreateTestConfig(), DefaultMapFactory.Create());

        var result = session.SkipPreparation();

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchPhaseDto.Wave, session.Snapshot.Hud.Phase);
        Assert.Equal(1, session.Snapshot.Hud.WaveNumber);
    }

    [Fact]
    public void PlaceTower_AddsTower()
    {
        var session = new GameSession(CreateTestConfig(), DefaultMapFactory.Create());

        var result = session.PlaceTower(TowerTypeDto.BasicShooter, new GridPositionDto(0, 0));

        Assert.True(result.IsSuccess);
        Assert.Single(session.Snapshot.Towers);
    }

    [Fact]
    public void Tick_WhenTowerShootsUnit_SnapshotIncludesCombatEvent()
    {
        var session = new GameSession(CreateTestConfig(), DefaultMapFactory.Create());

        Assert.True(session.PlaceTower(TowerTypeDto.BasicShooter, new GridPositionDto(0, 4)).IsSuccess);
        Assert.True(session.SkipPreparation().IsSuccess);
        Assert.True(session.SendUnit(UnitTypeDto.Soldat).IsSuccess);

        session.Tick(1);

        Assert.Equal(1, session.Snapshot.Hud.Tick);
        Assert.Single(session.Snapshot.CombatEvents);

        var e = session.Snapshot.CombatEvents[0];
        Assert.Equal(1, e.Tick);
        Assert.Equal(CombatEventKindDto.TowerShot, e.Kind);
        Assert.Equal(new WorldPositionDto(20, 180), e.From);
        Assert.Equal(new WorldPositionDto(20, 220), e.To);
        Assert.Equal(1, e.Damage);
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

using System;
using TowerFluffy.Domain.Match;
using TowerFluffy.Domain.Simulation;
using Xunit;

namespace TowerFluffy.Domain.Tests;

public sealed class MatchStateTests
{
    [Fact]
    public void CreateNew_StartsInPreparation()
    {
        var config = CreateTestConfig();
        var map = DefaultMapFactory.Create();

        var match = MatchState.CreateNew(config, map);

        Assert.Equal(MatchPhase.Preparation, match.Phase);
        Assert.Equal(0, match.WaveNumber);
        Assert.Equal(MatchOutcome.None, match.Outcome);
        Assert.Equal(config.StartingGold, match.DefenderGold);
        Assert.Equal(Budget.Zero, match.AttackerBudget);
        Assert.Equal(config.PreparationTicks, match.PreparationTicksRemaining);
    }

    [Fact]
    public void SkipPreparation_StartsWaveAndAllocatesBudget()
    {
        var config = CreateTestConfig();
        var map = DefaultMapFactory.Create();
        var match = MatchState.CreateNew(config, map);

        var started = match.SkipPreparation();

        Assert.True(started.IsSuccess);
        Assert.Equal(MatchPhase.Wave, started.Value!.Phase);
        Assert.Equal(1, started.Value.WaveNumber);
        Assert.Equal(config.GetWaveBudget(1), started.Value.AttackerBudget);
        Assert.Equal(config.WaveSendWindowTicks, started.Value.WaveSendTicksRemaining);
    }

    [Fact]
    public void PlaceTower_SpendsGold_AndAddsTower()
    {
        var config = CreateTestConfig();
        var map = DefaultMapFactory.Create();
        var match = MatchState.CreateNew(config, map);

        var result = match.PlaceTower(TowerType.BasicShooter, new GridPosition(0, 0));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Simulation.Towers);
        Assert.True(result.Value.DefenderGold.Value < match.DefenderGold.Value);
    }

    [Fact]
    public void SendUnit_SpendsBudget_AndAddsUnit()
    {
        var config = CreateTestConfig();
        var map = DefaultMapFactory.Create();
        var match = MatchState.CreateNew(config, map);
        var started = match.SkipPreparation().Value!;

        var sent = started.SendUnit(UnitType.Grunt);

        Assert.True(sent.IsSuccess);
        Assert.Single(sent.Value!.Simulation.Units);
        Assert.True(sent.Value.AttackerBudget.Value < started.AttackerBudget.Value);
    }

    [Fact]
    public void Tick_WhenTowerKillsUnit_AwardsGold()
    {
        var config = CreateTestConfig(
            towerDamage: 10,
            towerCooldown: 0,
            unitDamageToTower: 0,
            unitAttackRange: 0,
            unitHealth: 3,
            unitLootGold: 7,
            unitSpeedPerTick: 0);
        var map = DefaultMapFactory.Create();

        var match = MatchState.CreateNew(config, map)
            .PlaceTower(TowerType.BasicShooter, new GridPosition(0, 4)).Value!
            .SkipPreparation().Value!
            .SendUnit(UnitType.Grunt).Value!;

        var goldBefore = match.DefenderGold;
        var after = match.Tick();

        Assert.Empty(after.Simulation.Units);
        Assert.True(after.DefenderGold.Value > goldBefore.Value);
    }

    [Fact]
    public void Tick_WhenUnitDestroysTower_AwardsAttackerBudgetBonus()
    {
        var config = CreateTestConfig(
            towerDamage: 0,
            towerCooldown: 0,
            towerHealth: 2,
            budgetBonusPerTowerDestroyed: 7,
            unitHealth: 10,
            unitSpeedPerTick: 0,
            unitDamageToTower: 5,
            unitAttackRange: 10_000,
            unitAttackCooldown: 0);
        var map = DefaultMapFactory.Create();

        var match = MatchState.CreateNew(config, map)
            .PlaceTower(TowerType.BasicShooter, new GridPosition(0, 4)).Value!
            .SkipPreparation().Value!
            .SendUnit(UnitType.Grunt).Value!;

        var budgetBefore = match.AttackerBudget;
        var after = match.Tick();

        Assert.Empty(after.Simulation.Towers);
        Assert.Equal(budgetBefore.Value + 7, after.AttackerBudget.Value);
    }

    private static GameConfig CreateTestConfig(
        int towerDamage = 1,
        int towerCooldown = 1,
        int towerHealth = 10,
        int budgetBonusPerTowerDestroyed = 0,
        int unitHealth = 1,
        int unitLootGold = 0,
        int unitSpeedPerTick = 1,
        int unitDamageToTower = 0,
        int unitAttackRange = 0,
        int unitAttackCooldown = 1)
    {
        return new GameConfig(
            TotalWaves: 2,
            PreparationTicks: 3,
            WaveSendWindowTicks: 3,
            BaseWaveBudget: new Budget(100),
            WaveBudgetIncrement: new Budget(0),
            BudgetBonusPerTowerDestroyed: new Budget(budgetBonusPerTowerDestroyed),
            StartingGold: new Gold(100),
            StartingBaseHealth: new Health(10),
            Towers: new[]
            {
                new TowerDefinition(
                    TowerType.BasicShooter,
                    new TowerStats(
                        Cost: new Gold(10),
                        DamagePerShot: new Damage(towerDamage),
                        Range: 10_000,
                        CooldownTicksBetweenShots: towerCooldown),
                    Health: new Health(towerHealth)),
            },
            Units: new[]
            {
                new UnitDefinition(
                    UnitType.Grunt,
                    Cost: new Budget(10),
                    Health: new Health(unitHealth),
                    SpeedPerTick: unitSpeedPerTick,
                    DamageToBase: new Damage(1),
                    DamageToTower: new Damage(unitDamageToTower),
                    AttackRange: unitAttackRange,
                    AttackCooldownTicksBetweenAttacks: unitAttackCooldown,
                    LootGold: new Gold(unitLootGold)),
            });
    }
}

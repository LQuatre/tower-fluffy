using System;
using System.Collections.Generic;
using System.Linq;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Domain.Match;

public sealed record GameConfig(
    int TotalWaves,
    int PreparationTicks,
    int WaveSendWindowTicks,
    Budget BaseWaveBudget,
    Budget WaveBudgetIncrement,
    Budget BudgetBonusPerTowerDestroyed,
    Gold StartingGold,
    Health StartingBaseHealth,
    int GoldPerBaseDamageTaken,
    IReadOnlyList<TowerDefinition> Towers,
    IReadOnlyList<UnitDefinition> Units)
{
    public static GameConfig CreateMvpDefaults()
    {
        return new GameConfig(
            TotalWaves: 10,
            PreparationTicks: 60 * 30,
            WaveSendWindowTicks: 60 * 20,
            BaseWaveBudget: new Budget(80),
            WaveBudgetIncrement: new Budget(20),
            BudgetBonusPerTowerDestroyed: new Budget(10),
            StartingGold: new Gold(300),
            StartingBaseHealth: new Health(100),
            GoldPerBaseDamageTaken: 10,
            Towers: new[]
            {
                new TowerDefinition(
                    TowerType.BasicShooter,
                    new TowerStats(
                        Cost: new Gold(50),
                        DamagePerShot: new Damage(2),
                        Range: 220,
                        CooldownTicksBetweenShots: 20),
                    Health: new Health(40)),
                new TowerDefinition(
                    TowerType.Flamethrower,
                    new TowerStats(
                        Cost: new Gold(200),
                        DamagePerShot: new Damage(1),
                        Range: 160,
                        CooldownTicksBetweenShots: 12),
                    Health: new Health(50)),
            },
            Units: new[]
            {
                new UnitDefinition(
                    UnitType.Soldat,
                    Cost: new Budget(12),
                    Health: new Health(5),
                    SpeedPerTick: 3,
                    DamageToBase: new Damage(1),
                    DamageToTower: new Damage(2),
                    AttackRange: 150,
                    AttackCooldownTicksBetweenAttacks: 40,
                    LootGold: new Gold(10)),
                new UnitDefinition(
                    UnitType.Brute,
                    Cost: new Budget(50),
                    Health: new Health(40),
                    SpeedPerTick: 1,
                    DamageToBase: new Damage(5),
                    DamageToTower: new Damage(10),
                    AttackRange: 180,
                    AttackCooldownTicksBetweenAttacks: 60,
                    LootGold: new Gold(30)),
            });
    }

    public Budget GetWaveBudget(int waveNumber)
    {
        if (waveNumber < 1 || waveNumber > TotalWaves)
        {
            throw new ArgumentOutOfRangeException(nameof(waveNumber), waveNumber, "Le numéro de vague est hors limites.");
        }

        return BaseWaveBudget.Add(new Budget((waveNumber - 1) * WaveBudgetIncrement.Value));
    }

    public TowerDefinition GetTower(TowerType type)
    {
        var tower = Towers.FirstOrDefault(t => t.Type == type);
        if (EqualityComparer<TowerDefinition>.Default.Equals(tower, default))
        {
            throw new InvalidOperationException($"Type de tour non configuré : {type}");
        }

        return tower;
    }

    public UnitDefinition GetUnit(UnitType type)
    {
        var unit = Units.FirstOrDefault(u => u.Type == type);
        if (EqualityComparer<UnitDefinition>.Default.Equals(unit, default))
        {
            throw new InvalidOperationException($"Type d'unité non configuré : {type}");
        }

        return unit;
    }
}

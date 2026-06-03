using System;
using System.Collections.Generic;
using System.Linq;
using TowerFluffy.Domain.Combat;
using TowerFluffy.Domain.Shared;

namespace TowerFluffy.UI.Desktop.Services;

public sealed class MatchAnalyzer
{
    private readonly Dictionary<TowerType, int> _towersPlaced = new();
    private readonly Dictionary<TowerType, int> _towersSold = new();
    private readonly Dictionary<TowerType, int> _towerDamage = new();
    private readonly Dictionary<TowerType, int> _towerKills = new();

    private readonly Dictionary<UnitType, int> _unitsSent = new();
    private readonly Dictionary<UnitType, int> _unitsReachedBase = new();
    private readonly Dictionary<UnitType, int> _unitsKilled = new();

    private int _endingGold;
    private int _startingGold;
    private int _endingBaseHealth;
    private int _waveReached;

    public void Reset(int startingGold)
    {
        _towersPlaced.Clear();
        _towersSold.Clear();
        _towerDamage.Clear();
        _towerKills.Clear();

        _unitsSent.Clear();
        _unitsReachedBase.Clear();
        _unitsKilled.Clear();

        _startingGold = startingGold;
        _endingGold = startingGold;
        _endingBaseHealth = 100;
        _waveReached = 1;
    }

    public void RecordTowerPlaced(TowerType type)
    {
        _towersPlaced[type] = _towersPlaced.GetValueOrDefault(type) + 1;
    }

    public void RecordTowerSold(TowerType type)
    {
        _towersSold[type] = _towersSold.GetValueOrDefault(type) + 1;
    }

    public void RecordUnitSent(UnitType type)
    {
        _unitsSent[type] = _unitsSent.GetValueOrDefault(type) + 1;
    }

    public void RecordEvents(IReadOnlyList<CombatEvent> events)
    {
        foreach (var ev in events)
        {
            if (ev.Kind == CombatEventKind.TowerShot && ev.SourceTowerType.HasValue)
            {
                var type = ev.SourceTowerType.Value;
                _towerDamage[type] = _towerDamage.GetValueOrDefault(type) + ev.Damage.Value;
                if (ev.TargetDestroyed)
                {
                    _towerKills[type] = _towerKills.GetValueOrDefault(type) + 1;
                }
            }
        }
    }

    public void RecordEndState(int gold, int baseHealth, int wave)
    {
        _endingGold = gold;
        _endingBaseHealth = baseHealth;
        _waveReached = wave;
    }

    public List<string> GenerateSuggestions()
    {
        var suggestions = new List<string>();

        // Heuristic 1: Tower damage share
        int totalTowerDamage = _towerDamage.Values.Sum();
        if (totalTowerDamage > 0)
        {
            foreach (var kvp in _towerDamage)
            {
                var type = kvp.Key;
                var dmg = kvp.Value;
                double percentage = (dmg * 100.0) / totalTowerDamage;
                int built = _towersPlaced.GetValueOrDefault(type);

                string towerName = GetFrenchTowerName(type);

                if (percentage > 60 && built >= 2)
                {
                    suggestions.Add($"⚠️ **{towerName} dominant** : Il a infligé {percentage:F0}% des dégâts totaux. Pensez à augmenter son coût (+20%) ou à réduire ses dégâts.");
                }
                else if (percentage < 8 && built >= 2)
                {
                    suggestions.Add($"💡 **{towerName} faible** : Seulement {percentage:F0}% des dégâts totaux malgré {built} constructions. Pensez à réduire son coût ou à augmenter ses dégâts.");
                }
            }
        }
        else
        {
            suggestions.Add("💡 Aucune tour n'a infligé de dégâts. Construisez des défenses pour repousser les vagues !");
        }

        // Heuristic 2: Economy
        if (_endingBaseHealth > 0) // Victoire
        {
            if (_endingGold > 800)
            {
                suggestions.Add($"💡 **Économie trop facile** : Le défenseur a terminé avec {_endingGold} CR restants. Réduisez le butin (Bounty) des unités de 20% pour relever le défi.");
            }
        }
        else // Défaite
        {
            if (_waveReached <= 3)
            {
                suggestions.Add($"💡 **Défaite précoce** (Vague {_waveReached}) : Le défenseur a manqué de ressources. Augmentez l'or de départ ou le butin des unités de base.");
            }
            else
            {
                suggestions.Add($"💡 **Défaite tardive** (Vague {_waveReached}) : Ajustez les PV des vagues de fin ou réduisez le délai de recharge des canons/lasers.");
            }
        }

        // Heuristic 3: Unit usage
        foreach (var kvp in _unitsSent)
        {
            var type = kvp.Key;
            var count = kvp.Value;
            string unitName = GetFrenchUnitName(type);
            
            if (count >= 5)
            {
                suggestions.Add($"👾 **Troupes d'assaut** : {count}x '{unitName}' déployés. Si la vague a été trop simple à repousser, augmentez la puissance de feu de vos tours.");
            }
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add("✨ Vos réglages actuels semblent équilibrés pour cette configuration de jeu !");
        }

        return suggestions;
    }

    private string GetFrenchTowerName(TowerType type) => type switch
    {
        TowerType.BasicShooter => "Mitrailleuse Standard",
        TowerType.Flamethrower => "Lance-flammes",
        TowerType.Sniper => "Sniper",
        TowerType.Cannon => "Canon",
        TowerType.Laser => "Laser",
        _ => type.ToString()
    };

    private string GetFrenchUnitName(UnitType type) => type switch
    {
        UnitType.Soldat => "Soldat d'assaut",
        UnitType.Brute => "Brute lourde",
        UnitType.Rapide => "Éclaireur rapide",
        UnitType.TireurElite => "Tireur d'élite",
        UnitType.Tank => "Tank blindé",
        _ => type.ToString()
    };
}

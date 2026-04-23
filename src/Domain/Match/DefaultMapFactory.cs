using System.Collections.Generic;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Domain.Match;

public static class DefaultMapFactory
{
    public static Map Create(int level = 1, int? seed = null)
    {
        const int width = 16;
        const int height = 10;
        const int cellSize = 40;
        
        // Si un seed est fourni, on l'utilise pour la reproductibilité (multiplayer)
        // Sinon on utilise l'aléatoire par défaut (solo)
        var rand = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

        var grid = new Grid(Width: width, Height: height, CellSize: cellSize);
        var pathCells = new List<GridPosition>();
        var blocked = new HashSet<GridPosition>();

        // 1. Génération du chemin (Random Walk)
        var currentY = rand.Next(2, height - 2);
        var current = new GridPosition(0, currentY);
        pathCells.Add(current);
        blocked.Add(current);

        while (current.X < width - 1)
        {
            // Déterminer les poids des directions
            // Plus le niveau est élevé, plus on a de chances de faire des détours verticaux
            double verticalChance = Math.Min(0.45, (level - 1) * 0.05 + 0.1);
            
            bool moveVertical = rand.NextDouble() < verticalChance;
            
            if (moveVertical)
            {
                int dy = rand.Next(0, 2) == 0 ? -1 : 1;
                var next = new GridPosition(current.X, current.Y + dy);
                
                // Vérifier les limites et ne pas revenir en arrière ou se coller au chemin existant
                if (next.Y >= 1 && next.Y < height - 1 && !blocked.Contains(next))
                {
                    current = next;
                    pathCells.Add(current);
                    blocked.Add(current);
                    continue; // On peut encore bouger verticalement ou repartir à droite
                }
            }

            // Toujours avancer à droite si on ne bouge pas verticalement
            current = new GridPosition(current.X + 1, current.Y);
            pathCells.Add(current);
            blocked.Add(current);
        }

        // 2. Conversion des cellules en Waypoints (WorldPositions)
        // On simplifie le chemin pour n'avoir que les points de virage
        var waypoints = new List<WorldPosition>();
        foreach (var cell in pathCells)
        {
            waypoints.Add(new WorldPosition(
                (cell.X * cellSize) + (cellSize / 2),
                (cell.Y * cellSize) + (cellSize / 2)));
        }

        var path = new TowerFluffy.Domain.Simulation.Path(waypoints.ToArray());

        return new Map(path, grid, blocked);
    }
}

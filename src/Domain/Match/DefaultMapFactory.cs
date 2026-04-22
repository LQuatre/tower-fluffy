using System.Collections.Generic;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Domain.Match;

public static class DefaultMapFactory
{
    public static Map Create(int level = 1)
    {
        const int width = 16;
        const int height = 10;
        const int cellSize = 40;

        var grid = new Grid(Width: width, Height: height, CellSize: cellSize);
        var blocked = new List<GridPosition>();
        TowerFluffy.Domain.Simulation.Path path;

        if (level == 2)
        {
            // L-shape path
            path = new TowerFluffy.Domain.Simulation.Path(new[]
            {
                new WorldPosition(cellSize / 2, (height / 2) * cellSize + cellSize / 2),
                new WorldPosition((width / 2) * cellSize + cellSize / 2, (height / 2) * cellSize + cellSize / 2),
                new WorldPosition((width / 2) * cellSize + cellSize / 2, cellSize / 2),
                new WorldPosition((width - 1) * cellSize + cellSize / 2, cellSize / 2),
            });

            // Block path cells
            for (int x = 0; x <= width / 2; x++) blocked.Add(new GridPosition(x, height / 2));
            for (int y = 0; y <= height / 2; y++) blocked.Add(new GridPosition(width / 2, y));
            for (int x = width / 2; x < width; x++) blocked.Add(new GridPosition(x, 0));
        }
        else if (level >= 3)
        {
            // S-shape path
            path = new TowerFluffy.Domain.Simulation.Path(new[]
            {
                new WorldPosition(cellSize / 2, 2 * cellSize + cellSize / 2),
                new WorldPosition((width - 2) * cellSize + cellSize / 2, 2 * cellSize + cellSize / 2),
                new WorldPosition((width - 2) * cellSize + cellSize / 2, (height - 2) * cellSize + cellSize / 2),
                new WorldPosition(cellSize / 2, (height - 2) * cellSize + cellSize / 2),
            });

            for (int x = 0; x < width - 1; x++) blocked.Add(new GridPosition(x, 2));
            for (int y = 2; y < height - 1; y++) blocked.Add(new GridPosition(width - 2, y));
            for (int x = 0; x < width - 1; x++) blocked.Add(new GridPosition(x, height - 2));
        }
        else
        {
            // Level 1: Straight line
            const int pathRow = 5;
            var y = (pathRow * cellSize) + (cellSize / 2);
            path = new TowerFluffy.Domain.Simulation.Path(new[]
            {
                new WorldPosition(cellSize / 2, y),
                new WorldPosition((width * cellSize) - (cellSize / 2), y),
            });

            for (var x = 0; x < width; x++) blocked.Add(new GridPosition(x, pathRow));
        }

        return new Map(path, grid, blocked);
    }
}

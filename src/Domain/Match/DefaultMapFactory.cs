using System.Collections.Generic;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Domain.Match;

public static class DefaultMapFactory
{
    public static Map Create()
    {
        const int width = 16;
        const int height = 10;
        const int cellSize = 40;
        const int pathRow = 5;

        var grid = new Grid(Width: width, Height: height, CellSize: cellSize);
        var y = (pathRow * cellSize) + (cellSize / 2);

        var path = new TowerFluffy.Domain.Simulation.Path(new[]
        {
            new WorldPosition(cellSize / 2, y),
            new WorldPosition((width * cellSize) - (cellSize / 2), y),
        });

        var blocked = new List<GridPosition>(width);
        for (var x = 0; x < width; x++)
        {
            blocked.Add(new GridPosition(x, pathRow));
        }

        return new Map(path, grid, blocked);
    }
}

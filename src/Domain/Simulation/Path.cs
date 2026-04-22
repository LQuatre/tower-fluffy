using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerFluffy.Domain.Simulation;

public sealed class Path
{
    private readonly WorldPosition[] _waypoints;
    private readonly int _totalLength;

    public Path(IEnumerable<WorldPosition> waypoints)
    {
        if (waypoints is null)
        {
            throw new ArgumentNullException(nameof(waypoints));
        }

        _waypoints = waypoints.ToArray();
        if (_waypoints.Length < 2)
        {
            throw new ArgumentException("A path must contain at least two waypoints.", nameof(waypoints));
        }

        _totalLength = CalculateTotalLength(_waypoints);
    }

    public IReadOnlyList<WorldPosition> Waypoints => _waypoints;
    public int TotalLength => _totalLength;
    public WorldPosition Start => _waypoints[0];
    public WorldPosition End => _waypoints[^1];

    public WorldPosition GetPositionAtDistance(int distance)
    {
        if (distance <= 0)
        {
            return Start;
        }

        if (distance >= _totalLength)
        {
            return End;
        }

        var remaining = distance;
        for (var index = 0; index < _waypoints.Length - 1; index++)
        {
            var segmentStart = _waypoints[index];
            var segmentEnd = _waypoints[index + 1];
            var segmentLength = GetSegmentLength(segmentStart, segmentEnd);

            if (remaining <= segmentLength)
            {
                return GetPositionOnSegment(segmentStart, segmentEnd, remaining);
            }

            remaining -= segmentLength;
        }

        return End;
    }

    public WorldPosition GetDirectionAtDistance(int distance)
    {
        if (distance <= 0)
        {
            return GetSegmentDirection(0);
        }

        if (distance >= _totalLength)
        {
            return GetSegmentDirection(_waypoints.Length - 2);
        }

        var remaining = distance;
        for (var index = 0; index < _waypoints.Length - 1; index++)
        {
            var segmentStart = _waypoints[index];
            var segmentEnd = _waypoints[index + 1];
            var segmentLength = GetSegmentLength(segmentStart, segmentEnd);

            if (remaining < segmentLength)
            {
                return GetSegmentDirection(index);
            }

            remaining -= segmentLength;
        }

        return GetSegmentDirection(_waypoints.Length - 2);
    }

    private WorldPosition GetSegmentDirection(int segmentIndex)
    {
        var start = _waypoints[segmentIndex];
        var end = _waypoints[segmentIndex + 1];
        return new WorldPosition(Math.Sign(end.X - start.X), Math.Sign(end.Y - start.Y));
    }

    private static int CalculateTotalLength(IReadOnlyList<WorldPosition> waypoints)
    {
        var length = 0;
        for (var index = 0; index < waypoints.Count - 1; index++)
        {
            length += GetSegmentLength(waypoints[index], waypoints[index + 1]);
        }

        return length;
    }

    private static int GetSegmentLength(WorldPosition start, WorldPosition end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;

        if (dx != 0 && dy != 0)
        {
            throw new ArgumentException("Only axis-aligned path segments are supported.");
        }

        return Math.Abs(dx) + Math.Abs(dy);
    }

    private static WorldPosition GetPositionOnSegment(WorldPosition start, WorldPosition end, int distanceFromStart)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;

        if (dx != 0)
        {
            return new WorldPosition(start.X + (Math.Sign(dx) * distanceFromStart), start.Y);
        }

        return new WorldPosition(start.X, start.Y + (Math.Sign(dy) * distanceFromStart));
    }
}

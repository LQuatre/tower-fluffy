using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using TowerFluffy.Domain.Combat;
using TowerFluffy.Domain.Environment;
using TowerFluffy.Domain.Match;
using TowerFluffy.Domain.Shared;

namespace TowerFluffy.UI.Desktop.Controls;

public sealed class GameBoardControl : Control
{
    private const int LineTtlTicks = 3;
    private const int PulseTtlTicks = 6;

    public static readonly StyledProperty<MatchState?> SnapshotProperty =
        AvaloniaProperty.Register<GameBoardControl, MatchState?>(nameof(Snapshot));

    public static readonly StyledProperty<ICommand?> PlaceTowerCommandProperty =
        AvaloniaProperty.Register<GameBoardControl, ICommand?>(nameof(PlaceTowerCommand));

    public static readonly StyledProperty<ICommand?> SellTowerCommandProperty =
        AvaloniaProperty.Register<GameBoardControl, ICommand?>(nameof(SellTowerCommand));

    public static readonly StyledProperty<TowerType> SelectedTowerTypeProperty =
        AvaloniaProperty.Register<GameBoardControl, TowerType>(nameof(SelectedTowerType), defaultValue: TowerType.BasicShooter);

    public static readonly StyledProperty<bool> IsPlacementModeProperty =
        AvaloniaProperty.Register<GameBoardControl, bool>(nameof(IsPlacementMode), defaultValue: false);

    public MatchState? Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public ICommand? PlaceTowerCommand
    {
        get => GetValue(PlaceTowerCommandProperty);
        set => SetValue(PlaceTowerCommandProperty, value);
    }

    public ICommand? SellTowerCommand
    {
        get => GetValue(SellTowerCommandProperty);
        set => SetValue(SellTowerCommandProperty, value);
    }

    public TowerType SelectedTowerType
    {
        get => GetValue(SelectedTowerTypeProperty);
        set => SetValue(SelectedTowerTypeProperty, value);
    }

    public bool IsPlacementMode
    {
        get => GetValue(IsPlacementModeProperty);
        set => SetValue(IsPlacementModeProperty, value);
    }

    private readonly List<LineEffect> _lineEffects = new();
    private readonly List<PulseEffect> _pulseEffects = new();
    private GridPosition? _hoveredCell;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SnapshotProperty)
        {
            if (change.NewValue is MatchState snapshot)
            {
                UpdateCombatEffects(snapshot);
            }

            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var snapshot = Snapshot;
        if (snapshot is null)
        {
            return new Size(640, 400);
        }

        return new Size(snapshot.Map.Grid.Width * snapshot.Map.Grid.CellSize, snapshot.Map.Grid.Height * snapshot.Map.Grid.CellSize);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var snapshot = Snapshot;
        if (snapshot is null)
        {
            return;
        }

        var cellSize = snapshot.Map.Grid.CellSize;
        var boardWidth = snapshot.Map.Grid.Width * cellSize;
        var boardHeight = snapshot.Map.Grid.Height * cellSize;

        var boardRect = new Rect(0, 0, boardWidth, boardHeight);

        // Futuristic Colors
        var defenderColor = Color.Parse("#00F2FF");
        var attackerColor = Color.Parse("#FF00E5");
        var gridColor = Color.Parse("#48FFFFFF");
        var pathColor = Color.Parse("#15FFFFFF");

        var defenderBrush = new SolidColorBrush(defenderColor);
        var attackerBrush = new SolidColorBrush(attackerColor);
        var gridPen = new Pen(new SolidColorBrush(gridColor), thickness: 0.8);

        RenderBoard(context, boardRect, Brushes.Transparent);
        RenderGrid(context, snapshot, gridPen);
        RenderPath(context, snapshot, pathColor, cellSize);
        RenderTowers(context, snapshot, defenderBrush, cellSize);
        RenderUnits(context, snapshot, attackerBrush, cellSize);
        RenderCombatEffects(context, snapshot, defenderBrush, attackerBrush, cellSize);
        RenderHoverAndRange(context, snapshot, cellSize);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var snapshot = Snapshot;
        if (snapshot is null) return;

        var point = e.GetPosition(this);
        var cellSize = snapshot.Map.Grid.CellSize;
        if (cellSize <= 0) return;

        var cellX = (int)(point.X / cellSize);
        var cellY = (int)(point.Y / cellSize);
        var cell = new GridPosition(cellX, cellY);

        var hasTower = snapshot.Simulation.Towers.Any(t => t.Position.X == cellX && t.Position.Y == cellY);

        if (e.ClickCount == 2)
        {
            var sellCommand = SellTowerCommand;
            if (hasTower && sellCommand != null && sellCommand.CanExecute(cell))
            {
                sellCommand.Execute(cell);
                return;
            }
        }

        var command = PlaceTowerCommand;
        if (hasTower && command != null && command.CanExecute(cell))
        {
            command.Execute(cell);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        var snapshot = Snapshot;
        if (snapshot is null) return;

        var point = e.GetPosition(this);
        var cellSize = snapshot.Map.Grid.CellSize;
        if (cellSize <= 0) return;

        var cellX = (int)(point.X / cellSize);
        var cellY = (int)(point.Y / cellSize);
        var cell = new GridPosition(cellX, cellY);

        var command = PlaceTowerCommand;
        if (command != null && command.CanExecute(cell))
        {
            command.Execute(cell);
            InvalidateVisual();
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var snapshot = Snapshot;
        if (snapshot is null) return;

        var point = e.GetPosition(this);
        var cellSize = snapshot.Map.Grid.CellSize;
        if (cellSize <= 0) return;

        var cellX = (int)(point.X / cellSize);
        var cellY = (int)(point.Y / cellSize);

        // Clamp inside map grid bounds
        cellX = Math.Clamp(cellX, 0, snapshot.Map.Grid.Width - 1);
        cellY = Math.Clamp(cellY, 0, snapshot.Map.Grid.Height - 1);

        var newCell = new GridPosition(cellX, cellY);
        if (_hoveredCell != newCell)
        {
            _hoveredCell = newCell;
            InvalidateVisual();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_hoveredCell != null)
        {
            _hoveredCell = null;
            InvalidateVisual();
        }
    }

    private static void RenderBoard(DrawingContext context, Rect boardRect, IBrush background)
    {
        context.DrawRectangle(background, pen: null, boardRect);
    }

    private static void RenderGrid(DrawingContext context, MatchState snapshot, Pen gridPen)
    {
        var cellSize = snapshot.Map.Grid.CellSize;

        var boardWidth = snapshot.Map.Grid.Width * cellSize;
        var boardHeight = snapshot.Map.Grid.Height * cellSize;

        for (var x = 0; x <= snapshot.Map.Grid.Width; x++)
        {
            var xPixel = x * cellSize;
            context.DrawLine(gridPen, new Point(xPixel, 0), new Point(xPixel, boardHeight));
        }

        for (var y = 0; y <= snapshot.Map.Grid.Height; y++)
        {
            var yPixel = y * cellSize;
            context.DrawLine(gridPen, new Point(0, yPixel), new Point(boardWidth, yPixel));
        }
    }

    private static void RenderPath(DrawingContext context, MatchState snapshot, Color pathColor, int cellSize)
    {
        if (snapshot.Map.Path.Waypoints.Count < 2)
        {
            return;
        }

        var pathBrush = new SolidColorBrush(pathColor);
        var pathPen = new Pen(pathBrush, thickness: cellSize * 0.8)
        {
            LineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };

        var centerPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), thickness: 2)
        {
            DashStyle = new DashStyle(new[] { 4.0, 6.0 }, offset: 0),
        };

        for (var i = 0; i < snapshot.Map.Path.Waypoints.Count - 1; i++)
        {
            var p1 = new Point(snapshot.Map.Path.Waypoints[i].X, snapshot.Map.Path.Waypoints[i].Y);
            var p2 = new Point(snapshot.Map.Path.Waypoints[i + 1].X, snapshot.Map.Path.Waypoints[i + 1].Y);

            context.DrawLine(pathPen, p1, p2);
            context.DrawLine(centerPen, p1, p2);
        }

        var start = new Point(snapshot.Map.Path.Waypoints[0].X, snapshot.Map.Path.Waypoints[0].Y);
        var end = new Point(snapshot.Map.Path.Waypoints[^1].X, snapshot.Map.Path.Waypoints[^1].Y);

        DrawEndpointMarker(context, start, new SolidColorBrush(Color.Parse("#FF2E2E")), size: cellSize * 0.2);
        DrawEndpointMarker(context, end, new SolidColorBrush(Color.Parse("#00FF94")), size: cellSize * 0.3);
    }

    private static void DrawEndpointMarker(DrawingContext context, Point center, IBrush stroke, double size)
    {
        var pen = new Pen(CreateTintBrush(stroke, alpha: 160), thickness: 2);

        context.DrawEllipse(Brushes.Transparent, pen, center, size, size);
        context.DrawLine(pen, new Point(center.X - size, center.Y), new Point(center.X + size, center.Y));
        context.DrawLine(pen, new Point(center.X, center.Y - size), new Point(center.X, center.Y + size));
    }

    private static void RenderTowers(
        DrawingContext context,
        MatchState snapshot,
        ISolidColorBrush brush,
        int cellSize)
    {
        foreach (var tower in snapshot.Simulation.Towers)
        {
            var towerBrush = tower.Type switch
            {
                TowerType.BasicShooter => brush,
                TowerType.Flamethrower => new SolidColorBrush(Color.Parse("#FF4500")),
                TowerType.Sniper => new SolidColorBrush(Color.Parse("#FFD700")),
                TowerType.Cannon => new SolidColorBrush(Color.Parse("#8A2BE2")),
                TowerType.Laser => new SolidColorBrush(Color.Parse("#00FF7F")),
                _ => brush
            };
            var towerGlowBrush = new SolidColorBrush(towerBrush.Color, opacity: 0.2);
            var towerStrongPen = new Pen(towerBrush, thickness: 2);

            var cellRect = new Rect(tower.Position.X * cellSize, tower.Position.Y * cellSize, cellSize, cellSize);
            var inset = cellSize * 0.2;
            var outer = cellRect.Deflate(inset);

            context.DrawEllipse(towerGlowBrush, null, outer.Center, outer.Width * 0.8, outer.Height * 0.8);

            var geometry = CreateCutCornerRectGeometry(outer, cut: cellSize * 0.15);
            context.DrawGeometry(new SolidColorBrush(towerBrush.Color, 0.1), towerStrongPen, geometry);

            var coreRadius = cellSize * 0.1;
            context.DrawEllipse(towerBrush, null, outer.Center, coreRadius, coreRadius);

            DrawHealthBar(context, tower.Health.Value, GetMaxHealth(tower.Type), new Point(outer.Left, outer.Top - 8), towerBrush, outer.Width);
        }
    }

    private static void RenderUnits(
        DrawingContext context,
        MatchState snapshot,
        ISolidColorBrush brush,
        int cellSize)
    {
        foreach (var unit in snapshot.Simulation.Units)
        {
            var unitBrush = unit.Type switch
            {
                UnitType.Soldat => brush,
                UnitType.Brute => new SolidColorBrush(Color.Parse("#FF8C00")),
                UnitType.Rapide => new SolidColorBrush(Color.Parse("#FF1493")),
                UnitType.TireurElite => new SolidColorBrush(Color.Parse("#4169E1")),
                UnitType.Tank => new SolidColorBrush(Color.Parse("#8B0000")),
                _ => brush
            };
            
            var strongPen = new Pen(unitBrush, thickness: 2);
            var trailBrush = new SolidColorBrush(unitBrush.Color, opacity: 0.3);
            var trailPen = new Pen(trailBrush, thickness: 2) { LineCap = PenLineCap.Round };

            // Récupérer la position et la direction calculées par le chemin du Domain
            var unitPos = snapshot.Map.Path.GetPositionAtDistance(unit.DistanceAlongPath);
            var unitDir = snapshot.Map.Path.GetDirectionAtDistance(unit.DistanceAlongPath);

            var center = new Point(unitPos.X, unitPos.Y);
            var direction = new Vector(unitDir.X, unitDir.Y);
            var radius = cellSize * 0.15;

            DrawTrail(context, center, direction, trailPen, length: radius * 2.5);

            var unitGeometry = CreateArrowGeometry(center, radius, direction);
            context.DrawGeometry(new SolidColorBrush(unitBrush.Color, 0.2), strongPen, unitGeometry);

            DrawHealthBar(context, unit.Health.Value, GetMaxHealth(unit.Type), new Point(center.X - radius, center.Y - radius - 8), unitBrush, radius * 2);
        }
    }

    private static void DrawTrail(DrawingContext context, Point center, Vector direction, Pen pen, double length)
    {
        var lengthSquared = (direction.X * direction.X) + (direction.Y * direction.Y);
        if (lengthSquared < 0.0001)
        {
            return;
        }

        var back = new Point(center.X - (direction.X * length), center.Y - (direction.Y * length));
        context.DrawLine(pen, back, center);
    }

    private static Geometry CreateCutCornerRectGeometry(Rect rect, double cut)
    {
        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(new Point(rect.Left + cut, rect.Top), isFilled: true);
            geometryContext.LineTo(new Point(rect.Right - cut, rect.Top));
            geometryContext.LineTo(new Point(rect.Right, rect.Top + cut));
            geometryContext.LineTo(new Point(rect.Right, rect.Bottom - cut));
            geometryContext.LineTo(new Point(rect.Right - cut, rect.Bottom));
            geometryContext.LineTo(new Point(rect.Left + cut, rect.Bottom));
            geometryContext.LineTo(new Point(rect.Left, rect.Bottom - cut));
            geometryContext.LineTo(new Point(rect.Left, rect.Top + cut));
            geometryContext.EndFigure(isClosed: true);
        }

        return geometry;
    }

    private static Geometry CreateArrowGeometry(Point center, double radius, Vector direction)
    {
        var lengthSquared = (direction.X * direction.X) + (direction.Y * direction.Y);
        if (lengthSquared < 0.0001)
        {
            direction = new Vector(1, 0);
        }

        var perp = new Vector(-direction.Y, direction.X);
        var tip = center + (direction * radius);
        var baseCenter = center - (direction * (radius * 0.6));
        var left = baseCenter + (perp * (radius * 0.65));
        var right = baseCenter - (perp * (radius * 0.65));

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(tip, isFilled: true);
            geometryContext.LineTo(left);
            geometryContext.LineTo(right);
            geometryContext.EndFigure(isClosed: true);
        }

        return geometry;
    }

    private static int GetMaxHealth(TowerType type) => type switch
    {
        TowerType.BasicShooter => 100,
        TowerType.Flamethrower => 120,
        TowerType.Sniper => 80,
        TowerType.Cannon => 150,
        TowerType.Laser => 150,
        _ => 100
    };

    private static int GetMaxHealth(UnitType type) => type switch
    {
        UnitType.Soldat => 20,
        UnitType.Brute => 80,
        UnitType.Rapide => 15,
        UnitType.TireurElite => 30,
        UnitType.Tank => 300,
        _ => 20
    };

    private static void DrawHealthBar(DrawingContext context, int health, int maxHealth, Point anchor, IBrush stroke, double width)
    {
        if (health <= 0) return;

        double fraction = Math.Clamp((double)health / maxHealth, 0.0, 1.0);
        var height = 4.0;
        
        var bgRect = new Rect(anchor.X, anchor.Y, width, height);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#80000000")), null, bgRect);
        
        var fgRect = new Rect(anchor.X, anchor.Y, width * fraction, height);
        var color = fraction > 0.5 ? Color.Parse("#32CD32") : (fraction > 0.2 ? Color.Parse("#FFA500") : Color.Parse("#FF0000"));
        context.DrawRectangle(new SolidColorBrush(color), null, fgRect);

        context.DrawRectangle(null, new Pen(stroke, 0.5), bgRect);
    }

    private static IBrush CreateTintBrush(IBrush source, byte alpha)
    {
        if (source is ISolidColorBrush solid)
        {
            var color = solid.Color;
            return new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        }

        return Brushes.Transparent;
    }

    private void UpdateCombatEffects(MatchState snapshot)
    {
        var currentTick = snapshot.Simulation.Tick.Value;
        if (currentTick < 0)
        {
            _lineEffects.Clear();
            _pulseEffects.Clear();
            return;
        }

        _lineEffects.RemoveAll(e => (currentTick - e.SpawnTick) >= e.Ttl);
        _pulseEffects.RemoveAll(e => (currentTick - e.SpawnTick) >= PulseTtlTicks);

        foreach (var e in snapshot.LastCombatEvents)
        {
            var from = new Point(e.From.X, e.From.Y);
            var to = new Point(e.To.X, e.To.Y);
            var isKill = e.TargetDestroyed;
            var sourceType = e.SourceTowerType;
            var ttl = sourceType == TowerType.Flamethrower ? 8 : LineTtlTicks;
            var isAttacker = e.Kind == CombatEventKind.UnitAttackTower || e.Kind == CombatEventKind.UnitHitBase;
            _lineEffects.Add(new LineEffect(e.Tick.Value, from, to, isKill, sourceType, ttl, isAttacker));
            _pulseEffects.Add(new PulseEffect(e.Tick.Value, to, isKill));
        }
    }

    private void RenderCombatEffects(
        DrawingContext context,
        MatchState snapshot,
        IBrush defenderBrush,
        IBrush attackerBrush,
        int cellSize)
    {
        var currentTick = snapshot.Simulation.Tick.Value;
        if (currentTick < 0)
        {
            return;
        }

        var baseLineThickness = Math.Max(2.0, cellSize * 0.08);
        var basePulseRadius = Math.Max(6.0, cellSize * 0.15);

        foreach (var effect in _lineEffects)
        {
            var age = currentTick - effect.SpawnTick;
            if (age < 0) continue;

            var progress = GetProgress(age, effect.Ttl);
            var alpha = GetFadedAlpha(maxAlpha: effect.IsKill ? 255 : 180, progress);
            var thickness = effect.IsKill ? baseLineThickness * 1.5 : baseLineThickness;

            var brush = (effect.IsKill || effect.IsAttacker) ? attackerBrush : defenderBrush;

            if (effect.SourceType == TowerType.Flamethrower)
            {
                var fireColors = new[] { "#FFFF00", "#FFD700", "#FF8C00", "#FF4500" };
                var random = new Random(effect.SpawnTick + (int)effect.From.X + (int)effect.From.Y);
                
                var dir = new Vector(effect.To.X - effect.From.X, effect.To.Y - effect.From.Y);
                var length = dir.Length;
                if (length > 0.01)
                {
                    dir /= length;
                    var perp = new Vector(-dir.Y, dir.X);
                    
                    var coneWidth = length * 0.4;
                    var p1 = effect.From;
                    var p2 = effect.To + perp * coneWidth;
                    var p3 = effect.To - perp * coneWidth;
                    
                    var coneGeometry = new StreamGeometry();
                    using (var geoCtx = coneGeometry.Open())
                    {
                        geoCtx.BeginFigure(p1, isFilled: true);
                        geoCtx.LineTo(p2);
                        geoCtx.LineTo(p3);
                        geoCtx.EndFigure(isClosed: true);
                    }
                    
                    context.DrawGeometry(new SolidColorBrush(Color.Parse("#FF4500"), (byte)(alpha * 0.4)), null, coneGeometry);
                    
                    for (int i = 0; i < 12; i++)
                    {
                        var dist = random.NextDouble() * length;
                        var spread = (dist / length) * coneWidth;
                        var offset = perp * (random.NextDouble() * spread * 2 - spread);
                        var pos = effect.From + dir * dist + offset;
                        
                        var pSize = (random.NextDouble() * 6 + 3.0) * (1.0 - progress);
                        var pColor = Color.Parse(fireColors[random.Next(fireColors.Length)]);
                        var pBrush = new SolidColorBrush(pColor, (byte)(alpha * 0.8));
                        
                        context.DrawEllipse(pBrush, null, pos, pSize, pSize);
                    }
                }
            }
            else
            {
                // Glowing laser drawing style: outer glow + bright core
                var glowAlpha = (byte)(alpha * 0.35);
                var glowPen = new Pen(CreateTintBrush(brush, glowAlpha), thickness * 3.0) { LineCap = PenLineCap.Round };
                context.DrawLine(glowPen, effect.From, effect.To);

                var corePen = new Pen(Brushes.White, thickness * 0.8) { LineCap = PenLineCap.Round };
                context.DrawLine(corePen, effect.From, effect.To);
            }
        }

        foreach (var effect in _pulseEffects)
        {
            var age = currentTick - effect.SpawnTick;
            if (age < 0) continue;

            var progress = GetProgress(age, PulseTtlTicks);
            var alpha = GetFadedAlpha(maxAlpha: 180, progress);

            var radius = basePulseRadius + (progress * basePulseRadius * 1.5);
            var pen = new Pen(CreateTintBrush(attackerBrush, alpha), 2);
            var fill = CreateTintBrush(attackerBrush, (byte)(alpha / 3));

            context.DrawEllipse(fill, pen, effect.Center, radius, radius);
        }
    }

    private static double GetProgress(int age, int ttl)
    {
        if (ttl <= 1)
        {
            return 1.0;
        }

        var clamped = Math.Clamp(age, 0, ttl - 1);
        return clamped / (double)(ttl - 1);
    }

    private static byte GetFadedAlpha(int maxAlpha, double progress)
    {
        var a = (int)Math.Round(maxAlpha * (1.0 - progress));
        return (byte)Math.Clamp(a, 0, 255);
    }

    private readonly record struct LineEffect(int SpawnTick, Point From, Point To, bool IsKill, TowerType? SourceType, int Ttl, bool IsAttacker);

    private readonly record struct PulseEffect(int SpawnTick, Point Center, bool IsKill);

    private void RenderHoverAndRange(DrawingContext context, MatchState snapshot, int cellSize)
    {
        if (_hoveredCell == null) return;

        var cell = _hoveredCell.Value;
        var existingTower = snapshot.Simulation.Towers.FirstOrDefault(t => t.Position.X == cell.X && t.Position.Y == cell.Y);

        double range = 0;
        Color color = Color.Parse("#00F2FF");

        if (existingTower != null)
        {
            range = existingTower.Stats.Range;
            color = GetTowerColor(existingTower.Type);
        }
        else if (IsPlacementMode)
        {
            range = GetTowerRange(SelectedTowerType);
            color = GetTowerColor(SelectedTowerType);
        }

        // Draw hover square
        var hoverRect = new Rect(cell.X * cellSize, cell.Y * cellSize, cellSize, cellSize);
        var hoverBrush = new SolidColorBrush(color, 0.12);
        var hoverPen = new Pen(new SolidColorBrush(color, 0.6), 1.5);
        context.DrawRectangle(hoverBrush, hoverPen, hoverRect);

        // Draw range circle if applicable
        if (range > 0)
        {
            var center = new Point(cell.X * cellSize + cellSize / 2.0, cell.Y * cellSize + cellSize / 2.0);
            var rangeBrush = new SolidColorBrush(color, 0.06);
            var rangePen = new Pen(new SolidColorBrush(color, 0.35), 1.5)
            {
                DashStyle = new DashStyle(new[] { 6.0, 4.0 }, offset: 0)
            };
            context.DrawEllipse(rangeBrush, rangePen, center, range, range);
        }
    }

    private static int GetTowerRange(TowerType type) => type switch
    {
        TowerType.BasicShooter => 250,
        TowerType.Flamethrower => 180,
        TowerType.Sniper => 400,
        TowerType.Cannon => 220,
        TowerType.Laser => 300,
        _ => 250
    };

    private static Color GetTowerColor(TowerType type) => type switch
    {
        TowerType.BasicShooter => Color.Parse("#00F2FF"), // Cyan
        TowerType.Flamethrower => Color.Parse("#FF4500"), // Orange-red
        TowerType.Sniper => Color.Parse("#FFD700"),       // Gold
        TowerType.Cannon => Color.Parse("#8A2BE2"),       // Purple
        TowerType.Laser => Color.Parse("#00FF7F"),        // SpringGreen
        _ => Color.Parse("#00F2FF")
    };

    public GridPosition? HoveredCell => _hoveredCell;
}

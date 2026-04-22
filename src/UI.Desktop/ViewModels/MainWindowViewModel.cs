using TowerFluffy.Application.Game;
using TowerFluffy.Application.Game.Dtos;
using ReactiveUI;
using System.Reactive;

namespace TowerFluffy.UI.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly GameSession _session;
    private GameSnapshotDto _snapshot;
    private string? _lastError;
    private GameSpeedSetting _speedSetting;
    private bool _halfSpeedToggle;

    public MainWindowViewModel()
        : this(GameSession.CreateMvp())
    {
    }

    public MainWindowViewModel(GameSession session)
    {
        _session = session;
        _snapshot = _session.Snapshot;
        _speedSetting = GameSpeedSetting.Normal;

        SkipPreparationCommand = ReactiveCommand.Create(ExecuteSkipPreparation);
        SendGruntCommand = ReactiveCommand.Create(ExecuteSendGrunt);
        ResetCommand = ReactiveCommand.Create(ExecuteReset);
        PlaceTowerCommand = ReactiveCommand.Create<GridPositionDto>(ExecutePlaceTower);
        SetSpeedHalfCommand = ReactiveCommand.Create(() => SetSpeed(GameSpeedSetting.Half));
        SetSpeedNormalCommand = ReactiveCommand.Create(() => SetSpeed(GameSpeedSetting.Normal));
        SetSpeedDoubleCommand = ReactiveCommand.Create(() => SetSpeed(GameSpeedSetting.Double));
    }

    public GameSnapshotDto Snapshot
    {
        get => _snapshot;
        private set => this.RaiseAndSetIfChanged(ref _snapshot, value);
    }

    public string? LastError
    {
        get => _lastError;
        private set => this.RaiseAndSetIfChanged(ref _lastError, value);
    }

    public GameSpeedSetting SpeedSetting
    {
        get => _speedSetting;
        private set => this.RaiseAndSetIfChanged(ref _speedSetting, value);
    }

    public ReactiveCommand<Unit, Unit> SkipPreparationCommand { get; }
    public ReactiveCommand<Unit, Unit> SendGruntCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public ReactiveCommand<GridPositionDto, Unit> PlaceTowerCommand { get; }
    public ReactiveCommand<Unit, Unit> SetSpeedHalfCommand { get; }
    public ReactiveCommand<Unit, Unit> SetSpeedNormalCommand { get; }
    public ReactiveCommand<Unit, Unit> SetSpeedDoubleCommand { get; }

    public void Tick()
    {
        var ticks = GetTicksToSimulate();
        if (ticks <= 0)
        {
            return;
        }

        _session.Tick(ticks);
        Snapshot = _session.Snapshot;
    }

    private int GetTicksToSimulate()
    {
        if (SpeedSetting == GameSpeedSetting.Half)
        {
            _halfSpeedToggle = !_halfSpeedToggle;
            return _halfSpeedToggle ? 1 : 0;
        }

        if (SpeedSetting == GameSpeedSetting.Double)
        {
            return 2;
        }

        return 1;
    }

    private void ExecuteSkipPreparation()
    {
        Apply(_session.SkipPreparation());
    }

    private void ExecuteSendGrunt()
    {
        Apply(_session.SendUnit(UnitTypeDto.Grunt));
    }

    private void ExecuteReset()
    {
        _session.Reset();
        LastError = null;
        Snapshot = _session.Snapshot;
    }

    private void ExecutePlaceTower(GridPositionDto position)
    {
        Apply(_session.PlaceTower(TowerTypeDto.BasicShooter, position));
    }

    private void SetSpeed(GameSpeedSetting speed)
    {
        SpeedSetting = speed;
        _halfSpeedToggle = false;
    }

    private void Apply(CommandResult result)
    {
        if (!result.IsSuccess)
        {
            LastError = result.ErrorMessage;
            return;
        }

        LastError = null;
        Snapshot = _session.Snapshot;
    }
}

public enum GameSpeedSetting
{
    Half = 0,
    Normal = 1,
    Double = 2,
}

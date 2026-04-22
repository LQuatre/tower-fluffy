using TowerFluffy.Application.Game;
using TowerFluffy.Application.Game.Dtos;
using TowerFluffy.Application.Common.Networking;
using TowerFluffy.Infrastructure.Networking;
using ReactiveUI;
using System;
using System.Globalization;
using System.Reactive;
using System.Threading.Tasks;
using System.Linq;

namespace TowerFluffy.UI.Desktop.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly GameSession _session;
    private GameSnapshotDto _snapshot;
    private string? _lastError;
    private GameSpeedSetting _speedSetting;
    private bool _halfSpeedToggle;
    private SignalRGameClient? _networkClient;
    private bool _isConnected;
    private bool _isConnecting;
    private bool _isReady;
    private bool _isGameStarted;
    private bool _isOpponentReady;
    private string _serverUrl = "http://localhost:5128/gameHub";
    private PlayerRole _selectedRole = PlayerRole.Both;

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
        ConnectCommand = ReactiveCommand.CreateFromTask(ExecuteConnect);
        StartSoloCommand = ReactiveCommand.Create(ExecuteStartSolo);
    }

    public string ServerUrl
    {
        get => _serverUrl;
        set => this.RaiseAndSetIfChanged(ref _serverUrl, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set 
        {
            this.RaiseAndSetIfChanged(ref _isConnected, value);
            this.RaisePropertyChanged(nameof(IsLobbyVisible));
            this.RaisePropertyChanged(nameof(IsConnectionVisible));
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        private set => this.RaiseAndSetIfChanged(ref _isConnecting, value);
    }

    public PlayerRole SelectedRole
    {
        get => _selectedRole;
        set 
        {
            this.RaiseAndSetIfChanged(ref _selectedRole, value);
            this.RaisePropertyChanged(nameof(CanPlaceTower));
            this.RaisePropertyChanged(nameof(CanSendUnits));
            this.RaisePropertyChanged(nameof(IsSoloMode));
        }
    }

    public bool CanPlaceTower => SelectedRole is PlayerRole.Both or PlayerRole.Defender;
    public bool CanSendUnits => SelectedRole is PlayerRole.Both or PlayerRole.Attacker;

    public bool IsReady
    {
        get => _isReady;
        set 
        {
            this.RaiseAndSetIfChanged(ref _isReady, value);
            _networkClient?.SetReady(value);
        }
    }

    public bool IsGameStarted
    {
        get => _isGameStarted;
        private set 
        {
            this.RaiseAndSetIfChanged(ref _isGameStarted, value);
            this.RaisePropertyChanged(nameof(IsLobbyVisible));
            this.RaisePropertyChanged(nameof(IsConnectionVisible));
        }
    }

    public bool IsOpponentReady
    {
        get => _isOpponentReady;
        private set => this.RaiseAndSetIfChanged(ref _isOpponentReady, value);
    }

    public bool IsLobbyVisible => IsConnected && !IsGameStarted;
    public bool IsConnectionVisible => !IsConnected && !IsGameStarted;
    public bool IsSoloMode => SelectedRole == PlayerRole.Both;

    public GameSnapshotDto Snapshot
    {
        get => _snapshot;
        private set
        {
            this.RaiseAndSetIfChanged(ref _snapshot, value);
            this.RaisePropertyChanged(nameof(PreparationTimeFormatted));
            this.RaisePropertyChanged(nameof(WaveSendTimeFormatted));
            this.RaisePropertyChanged(nameof(IsPreparationTimerVisible));
            this.RaisePropertyChanged(nameof(PhaseFormatted));
            this.RaisePropertyChanged(nameof(ActivePhaseLabel));
            this.RaisePropertyChanged(nameof(ActivePhaseTime));
            this.RaisePropertyChanged(nameof(IsDefenderPhase));
        }
    }

    public string PreparationTimeFormatted => $"{Snapshot.Hud.PreparationTicksRemaining / 60.0:F1} sec";
    public string WaveSendTimeFormatted => $"{Snapshot.Hud.WaveSendTicksRemaining / 60.0:F1} sec";
    public bool IsPreparationTimerVisible => Snapshot.Hud.PreparationTicksRemaining > 0;

    public string PhaseFormatted => Snapshot.Hud.Phase switch
    {
        MatchPhaseDto.Preparation => "Préparation",
        MatchPhaseDto.Wave => "Vague",
        MatchPhaseDto.Finished => "Terminé",
        _ => Snapshot.Hud.Phase.ToString()
    };

    public string ActivePhaseLabel => Snapshot.Hud.Phase == MatchPhaseDto.Preparation ? "PHASE DE DÉFENSE" : "PHASE D'ATTAQUE";
    public string ActivePhaseTime => Snapshot.Hud.Phase == MatchPhaseDto.Preparation ? PreparationTimeFormatted : WaveSendTimeFormatted;
    public bool IsDefenderPhase => Snapshot.Hud.Phase == MatchPhaseDto.Preparation;

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
    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
    public ReactiveCommand<Unit, Unit> StartSoloCommand { get; }

    public void Tick()
    {
        if (!IsGameStarted && SelectedRole != PlayerRole.Both)
        {
            return;
        }

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
        if (!CanSendUnits) return;
        Apply(_session.SendUnit(UnitTypeDto.Grunt));
        BroadcastAction(PlayerActionKind.SendWave, unitType: (int)UnitTypeDto.Grunt);
    }

    private void ExecuteReset()
    {
        _session.Reset();
        LastError = null;
        Snapshot = _session.Snapshot;
    }

    private void ExecutePlaceTower(GridPositionDto position)
    {
        if (!CanPlaceTower) return;
        Apply(_session.PlaceTower(TowerTypeDto.BasicShooter, position));
        BroadcastAction(PlayerActionKind.PlaceTower, towerType: (int)TowerTypeDto.BasicShooter, x: position.X, y: position.Y);
    }

    private async Task ExecuteConnect()
    {
        IsConnecting = true;
        LastError = null;
        try
        {
            await Task.Delay(500);

            _networkClient = new SignalRGameClient(ServerUrl.Trim());
            _networkClient.OnPlayerActionReceived += HandleNetworkAction;
            _networkClient.OnGameStarted += () => Avalonia.Threading.Dispatcher.UIThread.Post(() => IsGameStarted = true);
            _networkClient.OnOpponentReady += (ready) => Avalonia.Threading.Dispatcher.UIThread.Post(() => IsOpponentReady = ready);
            
            await _networkClient.StartAsync();
            await _networkClient.JoinGame("default-game");
            
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                IsConnected = true;
                LastError = null;
            });
        }
        catch (System.Exception ex)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                LastError = $"ERREUR RÉSEAU : {ex.Message}";
            });
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private void ExecuteStartSolo()
    {
        IsGameStarted = true;
    }

    private void HandleNetworkAction(PlayerAction action)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            switch (action.Kind)
            {
                case PlayerActionKind.PlaceTower:
                    if (action.TowerType.HasValue && action.X.HasValue && action.Y.HasValue)
                    {
                        Apply(_session.PlaceTower((TowerTypeDto)action.TowerType.Value, new GridPositionDto(action.X.Value, action.Y.Value)));
                    }
                    break;
                case PlayerActionKind.SendWave:
                    if (action.UnitType.HasValue)
                    {
                        Apply(_session.SendUnit((UnitTypeDto)action.UnitType.Value));
                    }
                    break;
            }
        });
    }

    private void SetSpeed(GameSpeedSetting speed)
    {
        SpeedSetting = speed;
        _halfSpeedToggle = false;
    }

    private void BroadcastAction(PlayerActionKind kind, int? towerType = null, int? x = null, int? y = null, int? unitType = null)
    {
        if (_networkClient != null && IsConnected)
        {
            _ = _networkClient.SendPlayerAction(new PlayerAction(1, kind, towerType, x, y, unitType));
        }
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

public enum PlayerRole
{
    Both,
    Attacker,
    Defender
}

public enum GameSpeedSetting
{
    Half = 0,
    Normal = 1,
    Double = 2,
}

public class ReadyToColorConverter : Avalonia.Data.Converters.IValueConverter
{
    public static readonly ReadyToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool ready && ready)
        {
            return Avalonia.Media.Brush.Parse("#00F2FF"); // DefenderColor
        }
        return Avalonia.Media.Brush.Parse("#40FFFFFF"); // Muted/Empty
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

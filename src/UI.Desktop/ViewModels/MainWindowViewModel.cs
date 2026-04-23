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
    private GameSession _session;
    private GameSnapshotDto _snapshot;
    private string? _lastError;
    private SignalRGameClient? _networkClient;
    private DateTime? _gameStartTime;
    private long _totalTicksProcessed;
    private bool _isConnected;
    private bool _isConnecting;
    private bool _isReady;
    private bool _isGameStarted;
    private bool _isOpponentReady;
    private bool _isInGameRoom;
    private string _serverUrl = "http://vps.lquatre.fr:5128/gameHub";
    private System.Collections.ObjectModel.ObservableCollection<GameInfoDto> _availableGames = new();
    private PlayerRole _selectedRole = PlayerRole.Both;
    private GridPositionDto? _movingTowerFrom;
    private TowerTypeDto _currentTowerType = TowerTypeDto.BasicShooter;

    public MainWindowViewModel()
        : this(GameSession.CreateMvp())
    {
    }

    public MainWindowViewModel(GameSession session)
    {
        _session = session;
        _snapshot = _session.Snapshot;

        SkipPreparationCommand = ReactiveCommand.Create(ExecuteSkipPreparation);
        SendSoldatCommand = ReactiveCommand.Create(ExecuteSendSoldat);
        SendBruteCommand = ReactiveCommand.Create(ExecuteSendBrute);
        PlaceTowerCommand = ReactiveCommand.Create<GridPositionDto>(ExecutePlaceTower);
        SetBasicTowerCommand = ReactiveCommand.Create(() => { CurrentTowerType = TowerTypeDto.BasicShooter; });
        SetFlamethrowerCommand = ReactiveCommand.Create(() => { CurrentTowerType = TowerTypeDto.Flamethrower; });
        ConnectCommand = ReactiveCommand.CreateFromTask(ExecuteConnect);
        RefreshGamesCommand = ReactiveCommand.CreateFromTask(ExecuteRefreshGames);
        JoinSpecificGameCommand = ReactiveCommand.CreateFromTask<string>(ExecuteJoinSpecificGame);
        StartSoloCommand = ReactiveCommand.Create(ExecuteStartSolo);
        ReplayCommand = ReactiveCommand.Create(ExecuteReplay);
    }

    public string ServerUrl
    {
        get => _serverUrl;
        set => this.RaiseAndSetIfChanged(ref _serverUrl, value);
    }

    public System.Collections.ObjectModel.ObservableCollection<GameInfoDto> AvailableGames => _availableGames;

    public bool IsConnected
    {
        get => _isConnected;
        private set 
        {
            this.RaiseAndSetIfChanged(ref _isConnected, value);
            this.RaisePropertyChanged(nameof(IsLobbyVisible));
            this.RaisePropertyChanged(nameof(IsWaitingRoomVisible));
            this.RaisePropertyChanged(nameof(IsConnectionVisible));
        }
    }

    public bool IsInGameRoom
    {
        get => _isInGameRoom;
        private set 
        {
            this.RaiseAndSetIfChanged(ref _isInGameRoom, value);
            this.RaisePropertyChanged(nameof(IsLobbyVisible));
            this.RaisePropertyChanged(nameof(IsWaitingRoomVisible));
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
            this.RaisePropertyChanged(nameof(CanSkipPreparation));
            this.RaisePropertyChanged(nameof(IsSoloMode));
        }
    }

    public bool CanPlaceTower => SelectedRole is PlayerRole.Both or PlayerRole.Defender;
    public bool CanSendUnits => SelectedRole is PlayerRole.Both or PlayerRole.Attacker;
    public bool CanSkipPreparation => SelectedRole is PlayerRole.Both or PlayerRole.Defender;

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
            
            if (value)
            {
                SoundEffects.StopTheme();
            }
        }
    }

    public bool IsOpponentReady
    {
        get => _isOpponentReady;
        private set => this.RaiseAndSetIfChanged(ref _isOpponentReady, value);
    }

    public bool IsLobbyVisible => false; // Désormais fusionné dans IsConnectionVisible
    public bool IsWaitingRoomVisible => IsConnected && IsInGameRoom && !IsGameStarted;
    public bool IsConnectionVisible => !IsInGameRoom && !IsGameStarted;
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
            this.RaisePropertyChanged(nameof(IsGameFinished));
            this.RaisePropertyChanged(nameof(GameResultMessage));
            this.RaisePropertyChanged(nameof(GameResultColor));
        }
    }

    public bool IsGameFinished => Snapshot.Hud.Phase == MatchPhaseDto.Finished;

    public string GameResultMessage => Snapshot.Hud.Outcome switch
    {
        MatchOutcomeDto.DefenderVictory => SelectedRole switch
        {
            PlayerRole.Defender => "VICTOIRE ! LA BASE EST SAUVE.",
            PlayerRole.Attacker => "DÉFAITE... L'ASSAUT A ÉCHOUÉ.",
            _ => "FIN DE MISSION : DÉFENSE VICTORIEUSE"
        },
        MatchOutcomeDto.AttackerVictory => SelectedRole switch
        {
            PlayerRole.Attacker => "VICTOIRE ! LE NOYAU EST DÉTRUIT.",
            PlayerRole.Defender => "DÉFAITE... LA BASE A SUCCOMBÉ.",
            _ => "FIN DE MISSION : ATTAQUE VICTORIEUSE"
        },
        _ => "MATCH NUL"
    };

    public string GameResultColor => Snapshot.Hud.Outcome switch
    {
        MatchOutcomeDto.DefenderVictory => SelectedRole == PlayerRole.Defender ? "#00F2FF" : "#FF2E2E",
        MatchOutcomeDto.AttackerVictory => SelectedRole == PlayerRole.Attacker ? "#FF00E5" : "#FF2E2E",
        _ => "#FFFFFF"
    };

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

    public TowerTypeDto CurrentTowerType
    {
        get => _currentTowerType;
        set {
            this.RaiseAndSetIfChanged(ref _currentTowerType, value);
            this.RaisePropertyChanged(nameof(CurrentTowerStats));
        }
    }

    public string CurrentTowerStats => CurrentTowerType switch
    {
        TowerTypeDto.BasicShooter => "Dégâts: 2 | Portée: 220 | Cadence: 0.3s | PV: 40",
        TowerTypeDto.Flamethrower => "Dégâts: 1 | Portée: 160 | Cadence: 0.2s | PV: 50",
        _ => ""
    };

    public string SoldatStats => "PV: 5 | Vitesse: 3 | Dégâts: 2 | Portée: 150";
    public string BruteStats => "PV: 40 | Vitesse: 1 | Dégâts: 10 | Portée: 180";

    public ReactiveCommand<Unit, Unit> SkipPreparationCommand { get; }
    public ReactiveCommand<Unit, Unit> SendSoldatCommand { get; }
    public ReactiveCommand<Unit, Unit> SendBruteCommand { get; }
    public ReactiveCommand<GridPositionDto, Unit> PlaceTowerCommand { get; }
    public ReactiveCommand<Unit, Unit> SetBasicTowerCommand { get; }
    public ReactiveCommand<Unit, Unit> SetFlamethrowerCommand { get; }
    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshGamesCommand { get; }
    public ReactiveCommand<string, Unit> JoinSpecificGameCommand { get; }
    public ReactiveCommand<Unit, Unit> StartSoloCommand { get; }
    public ReactiveCommand<Unit, Unit> ReplayCommand { get; }

    public void Tick()
    {
        if (!IsGameStarted || _gameStartTime == null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var elapsed = now - _gameStartTime.Value;
        
        // On vise 60 ticks par seconde
        long targetTicks = (long)(elapsed.TotalSeconds * 60);
        int ticksToProcess = (int)(targetTicks - _totalTicksProcessed);

        if (ticksToProcess <= 0) return;

        // On limite le rattrapage pour éviter de freezer si le décalage est énorme
        ticksToProcess = Math.Min(ticksToProcess, 10);
        
        _session.Tick(ticksToProcess);
        _totalTicksProcessed += ticksToProcess;
        Snapshot = _session.Snapshot;

        // Effets sonores
        foreach (var ev in Snapshot.CombatEvents)
        {
            if (ev.Kind == CombatEventKindDto.TowerShot)
            {
                if (ev.SourceTowerType == TowerTypeDto.Flamethrower)
                {
                    SoundEffects.PlayFlamme();
                }
                else
                {
                    SoundEffects.PlayPiou();
                }
            }
            else if (ev.Kind == CombatEventKindDto.UnitAttackTower || ev.Kind == CombatEventKindDto.UnitHitBase)
            {
                SoundEffects.PlayPiou();
            }
        }
    }

    private void ExecuteSkipPreparation()
    {
        Apply(_session.SkipPreparation());
        BroadcastAction(PlayerActionKind.SkipPreparation);
    }

    private void ExecuteSendSoldat()
    {
        if (!CanSendUnits) return;
        Apply(_session.SendUnit(UnitTypeDto.Soldat));
        BroadcastAction(PlayerActionKind.SendWave, unitType: (int)UnitTypeDto.Soldat);
    }

    private void ExecuteSendBrute()
    {
        if (!CanSendUnits) return;
        Apply(_session.SendUnit(UnitTypeDto.Brute));
        BroadcastAction(PlayerActionKind.SendWave, unitType: (int)UnitTypeDto.Brute);
    }

    private void ExecutePlaceTower(GridPositionDto position)
    {
        if (!CanPlaceTower) return;

        if (Snapshot.Hud.Phase != MatchPhaseDto.Preparation)
        {
            LastError = "ACTION IMPOSSIBLE : Attendez la phase de préparation.";
            return;
        }

        var existingTower = Snapshot.Towers.FirstOrDefault(t => t.Cell.X == position.X && t.Cell.Y == position.Y);

        // CAS 1 : On vient de "lâcher" une tour sélectionnée sur une case vide
        if (_movingTowerFrom != null && existingTower == null)
        {
            var oldPos = _movingTowerFrom.Value;
            var result = _session.MoveTower(oldPos, position);
            
            if (result.IsSuccess)
            {
                BroadcastAction(PlayerActionKind.MoveTower, x: position.X, y: position.Y, oldX: oldPos.X, oldY: oldPos.Y);
                _movingTowerFrom = null;
                Apply(result);
                LastError = null; // Effacer le message de sélection
                return;
            }
        }

        // CAS 2 : On clique sur une tour (pour commencer un drag)
        if (existingTower != null)
        {
            _movingTowerFrom = position;
            LastError = "DÉPLACEMENT : Maintenez et glissez vers une case vide.";
            return;
        }

        // CAS 3 : On relâche sur une case vide sans tour sélectionnée (ou après avoir annulé)
        // On ne construit que si on n'était pas en train de tenter un déplacement
        if (_movingTowerFrom == null)
        {
            Apply(_session.PlaceTower(CurrentTowerType, position));
            BroadcastAction(PlayerActionKind.PlaceTower, towerType: (int)CurrentTowerType, x: position.X, y: position.Y);
        }
        
        // Reset de sécurité
        _movingTowerFrom = null;
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
            _networkClient.OnGameStarted += (seed, startTime) => Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                _session = GameSession.CreateMvp(seed);
                _gameStartTime = new DateTime(startTime, DateTimeKind.Utc);
                _totalTicksProcessed = 0;
                IsGameStarted = true;
            });
            _networkClient.OnOpponentReady += (ready) => Avalonia.Threading.Dispatcher.UIThread.Post(() => IsOpponentReady = ready);
            _networkClient.OnGameListReceived += (games) => Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                _availableGames.Clear();
                foreach (var g in games) _availableGames.Add(g);
            });
            
            await _networkClient.StartAsync();
            IsConnected = true;
            
            // On récupère la liste initiale
            await _networkClient.GetActiveGames();
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

    private async Task ExecuteRefreshGames()
    {
        if (_networkClient != null)
        {
            await _networkClient.GetActiveGames();
        }
    }

    private async Task ExecuteJoinSpecificGame(string gameId)
    {
        if (_networkClient != null)
        {
            await _networkClient.JoinGame(gameId);
            IsInGameRoom = true;
        }
    }

    private void ExecuteStartSolo()
    {
        IsGameStarted = true;
    }

    private void ExecuteReplay()
    {
        IsGameStarted = false;
        IsInGameRoom = false;
        IsReady = false;
        IsOpponentReady = false;
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
                case PlayerActionKind.MoveTower:
                    if (action.X.HasValue && action.Y.HasValue && action.OldX.HasValue && action.OldY.HasValue)
                    {
                        Apply(_session.MoveTower(
                            new GridPositionDto(action.OldX.Value, action.OldY.Value), 
                            new GridPositionDto(action.X.Value, action.Y.Value)));
                    }
                    break;
                case PlayerActionKind.SkipPreparation:
                    Apply(_session.SkipPreparation());
                    break;
            }
        });
    }

    private void BroadcastAction(PlayerActionKind kind, int? towerType = null, int? x = null, int? y = null, int? unitType = null, int? oldX = null, int? oldY = null)
    {
        if (_networkClient != null && IsConnected)
        {
            var action = new PlayerAction(1, kind, towerType, x, y, unitType, oldX, oldY, _totalTicksProcessed);
            _ = _networkClient.SendPlayerAction(action);
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

public class TowerTypeToBrushConverter : Avalonia.Data.Converters.IValueConverter
{
    public static readonly TowerTypeToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is TowerTypeDto currentType && parameter is string targetTypeStr)
        {
            if (currentType.ToString() == targetTypeStr)
            {
                return Avalonia.Media.Brush.Parse("#00F2FF"); // Cyan quand sélectionné
            }
        }
        return Avalonia.Media.Brush.Parse("#20FFFFFF"); // Gris transparent par défaut
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

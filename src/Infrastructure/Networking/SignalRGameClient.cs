using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using TowerFluffy.Application.Common.Networking;
using TowerFluffy.Domain.Simulation;

namespace TowerFluffy.Infrastructure.Networking;

public class SignalRGameClient : IGameHub
{
    private readonly HubConnection _connection;

    public SignalRGameClient(string serverUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .AddMessagePackProtocol(options =>
            {
                options.SerializerOptions = MessagePackSerializerOptions.Standard
                    .WithResolver(CompositeResolver.Create(
                        StandardResolver.Instance,
                        ContractlessStandardResolver.Instance
                    ));
            })
            .WithAutomaticReconnect()
            .Build();

        // Register handlers
        _connection.On<GameState>("ReceiveGameState", (state) => OnGameStateReceived?.Invoke(state));
        _connection.On<CombatEvent>("ReceiveCombatEvent", (e) => OnCombatEventReceived?.Invoke(e));
        _connection.On<string, string>("ReceiveChat", (sender, message) => OnChatReceived?.Invoke(sender, message));
        _connection.On<PlayerAction>("ReceivePlayerAction", (action) => OnPlayerActionReceived?.Invoke(action));
        _connection.On("ReceiveGameStarted", () => OnGameStarted?.Invoke());
        _connection.On<bool>("ReceiveOpponentReady", (isReady) => OnOpponentReady?.Invoke(isReady));
    }

    public event Action<GameState>? OnGameStateReceived;
    public event Action<CombatEvent>? OnCombatEventReceived;
    public event Action<string, string>? OnChatReceived;
    public event Action<PlayerAction>? OnPlayerActionReceived;
    public event Action? OnGameStarted;
    public event Action<bool>? OnOpponentReady;

    public async Task StartAsync()
    {
        await _connection.StartAsync();
    }

    public async Task StopAsync()
    {
        await _connection.StopAsync();
    }

    public async Task JoinGame(string gameId)
    {
        await _connection.InvokeAsync(nameof(JoinGame), gameId);
    }

    public async Task SetReady(bool isReady)
    {
        await _connection.InvokeAsync(nameof(SetReady), isReady);
    }

    public async Task SendPlayerAction(PlayerAction action)
    {
        await _connection.InvokeAsync(nameof(SendPlayerAction), action);
    }

    public async Task SendChat(string message)
    {
        await _connection.InvokeAsync(nameof(SendChat), message);
    }
}

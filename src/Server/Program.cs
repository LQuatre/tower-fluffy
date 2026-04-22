using MessagePack;
using MessagePack.Resolvers;
using TowerFluffy.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR()
    .AddMessagePackProtocol(options =>
    {
        options.SerializerOptions = MessagePackSerializerOptions.Standard
            .WithResolver(CompositeResolver.Create(
                StandardResolver.Instance,
                ContractlessStandardResolver.Instance
            ));
    });

var app = builder.Build();

app.MapHub<GameHub>("/gameHub");

app.MapGet("/", () => "TowerFluffy Game Server is running.");

app.Run();

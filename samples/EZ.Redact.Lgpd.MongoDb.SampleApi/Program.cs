using EZ.Redact.Lgpd.Core;
using EZ.Redact.Lgpd.Core.Attributes;
using EZ.Redact.Lgpd.MongoDb;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLGPDRedaction()
    .AddMongoDbRedaction();

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("exemplo");
});

var app = builder.Build();

app.MapGet("/clientes", async (IMongoDatabase db) =>
{
    var collection = db.GetCollection<Cliente>("clientes");
    var comRedacao = await collection
        .UseRedaction()
        .Find(c => c.Ativo)
        .ToListAsync();

    var semRedacao = await collection
        .Find(c => c.Ativo)
        .ToListAsync();

    return Results.Ok(new { comRedacao, semRedacao });
});

app.Run();

public class Cliente
{
    [CPFData]
    public string? Cpf { get; set; }

    [NomeData]
    public string? Nome { get; set; }

    [EmailData]
    public string? Email { get; set; }

    public bool Ativo { get; set; }
}

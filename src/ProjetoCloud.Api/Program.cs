using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjetoCloud.Api.Data;
using ProjetoCloud.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// O Render injeta a porta na variável PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://+:{port}");

// Connection string: variável DATABASE_URL (Render) ou ConnectionStrings:Default (local).
// É opcional: a aplicação sobe sem banco e o botão da página informa o problema.
var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("Default");
var connectionString = string.IsNullOrWhiteSpace(rawConnectionString)
    ? null
    : ConnectionStringHelper.Normalize(rawConnectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (connectionString is null) options.UseNpgsql();
    else options.UseNpgsql(connectionString);
});

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Aplica as migrations ao subir apenas se RUN_MIGRATIONS=true,
// sem derrubar a aplicação se o banco estiver indisponível
var runMigrations = string.Equals(Environment.GetEnvironmentVariable("RUN_MIGRATIONS"), "true", StringComparison.OrdinalIgnoreCase);
if (connectionString is not null && runMigrations)
{
    try
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Falha ao aplicar migrations");
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapHealthChecks("/health");

app.MapGet("/api/db/status", async () =>
{
    if (connectionString is null)
        return Results.Ok(new
        {
            conectado = false,
            erro = "Connection string não configurada. Defina a variável DATABASE_URL."
        });

    var csb = new NpgsqlConnectionStringBuilder(connectionString) { Timeout = 15 };
    var sw = Stopwatch.StartNew();
    try
    {
        await using var conn = new NpgsqlConnection(csb.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT current_database(), version()", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        return Results.Ok(new
        {
            conectado = true,
            servidor = csb.Host,
            banco = reader.GetString(0),
            versao = reader.GetString(1),
            tempoMs = sw.ElapsedMilliseconds
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            conectado = false,
            servidor = csb.Host,
            erro = ex.Message,
            tempoMs = sw.ElapsedMilliseconds
        });
    }
});

var tarefas = app.MapGroup("/tarefas");

tarefas.MapGet("/", async (AppDbContext db) =>
    await db.Tarefas.OrderBy(t => t.Id).ToListAsync());

tarefas.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Tarefas.FindAsync(id) is { } tarefa ? Results.Ok(tarefa) : Results.NotFound());

tarefas.MapPost("/", async (TarefaInput input, AppDbContext db) =>
{
    var tarefa = new Tarefa { Titulo = input.Titulo, Concluida = input.Concluida };
    db.Tarefas.Add(tarefa);
    await db.SaveChangesAsync();
    return Results.Created($"/tarefas/{tarefa.Id}", tarefa);
});

tarefas.MapPut("/{id:int}", async (int id, TarefaInput input, AppDbContext db) =>
{
    var tarefa = await db.Tarefas.FindAsync(id);
    if (tarefa is null) return Results.NotFound();

    tarefa.Titulo = input.Titulo;
    tarefa.Concluida = input.Concluida;
    await db.SaveChangesAsync();
    return Results.Ok(tarefa);
});

tarefas.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var tarefa = await db.Tarefas.FindAsync(id);
    if (tarefa is null) return Results.NotFound();

    db.Tarefas.Remove(tarefa);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

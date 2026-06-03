using DeskConcierge.Core.Abstractions;
using DeskConcierge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DeskConcierge")
    ?? "Data Source=deskconcierge.db";

builder.Services.AddDbContext<DeskConciergeDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/health", () => new { status = "ok" });

app.Run();

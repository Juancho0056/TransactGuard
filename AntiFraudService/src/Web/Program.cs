using AntiFraudService.Infrastructure.Data;
using AntiFraudService.Web.Endpoints;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseHealthChecks("/health");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.UseProblemDetailsStatusCodePages((_, problem) =>
{
    problem.Type = "https://errors.example.com/transactions/not-found";
    problem.Detail = "No se encontró la transacción solicitada.";
});

app.Map("/", () => Results.Redirect("/api"));

app.MapEvaluationsEndpoints();

app.Run();

public partial class Program { }

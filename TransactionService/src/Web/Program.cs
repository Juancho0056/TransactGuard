using TransactionService.Infrastructure.Data;
using TransactionService.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
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

app.MapTransactionEndpoints();

app.Run();

public partial class Program { }

using Shared.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddCorrelationIdMiddleware();

var app = builder.Build();
app.UseCorrelationIdMiddleware();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/", () => Results.Redirect("/health"));

app.MapReverseProxy();

app.Run();

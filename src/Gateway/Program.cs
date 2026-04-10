const string CorrelationIdHeader = "X-Correlation-Id";

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.Use(async (context, next) =>
{
	var correlationId = context.Request.Headers[CorrelationIdHeader].ToString();

	if (string.IsNullOrWhiteSpace(correlationId))
	{
		correlationId = Guid.NewGuid().ToString();
		context.Request.Headers[CorrelationIdHeader] = correlationId;
	}

	context.Response.Headers[CorrelationIdHeader] = correlationId;
	await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapGet("/", () => Results.Redirect("/health"));

app.MapReverseProxy();

app.Run();

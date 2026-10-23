using System.Text.Json.Serialization;

using Restaurant.Api.Common;
using Restaurant.Application;
using Restaurant.Infrastructure;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        if (!context.ProblemDetails.Extensions.ContainsKey("code"))
        {
            context.ProblemDetails.Extensions["code"] = context.ProblemDetails.Status switch
            {
                400 => "Request.Invalid",
                401 => "Auth.Unauthenticated",
                403 => "Auth.Forbidden",
                404 => "Resource.NotFound",
                409 => "Conflict.Duplicate",
                422 => "Domain.Invalid",
                429 => "RateLimit.Exceeded",
                _ => "Server.Unexpected",
            };
        }
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<OpenApiExtensions>());

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Restaurant.Application.Abstractions.ICurrentUser, CurrentUser>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpoints(typeof(IEndpoint).Assembly);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapGroup("/api/v1").MapEndpoints();

app.Run();

public partial class Program;

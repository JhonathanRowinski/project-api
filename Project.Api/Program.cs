using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Project.Application.Auth;
using Project.Application.Auth.Interfaces;
using Project.Api.Exceptions;
using Project.Infrastructure;
using Project.Infrastructure.Database;
using Project.Infrastructure.Auth.Tokens;
using Scalar.AspNetCore;
using Project.Application.Auth.Interfaces.Repositories;

var builder = WebApplication.CreateBuilder(args);
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException(
        "Configure Authentication:Jwt:SigningKey with a secret of at least 32 bytes using user secrets or an environment variable."
    );
}

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddPolicy("LocalFrontend", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()));
}

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sessionIdValue = context.Principal?.FindFirst("sid")?.Value;

                if (!Guid.TryParse(sessionIdValue, out var sessionId))
                {
                    context.Fail("The token does not identify an active session.");
                    return;
                }

                var sessionRepository = context.HttpContext.RequestServices.GetRequiredService<IAuthSessionRepository>();
                var isActive = await sessionRepository.IsActiveAsync(sessionId, DateTime.UtcNow);

                if (!isActive)
                    context.Fail("The token session is revoked or expired.");
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

app.Services.GetRequiredService<DatabaseScriptRunner>().Run();

if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalFrontend");
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

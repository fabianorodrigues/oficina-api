using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Oficina.Api.Endpoints;
using Oficina.Api.Filters;
using Oficina.Api.Middlewares;
using Oficina.Api.Security;
using Oficina.Application;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Infrastructure;
using Oficina.Infrastructure.Persistencia;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

if (string.Equals(builder.Configuration["APP_MODE"], "migration", StringComparison.OrdinalIgnoreCase))
{
    var migrationApp = builder.Build();

    try
    {
        using var scope = migrationApp.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();

        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Migrations aplicadas com sucesso.");
        return;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Falha ao aplicar migrations: {ex.GetType().Name}.");
        Environment.ExitCode = 1;
        return;
    }
}

builder.Services.AddScoped<FluentValidationActionFilter>();

builder.Services.AddControllers(opt =>
{
    opt.Filters.Add<FluentValidationActionFilter>();
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Oficina API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Informe o token JWT no formato: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

builder.Services.AddApplication();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();
builder.Services.AddSingleton<IAdminInicialBootstrapper, AdminInicialBootstrapperAdapter>();
builder.Services.Configure<AdminInicialBootstrapOptions>(
    builder.Configuration.GetSection("AdminInicialBootstrap"));
builder.Services.AddHostedService<AdminInicialBackgroundService>();

var jwtKey = builder.Configuration["Jwt:Secret"]
             ?? builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("Jwt:Secret missing.");

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Secret deve ter ao menos 32 caracteres.");
}

var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false;

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(Policies.ClienteOnly, policy =>
        policy.RequireRole(PerfisAcesso.Cliente));

    options.AddPolicy(Policies.FuncionarioOuAdmin, policy =>
        policy.RequireRole(PerfisAcesso.Funcionario, PerfisAcesso.Admin));

    options.AddPolicy(Policies.AdminOnly, policy =>
        policy.RequireRole(PerfisAcesso.Admin));

    options.AddPolicy(Policies.ClienteOuAdmin, policy =>
        policy.RequireRole(PerfisAcesso.Cliente, PerfisAcesso.Admin));
});

var app = builder.Build();

app.UseSwagger(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Oficina API v1");
    options.RoutePrefix = "swagger";
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();

app.MapControllers();

app.Run();

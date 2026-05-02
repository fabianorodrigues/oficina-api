using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Oficina.Api.Middlewares;
using Oficina.Api.Security;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Application;
using Oficina.Infrastructure;
using Oficina.Infrastructure.Persistencia;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Oficina API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, new List<string>() } });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

var jwtKey = builder.Configuration["Jwt:Secret"] ?? builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Secret missing.");
if (jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Secret deve ter ao menos 32 caracteres.");

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

    options.AddPolicy(Policies.ClienteOnly, policy => policy.RequireRole(PerfisAcesso.Cliente));
    options.AddPolicy(Policies.FuncionarioOuAdmin, policy => policy.RequireRole(PerfisAcesso.Funcionario, PerfisAcesso.Admin));
    options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(PerfisAcesso.Admin));
    options.AddPolicy(Policies.ClienteOuAdmin, policy => policy.RequireRole(PerfisAcesso.Cliente, PerfisAcesso.Admin));
});

var app = builder.Build();

var runMigration = builder.Configuration.GetValue<bool>("RUN_MIGRATION");

if (runMigration)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        try
        {
            var dbContext = services.GetRequiredService<OficinaDbContext>();
            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao executar migration: {ex.Message}");
            throw;
        }
    }
}

await AdminInicialBootstrapper.GarantirAdminInicial(app);

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
   .AllowAnonymous();

app.MapControllers();

app.Run();

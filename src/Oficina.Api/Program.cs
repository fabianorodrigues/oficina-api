using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Oficina.Api.Filters;
using Oficina.Api.Middlewares;
using Oficina.Api.Security;
using Oficina.Application;
using Oficina.Application.Abstractions.Seguranca;
using Oficina.Infrastructure;
using Oficina.Infrastructure.Persistencia;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();

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

var runMigration = builder.Configuration.GetValue<bool>("RUN_MIGRATION");

await ExecutarInicializacaoBancoComRetry(app, runMigration);

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

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
   .AllowAnonymous();

app.MapControllers();

app.Run();

static async Task ExecutarInicializacaoBancoComRetry(WebApplication app, bool runMigration)
{
    const int maxTentativas = 12;
    var intervalo = TimeSpan.FromSeconds(5);

    for (var tentativa = 1; tentativa <= maxTentativas; tentativa++)
    {
        try
        {
            if (runMigration)
            {
                using var scope = app.Services.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();

                await dbContext.Database.MigrateAsync();
            }

            await AdminInicialBootstrapper.GarantirAdminInicial(app);

            return;
        }
        catch (Exception ex) when (tentativa < maxTentativas)
        {
            Console.WriteLine(
                $"Banco indisponivel na tentativa {tentativa}/{maxTentativas}. " +
                $"Nova tentativa em {intervalo.TotalSeconds}s. Erro: {ex.Message}");

            await Task.Delay(intervalo);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Erro ao inicializar banco apos {maxTentativas} tentativas: {ex.Message}");

            throw;
        }
    }
}

using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using InventoryApp.API.Middleware;
using InventoryApp.Application.Interfaces;
using InventoryApp.Application.Services;
using InventoryApp.Application.Validators;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Enums;
using InventoryApp.Infrastructure.Persistence;
using InventoryApp.Infrastructure.Repositories;
using InventoryApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ===== Controllers + FluentValidation =====
builder.Services.AddControllers();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 30 * 1024 * 1024;
});
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductoValidator>();

// ===== DbContext (MySQL) =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurado.");
var mysqlServerVersion = Version.Parse(builder.Configuration["Database:ServerVersion"] ?? "8.4.3");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(mysqlServerVersion)));

// ===== Repositorios y Servicios =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
builder.Services.AddScoped<IRolPermisoRepository, RolPermisoRepository>();
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<IRolService, RolService>();
builder.Services.AddScoped<IPermisoRepository, PermisoRepository>();
builder.Services.AddScoped<IPermisoCatalogoService, PermisoCatalogoService>();
builder.Services.AddScoped<IDescuentoRepository, DescuentoRepository>();
builder.Services.AddScoped<IDescuentoService, DescuentoService>();
builder.Services.AddScoped<IImpuestoRepository, ImpuestoRepository>();
builder.Services.AddScoped<IImpuestoService, ImpuestoService>();
builder.Services.AddScoped<ICalculoService, CalculoService>();
builder.Services.AddScoped<IPerfilService, PerfilService>();
builder.Services.AddScoped<IPerfilImagenStorageService, CloudinaryPerfilImagenStorageService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IPermisoService, PermisoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IImageStorageService, CloudinaryImageStorageService>();
builder.Services.AddScoped<ICompraDocumentoStorageService, CloudinaryCompraDocumentoStorageService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICompraRepository, CompraRepository>();
builder.Services.AddScoped<ICompraDocumentoRepository, CompraDocumentoRepository>();
builder.Services.AddScoped<ICompraDocumentoService, CompraDocumentoService>();
builder.Services.AddScoped<IMovimientoInventarioRepository, MovimientoInventarioRepository>();
builder.Services.AddScoped<IMovimientoFinancieroRepository, MovimientoFinancieroRepository>();
builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IFacturaRepository, FacturaRepository>();
builder.Services.AddScoped<IEmpresaConfiguracionRepository, EmpresaConfiguracionRepository>();
builder.Services.AddScoped<IRevisionFinancieraRepository, RevisionFinancieraRepository>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IFacturaService, FacturaService>();
builder.Services.AddScoped<IFacturaPdfService, QuestPdfFacturaService>();
builder.Services.AddScoped<IFacturaCompartirRepository, FacturaCompartirRepository>();
builder.Services.AddScoped<IFacturaCompartirService, FacturaCompartirService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ITemaVisualRepository, TemaVisualRepository>();
builder.Services.AddScoped<ITemaVisualService, TemaVisualService>();
builder.Services.AddScoped<IEmpresaConfiguracionService, EmpresaConfiguracionService>();
builder.Services.AddScoped<IFinanzasService, FinanzasService>();
builder.Services.AddScoped<IMovimientoInventarioService, MovimientoInventarioService>();

// ===== JWT Authentication =====
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret no configurado.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = async context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                var auditoria = context.HttpContext.RequestServices.GetService<IAuditoriaService>();
                if (auditoria is not null)
                {
                    await auditoria.RegistrarAsync(
                        ModuloSistema.Usuarios,
                        AccionPermiso.ConsultarHistorial,
                        "Intento de uso de token expirado.",
                        entidad: "Sesion",
                        resultado: "Rechazado",
                        error: "Token expirado");
                }
            }
        }
    };
});

builder.Services.AddAuthorization();

// ===== CORS =====
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ===== Swagger (con soporte JWT Bearer) =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "InventoryApp API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa: Bearer {tu token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ===== Middleware pipeline =====
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);
app.UseMiddleware<ExceptionHandlingMiddleware>();

var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "InventoryApp API",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
}));
app.MapControllers();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var repairService = new ProductionDataRepairService(db);
    await repairService.RepairAsync();

    var adminUsername = app.Configuration["SeedAdmin:Username"]?.Trim();
    var adminPassword = app.Configuration["SeedAdmin:Password"];

    // SeedAdmin es exclusivamente de creación inicial. Se ejecuta antes del
    // seeding de roles para que una cuenta nueva quede vinculada inmediatamente
    // al rol dinámico Administrador. Una cuenta existente nunca recibe de nuevo
    // contraseña, rol ni estado desde variables de entorno.
    if (!string.IsNullOrWhiteSpace(adminUsername) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var adminExiste = await db.Usuarios.AnyAsync(u => u.NombreUsuario == adminUsername);
        if (!adminExiste)
        {
            db.Usuarios.Add(new Usuario
            {
                NombreUsuario = adminUsername,
                NombreCompleto = "Administrador",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Rol = RolUsuario.Administrador,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    var seedPermisoService = new SeedPermisoService(db);
    await seedPermisoService.SeedDefaultsAsync();

    var seedFiscalService = new SeedFiscalService(db);
    await seedFiscalService.SeedDefaultsAsync();
}

await app.RunAsync();

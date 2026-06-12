using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PairCode.Application.Interfaces;
using PairCode.Application.Services;
using PairCode.Infrastructure.Data;
using PairCode.Infrastructure.Repositories;
using PairCode.Infrastructure.Services;
using PairCode.Application.Validators;
using PairCode.Web.Hubs;
using PairCode.Web.Middleware;
using PairCode.Web.Services;
using Prometheus;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "{Timestamp:o} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    });

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Cookies["AuthToken"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddSignalR();
    builder.Services.AddSingleton<ConnectionTracker>();

    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IRoomRepository, RoomRepository>();
    builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
    builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
    builder.Services.AddScoped<ISharedDocumentRepository, SharedDocumentRepository>();
    builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuditService, AuditService>();

    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<RoomService>();
    builder.Services.AddScoped<ParticipantService>();
    builder.Services.AddScoped<ChatService>();
    builder.Services.AddScoped<SharedDocumentService>();
    builder.Services.AddScoped<DashboardService>();

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("Login", cfg =>
        {
            cfg.PermitLimit = 10;
            cfg.Window = TimeSpan.FromMinutes(1);
            cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            cfg.QueueLimit = 0;
        });
    });

    builder.Services.AddScoped<RegisterUserValidator>();
    builder.Services.AddScoped<CreateRoomValidator>();
    builder.Services.AddScoped<JoinRoomValidator>();
    builder.Services.AddScoped<SendMessageValidator>();

    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "postgres", tags: ["db", "critical"]);

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("PairCode"))
        .WithTracing(t => t
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter());

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseMiddleware<ExceptionMiddleware>();

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (ctx, report) =>
        {
            ctx.Response.ContentType = "application/json";
            var result = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds
                }),
                uptime = (DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds
            };
            await System.Text.Json.JsonSerializer.SerializeAsync(ctx.Response.Body, result);
        }
    });

    app.MapMetrics();

    app.MapHub<ChatHub>("/hubs/chat");
    app.MapHub<DocumentHub>("/hubs/document");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        if (env.IsDevelopment())
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();
    }

    Log.Information("PairCode started");
    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "PairCode terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

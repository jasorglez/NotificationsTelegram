using Microsoft.EntityFrameworkCore;
using NotificationsTelegram;
using NotificationsTelegram.Services;
using NotificationsTelegram.Hub;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NotificationsTelegram API", Version = "v1.0" });
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200",
            "http://66.179.240.10:5010",
            "http://5.181.218.92:5010",
            "http://5.181.218.92:5003",
            "http://198.71.49.16:5009",
            "https://biapp.com.mx"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR();

// HttpClient for callbacks to other microservices
builder.Services.AddHttpClient();

// Database Contexts
builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NotificationsDb")));

builder.Services.AddDbContext<SecurityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SecurityDb")));

// Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITelegramService, TelegramService>();
builder.Services.AddScoped<ICallbackService, CallbackService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

app.Run();

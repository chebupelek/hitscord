using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.other;
using hitscord.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using hitscord.Utils;
using Microsoft.AspNetCore.HttpOverrides;
using hitscord.WebSockets;
using Quartz;
using hitscord.nClamUtil;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HitsContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RoomContext")));

builder.Services.AddDbContext<hitscord.Contexts.TokenContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TokenContext")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient();

builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IChannelService, ChannelService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRolesService, RolesService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IServerService, ServerService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

builder.Services.Configure<ClamAVOptions>(builder.Configuration.GetSection("ClamAV"));
builder.Services.AddSingleton<nClamService>();

builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("Minio"));
builder.Services.AddSingleton<MinioService>();

builder.Services.AddSingleton<WebSocketConnectionStore>();
builder.Services.AddScoped<WebSocketsManager>();
builder.Services.AddScoped<WebSocketHandler>();

builder.Services.AddAuthentication(opt => {
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"]!,
            ValidAudience = builder.Configuration["Jwt:Audience"]!,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Your API", Version = "v1" });

	c.AddServer(new OpenApiServer { Url = "/api" });

	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

builder.Services.AddQuartz(q =>
{
	var dailyJobKey = new JobKey("DailyJob");
	q.AddJob<DailyJobService>(opts => opts.WithIdentity(dailyJobKey));
	q.AddTrigger(opts => opts
		.ForJob(dailyJobKey)
		.WithIdentity("DailyTrigger")
		.WithCronSchedule("0 0 * * * ?"));

	var pairAttendanceJobKey = new JobKey("PairAttendanceTracker");
	q.AddJob<PairAttendanceTrackerJob>(opts => opts.WithIdentity(pairAttendanceJobKey));
	q.AddTrigger(opts => opts
		.ForJob(pairAttendanceJobKey)
		.WithIdentity("PairAttendanceTracker-trigger")
		.WithSimpleSchedule(x => x.WithIntervalInSeconds(30).RepeatForever()));

	var missingPairNotifierKey = new JobKey("MissingPairNotifierJob");
	q.AddJob<MissingPairNotifierJob>(opts => opts.WithIdentity(missingPairNotifierKey));
	q.AddTrigger(opts => opts
		.ForJob(missingPairNotifierKey)
		.WithIdentity("MissingPairNotifierJob-trigger")
		.WithCronSchedule("0 0 22 * * ?"));

	var remMesJob = new JobKey("remMesJob");
	q.AddJob<RemoveMessagesJob>(opts => opts.WithIdentity(remMesJob));
    q.AddTrigger(opts => opts
        .ForJob(remMesJob)
        .WithIdentity("remMesJob-Trigger")
        .WithCronSchedule("0 0 0 * * ?"));

	var removeOldFilesKey = new JobKey("removeOldFilesJob");
	q.AddJob<RemoveOldFilesJob>(opts => opts.WithIdentity(removeOldFilesKey));
	q.AddTrigger(opts => opts
		.ForJob(removeOldFilesKey)
		.WithIdentity("removeOldFiles-trigger")
		.WithCronSchedule("0 0 * * * ?"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var HitsContext = scope.ServiceProvider.GetRequiredService<HitsContext>();
    await HitsContext.Database.MigrateAsync();

    var LogContext = scope.ServiceProvider.GetRequiredService<hitscord.Contexts.TokenContext>();
    await LogContext.Database.MigrateAsync();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors("AllowSpecificOrigin");

app.UseWebSockets();
app.UseMiddleware<WebSocketMiddleware>();

app.MapGet("/", () => "WebSocket server is running!");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();


app.UseAuthorization();

app.MapControllers();

app.Run();

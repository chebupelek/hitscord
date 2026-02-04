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
using hitscord.Models.db;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddEnvironmentVariables();

builder.Services.Configure<ClamAVOptions>(options =>
{
	options.Host = builder.Configuration["CLAMAV_HOST"] ?? "clamav";

	var portStr = builder.Configuration["CLAMAV_PORT"];
	if (!int.TryParse(portStr, out var port))
	{
		port = 3310;
	}

	options.Port = port;
});

string dbHost = builder.Configuration["DB_HOST"]!;
string dbUser = builder.Configuration["DB_USER"]!;
string dbPassword = builder.Configuration["DB_PASSWORD"]!;
string dbNameFirst = builder.Configuration["DB_NAME_FIRST"]!;
string dbNameSecond = builder.Configuration["DB_NAME_SECOND"]!;

string roomConn =
	$"Host={dbHost};Database={dbNameFirst};Username={dbUser};Password={dbPassword};";

string tokenConn =
	$"Host={dbHost};Database={dbNameSecond};Username={dbUser};Password={dbPassword};";

builder.Services.AddDbContext<HitsContext>(options =>
    options.UseNpgsql(roomConn));

builder.Services.AddDbContext<hitscord.Contexts.TokenContext>(options =>
    options.UseNpgsql(tokenConn));

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
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.Configure<ApiSettings>(options =>
{
	options.BaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "https://default.url";
});

builder.Services.AddSingleton<nClamService>();

builder.Services.Configure<MinioSettings>(options =>
{
	options.Endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? "minio:9000";
	options.AccessKey = Environment.GetEnvironmentVariable("MINIO_USER") ?? "";
	options.SecretKey = Environment.GetEnvironmentVariable("MINIO_PASSWORD") ?? "";
	options.BucketName = Environment.GetEnvironmentVariable("MINIO_BUCKET") ?? "";
	options.UseSSL = false;
});

builder.Services.AddSingleton<MinioService>();

builder.Services.AddSingleton<WebSocketConnectionStore>();
builder.Services.AddScoped<WebSocketsManager>();
builder.Services.AddScoped<WebSocketHandler>();

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "defaultSecretTooShort";
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
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
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

	if (!HitsContext.SystemRole.Any())
	{
		HitsContext.SystemRole.AddRange(new[]
		{
			new SystemRoleDbModel
			{
				Name = "Студент",
				Type = SystemRoleTypeEnum.Student,
				ParentRoleId = null,
				ParentRole = null,
				ChildRoles = new List<SystemRoleDbModel>(),
				Users = new List<UserDbModel>()
			},
			new SystemRoleDbModel
			{
				Name = "Преподаватель",
				Type = SystemRoleTypeEnum.Teacher,
				ParentRoleId = null,
				ParentRole = null,
				ChildRoles = new List<SystemRoleDbModel>(),
				Users = new List<UserDbModel>()
			}
		});

		await HitsContext.SaveChangesAsync();
	}


	var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
	await adminService.CreateAccountOnce();

	var LogContext = scope.ServiceProvider.GetRequiredService<hitscord.Contexts.TokenContext>();
    await LogContext.Database.MigrateAsync();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseWebSockets();
app.UseMiddleware<WebSocketMiddleware>();

app.MapGet("/", () => "WebSocket server is running!");

/*
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
*/
app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API v1");
	c.RoutePrefix = "swagger"; // Доступ по /swagger
});

app.UseAuthentication();


app.UseAuthorization();

app.MapControllers();

app.Run();

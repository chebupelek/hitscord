using HitscordLibrary.Models.other;
using Message.Contexts;
using Message.IServices;
using Message.OrientDb.Service;
using Message.Services;
using Message.Utils;
using Message.WebSockets;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Quartz;
using hitscord_new.DailyJob;
using HitscordLibrary.nClamUtil;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MessageContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MessageContext")));

builder.Services.AddDbContext<HitscordLibrary.Contexts.TokenContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TokenContext")));

builder.Services.AddDbContext<HitscordLibrary.Contexts.FilesContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("FilesContext")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddSingleton<RabbitMQUtil>();

builder.Services.Configure<OrientDbConfig>(builder.Configuration.GetSection("OrientDb"));
builder.Services.AddSingleton<OrientDbService>();

builder.Services.Configure<ClamAVOptions>(builder.Configuration.GetSection("ClamAV"));
builder.Services.AddSingleton<nClamService>();

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

    c.AddServer(new OpenApiServer { Url = "/message" });

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
	var jobKey = new JobKey("DailyJob");

	q.AddJob<DailyJobService>(opts => opts.WithIdentity(jobKey));

	q.AddTrigger(opts => opts
		.ForJob(jobKey)
		.WithIdentity("DailyTrigger")
		.WithCronSchedule("0 0 0 * * ?")
	);
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var messageContext = scope.ServiceProvider.GetRequiredService<MessageContext>();
    await messageContext.Database.MigrateAsync();

    var logger = app.Services.GetRequiredService<ILogger<RabbitMQUtil>>();
    var bus = app.Services.GetRequiredService<RabbitMQUtil>();
    bus = new RabbitMQUtil(app.Services, logger);
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

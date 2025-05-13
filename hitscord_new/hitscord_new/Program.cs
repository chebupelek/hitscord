using hitscord.Contexts;
using hitscord.IServices;
using hitscord.Models.other;
using hitscord.OrientDb.Service;
using hitscord.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using HitscordLibrary.Contexts;
using System.Text;
using HitscordLibrary.Models.other;
using hitscord.Utils;
using Microsoft.AspNetCore.HttpOverrides;
using hitscord.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HitsContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RoomContext")));

builder.Services.AddDbContext<HitscordLibrary.Contexts.TokenContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TokenContext")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IServerService, ServerService>();
builder.Services.AddScoped<IChannelService, ChannelService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();

builder.Services.AddSingleton<RabbitMQUtil>();

builder.Services.Configure<OrientDbConfig>(builder.Configuration.GetSection("OrientDb"));
builder.Services.AddSingleton<OrientDbService>();

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

    //c.AddServer(new OpenApiServer { Url = "/api" });

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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var HitsContext = scope.ServiceProvider.GetRequiredService<HitsContext>();
    await HitsContext.Database.MigrateAsync();

    var LogContext = scope.ServiceProvider.GetRequiredService<HitscordLibrary.Contexts.TokenContext>();
    await LogContext.Database.MigrateAsync();

    var orientDbService = scope.ServiceProvider.GetRequiredService<OrientDbService>();
    await orientDbService.EnsureSchemaExistsAsync();

    var bus = app.Services.GetRequiredService<RabbitMQUtil>();
    bus = new RabbitMQUtil(app.Services);
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

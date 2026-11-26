using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    var xml = Path.Combine(AppContext.BaseDirectory, "RaceDay.Api.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml);
});

if (!isTesting)
{
    builder.Services.AddDbContext<RaceDayDbContext>(o =>
        o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection("AzureBlob"));
    builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
}
else
{
    builder.Services.AddSingleton<IBlobStorageService, InMemoryBlobStorageService>();
}

builder.Services.AddScoped<IPasswordHasher<Participant>, PasswordHasher<Participant>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "RaceDay.Auth";
        o.Cookie.HttpOnly = true;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (!isTesting)
{
    await DatabaseInitializer.InitialiseAsync(app.Services, app.Logger);
}

app.Run();

public partial class Program { }

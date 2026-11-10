using System.Reflection;
using System.Text;
using Azure.Data.Tables;
using CoffeeNChill.Gateway;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 UTF-8 bytes.");
var storage = builder.Configuration["AzureWebJobsStorage"]
    ?? throw new InvalidOperationException("AzureWebJobsStorage is required.");
var functionsUrl = builder.Configuration["Functions:BaseUrl"]
    ?? throw new InvalidOperationException("Functions:BaseUrl is required.");

builder.Services.AddSingleton(new TableServiceClient(storage));
builder.Services.AddScoped<UserStore>();
builder.Services.AddHttpClient("functions", c => c.BaseAddress = new Uri(functionsUrl.TrimEnd('/') + "/"));
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => {
        o.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CoffeeNChill",
            ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CoffeeNChillClients",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CoffeeNChill Gateway", Version = "v1" });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "JWT access token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Bootstrap an administrator only when explicitly configured by the operator.
// Remove bootstrap credentials from the environment after first deployment.
var bootstrapEmail = app.Configuration["BootstrapAdmin:Email"];
var bootstrapPassword = app.Configuration["BootstrapAdmin:Password"];
if (!string.IsNullOrWhiteSpace(bootstrapEmail) && !string.IsNullOrWhiteSpace(bootstrapPassword))
{
    if (bootstrapPassword.Length < 12)
        throw new InvalidOperationException("BootstrapAdmin password must contain at least 12 characters.");
    using var scope = app.Services.CreateScope();
    var store = scope.ServiceProvider.GetRequiredService<UserStore>();
    await store.CreateAsync(bootstrapEmail.Trim().ToLowerInvariant(), "Administrator", bootstrapPassword, "Admin");
}
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();

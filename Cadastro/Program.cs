using Cadastro.Data;
using Cadastro.Servicos.Auth;
using Cadastro.Servicos.Cadastro;
using Cadastro.Servicos.Cupom;
using Cadastro.Servicos.Email;
using Cadastro.Servicos.Utilidade;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Diagnostics.Metrics;
using System.Text;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

/*
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<CadastroContexto>()
    .AddDefaultTokenProviders();
*/

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CadastroContexto>(options =>
    options.UseSqlServer(connectionString));


builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddTransient<ICadastroServico, CadastroServico>();
builder.Services.AddScoped<CadastroServico>();

builder.Services.AddTransient<IUtilServico, UtilServico>();
builder.Services.AddScoped<UtilServico>();

builder.Services.AddTransient<IEmailSender, EnviarEmail>();
builder.Services.AddScoped<EnviarEmail>();

builder.Services.AddScoped<IAuthServico, AuthServico>();
builder.Services.AddScoped<AuthServico>();

builder.Services.AddScoped<ICupomServico, CupomServico>();
builder.Services.AddScoped<CupomServico>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "FenixSystemsCadastro_";
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.WithOrigins("https://localhost:7011/", "https://www.invertexto.com/")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.HeaderName = "RequestVerificationToken";
});

var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
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
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader))
            {
                var cookieHeader = context.Request.Cookies["BearerToken"];
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    context.Token = cookieHeader;
                }
            }
            return Task.CompletedTask;
        }
    };
});

var meter = new Meter("FenixSystems.RateLimiting");
var rateLimitExceededCounter = meter.CreateCounter<long>("rate_limit_exceeded_total", "count", "Total number of requests rejected due to rate limiting");

builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("CadastrarSlidingLimiter", slidingOptions =>
    {
        slidingOptions.PermitLimit = 5;
        slidingOptions.Window = TimeSpan.FromMinutes(1); 
        slidingOptions.SegmentsPerWindow = 4; 
        slidingOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        slidingOptions.QueueLimit = 0;
    });

    options.AddSlidingWindowLimiter("VerificationSlidingLimiter", slidingOptions =>
    {
        slidingOptions.PermitLimit = 10; 
        slidingOptions.Window = TimeSpan.FromMinutes(1);
        slidingOptions.SegmentsPerWindow = 2;
        slidingOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        slidingOptions.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var endpoint = context.HttpContext.Request.Path;
        logger.LogWarning("Rate limit exceeded for IP: {IPAddress}, Endpoint: {Endpoint}, Policy: {PolicyName}",
            ipAddress, endpoint, context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);

        rateLimitExceededCounter.Add(1, new KeyValuePair<string, object>("ip_address", ipAddress),
                                     new KeyValuePair<string, object>("endpoint", endpoint.ToString()));

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var retryAfterSeconds = 60; 
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
        {
            retryAfterSeconds = (int)retryAfterValue.TotalSeconds;
        }

        context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = $"Você fez muitas requisições. Por favor, aguarde {retryAfterSeconds} segundos antes de tentar novamente.",
            retryAfter = retryAfterSeconds,
            limit = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName == "CadastrarSlidingLimiter" ? 5 : 10,
            window = "1 minuto",
        }, cancellationToken);
    };
});


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cadastro", Version = "v1", Description = "API para cadastro de usuários no sistema" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            new string[] {}
        }
    });

    // Optional: Add XML comments for better documentation (if you have XML comments in your code)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseDefaultFiles();

app.UseRouting();
app.UseCors("AllowAllOrigins");




app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.UseAntiforgery();

app.MapGet("/", async (HttpContext context) =>
{
    context.Response.Redirect("/html/index.html");
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cadastro API v1");
        c.RoutePrefix = string.Empty; // Makes Swagger UI available at the root URL (e.g., https://localhost:7011/)
    });
}

app.MapControllers();

app.Run();
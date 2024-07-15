using OCIRegistry.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using OCIRegistry.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using OCIRegistry.Configuration;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Logger.Information(@"
  _____            _     _                _   _ ______ _______ 
 |  __ \          (_)   | |              | \ | |  ____|__   __|
 | |__) |___  __ _ _ ___| |_ _ __ _   _  |  \| | |__     | |   
 |  _  // _ \/ _` | / __| __| '__| | | | | . ` |  __|    | |   
 | | \ \  __/ (_| | \__ \ |_| |  | |_| |_| |\  | |____   | |   
 |_|  \_\___|\__, |_|___/\__|_|   \__, (_)_| \_|______|  |_|   
              __/ |                __/ |                       
             |___/                |___/                        
");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSerilog(lc => lc.ReadFrom.Configuration(builder.Configuration).WriteTo.Console());
builder.Services.AddSingleton<DigestService>();
builder.Services.AddSingleton<BlobUploadService>();
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IBlobStore, FileSystemBlobStore>();

builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection(TokenOptions.SectionName));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Audience = "registry";
        o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidIssuer = "registry-auth",
            ValidateIssuerSigningKey = false,
            RequireSignedTokens = false,
            SignatureValidator = (token, _) => new JsonWebToken(token) // TODO: Implement signature validation
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
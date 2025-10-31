using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"] ?? "my_secret_key";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? "api.todolist";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? "api.todolist.clients";
var jwtExpiresMinStr = Environment.GetEnvironmentVariable("JWT_EXPIRES_MIN") ?? builder.Configuration["Jwt:ExpiresMinutes"];
var jwtExpiresMinutes = int.TryParse(jwtExpiresMinStr, out var mins) ? mins : 60;
var demoUser = Environment.GetEnvironmentVariable("JWT_DEMO_USER");
var demoPass = Environment.GetEnvironmentVariable("JWT_DEMO_PASS");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = key
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IDbConnection>(_ =>
{
    return new MySqlConnection(builder.Configuration.GetConnectionString("MySql"));
});

var app = builder.Build();

// Auto-migrate (create table) if requested
// .vscode/launch.json   "env"
/*var autoMig = (Environment.GetEnvironmentVariable("AUTO_MIGRATE") 
    ?? builder.Configuration["AutoMigrate"])?.ToLowerInvariant();

if (autoMig == "true" || autoMig == "1")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    await db.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS tasks (
            id INT AUTO_INCREMENT PRIMARY KEY,
            title VARCHAR(255) NOT NULL,
            description TEXT NULL,
            completed BOOLEAN NOT NULL DEFAULT FALSE
        );");
}*/

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", (LoginRequest req) =>
{   
    if (!string.IsNullOrWhiteSpace(demoUser) || !string.IsNullOrWhiteSpace(demoPass))
    {
        if (req is null || req.Username != demoUser || req.Password != demoPass)
        {
            return Results.Unauthorized();
        }
    }
    else
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return Results.BadRequest(new { error = "username and password are required" });
    }

    var claims = new[]
    {
        new System.Security.Claims.Claim("username", req!.Username)
    };

    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(jwtExpiresMinutes),
        signingCredentials: creds
    );
    var tokenStr = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = tokenStr });
});

var tasks = app.MapGroup("/").RequireAuthorization();

tasks.MapPost("/tasks", async (IDbConnection db, TaskItem input) =>
{
    var sql = "INSERT INTO tasks (title, description, completed) VALUES (@Title, @Description, @Completed); SELECT LAST_INSERT_ID();";
    var id = await db.ExecuteScalarAsync<long>(sql, input);
    input.Id = (int)id;
    return Results.Created($"/tasks/{id}", input);
});

tasks.MapGet("/tasks", async (IDbConnection db) =>
{
    var sql = "SELECT id AS Id, title AS Title, description AS Description, completed AS Completed FROM tasks;";
    var list = await db.QueryAsync<TaskItem>(sql);
    return Results.Ok(list);
});

tasks.MapGet("/tasks/{id:int}", async (IDbConnection db, int id) =>
{
    var sql = "SELECT id AS Id, title AS Title, description AS Description, completed AS Completed FROM tasks WHERE id=@id;";
    var item = await db.QuerySingleOrDefaultAsync<TaskItem>(sql, new { id });
    return item is null ? Results.NotFound() : Results.Ok(item);
});

tasks.MapPut("/tasks/{id:int}", async (IDbConnection db, int id, TaskItem input) =>
{
    input.Id = id;
    var sql = "UPDATE tasks SET title=@Title, description=@Description, completed=@Completed WHERE id=@Id;";
    var rows = await db.ExecuteAsync(sql, input);
    if (rows == 0) return Results.NotFound();
    return Results.Ok(input);
});

tasks.MapDelete("/tasks/{id:int}", async (IDbConnection db, int id) =>
{
    var sql = "DELETE FROM tasks WHERE id=@id;";
    var rows = await db.ExecuteAsync(sql, new { id });
    if (rows == 0) return Results.NotFound();
    return Results.Ok(new { message = "Task deleted" });
});

app.Run();

record LoginRequest(string Username, string Password);
record TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Completed { get; set; }
}

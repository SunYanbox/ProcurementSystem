using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProcurementSystem.Data;
using ProcurementSystem.Models;
using ProcurementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ProcurementDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("ProcurementDb")));
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Jwt:Key is not configured."))),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Apply any pending EF Core migrations on startup (development convenience).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
    db.Database.Migrate();

    // Seed a department and an unbound employee record so the register → login
    // flow can be exercised before the admin user-management endpoints exist.
    if (app.Environment.IsDevelopment() && !db.Departments.Any())
    {
        var dept = new Department { Name = "研发部" };
        db.Users.Add(new User
        {
            WorkId = "A001",
            Name = "测试员工",
            Department = dept
            // Username and PasswordHash stay null: the employee has not bound
            // a login account yet, which is exactly what register is for.
        });
        db.SaveChanges();
    }
}

app.MapControllers();

app.Run();

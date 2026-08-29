using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ProcurementDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("ProcurementDb")));
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

// Apply any pending EF Core migrations on startup (development convenience).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
    db.Database.Migrate();
}

app.MapControllers();

app.Run();

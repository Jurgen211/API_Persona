using API_Persona.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//Agregar servicios al container
//Configurar conexion a Postgres
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Tu app de Angular
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Si necesitas enviar cookies/auth
    });
});

var app = builder.Build();
// Usar CORS ANTES de otros middlewares
app.UseCors("AllowAngularApp");


//Configuracion del Http
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

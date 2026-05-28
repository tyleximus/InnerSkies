using InnerSky.WebApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string WasmCorsPolicy = "WasmClient";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<InnerSkyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("InnerSky")));

builder.Services.AddCors(options =>
{
    options.AddPolicy(WasmCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5150",
                "https://localhost:7002")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
//app.UseCors(WasmCorsPolicy);
app.UseAuthorization();
app.MapControllers();
app.Run();

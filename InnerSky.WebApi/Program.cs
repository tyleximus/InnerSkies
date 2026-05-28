using InnerSky.WebApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<InnerSkyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("InnerSky")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        /// CORS notes
        /// - Use a CORS tester: https://cors-test.codehappy.dev/
        /// - Be precise: Trailing forward slashes result in CORS block
        /// - IIS URL rewrite rules (redirects/rewrites) strip these headers & trigger CORS
        ///   - i.e. AllowAnyHeader() et al do not fix these problems
        /// - If IIS must intervene (URL rewrites, reverse proxies, etc.)
        ///   - Download IIS Cors module: https://www.iis.net/downloads/microsoft/iis-cors-module
        ///   - Configure: https://learn.microsoft.com/en-us/iis/extensions/cors-module/cors-module-configuration-reference#cors-configuration
        ///   - Keep Headers: https://www.carlosag.net/articles/enable-cors-access-control-allow-origin.cshtml
        ///   - Custom Headers: https://learn.microsoft.com/en-us/iis/extensions/url-rewrite-module/modifying-http-response-headers
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;

                /// Allow any port on localhost (http or https)
                if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                    return true;

                /// Allow exact production domain and any subdomain
                if (uri.Host.Equals("innersky.tyleximus.com", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (uri.Host.EndsWith(".innersky.tyleximus.com", StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            })
            .AllowAnyHeader() /// Allows header content-type
            .AllowAnyMethod() /// Allows method PUT
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();

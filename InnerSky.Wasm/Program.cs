using InnerSky.Wasm;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://localhost:5271/") });

builder.Services.AddScoped<InnerSky.Wasm.Services.EmotionStateService>();
builder.Services.AddScoped<InnerSky.Wasm.Services.EmotionProfilesApiClient>();

await builder.Build().RunAsync();

using Blazored.LocalStorage;
using Ledajans.Client;
using Ledajans.Client.Auth;
using Ledajans.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var culture = TurkishFormat.Culture;
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());

builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddHttpClient("api", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<AuthHeaderHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<GeofenceService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<NonWorkingDayService>();
builder.Services.AddScoped<GeolocationService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<DepartmentService>();

await builder.Build().RunAsync();

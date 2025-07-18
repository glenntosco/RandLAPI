using RandLAPI;
using RandLAPI.Services;
using RandLAPI.Settings;

var builder = Host.CreateApplicationBuilder(args);

if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "RandL-BOL-Integration";
    });
}

builder.Services.Configure<ServiceSettings>(
    builder.Configuration.GetSection("ServiceSettings"));

builder.Services.Configure<RLCarrierSettings>(
    builder.Configuration.GetSection("RLCarrierSettings"));

builder.Services.AddHttpClient<IPickTicketService, PickTicketService>();
builder.Services.AddHttpClient<IRLCarrierService, RLCarrierService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

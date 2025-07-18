// Import necessary namespaces for our application
using RandLAPI;
using RandLAPI.Services;
using RandLAPI.Settings;

// Create a host builder - this is the foundation for our background service application
// The host manages the application lifecycle, dependency injection, and configuration
var builder = Host.CreateApplicationBuilder(args);

// Check if we're running on Windows, and if so, configure the app to run as a Windows Service
// This allows the application to be installed and managed by Windows Service Control Manager
if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options =>
    {
        // Set the service name that will appear in Windows Services
        options.ServiceName = "RandL-BOL-Integration";
    });
}

// Configure strongly-typed settings from appsettings.json
// This reads the "ServiceSettings" section and maps it to the ServiceSettings class
// This is the "Options Pattern" - a clean way to handle configuration in .NET
builder.Services.Configure<ServiceSettings>(
    builder.Configuration.GetSection("ServiceSettings"));

// Configure R&L Carrier API settings the same way
// This reads the "RLCarrierSettings" section and maps it to the RLCarrierSettings class
builder.Services.Configure<RLCarrierSettings>(
    builder.Configuration.GetSection("RLCarrierSettings"));

// Register HTTP clients for our services using dependency injection
// AddHttpClient creates and configures HttpClient instances for each service
// This ensures proper disposal and allows for configuration like base URLs and headers
builder.Services.AddHttpClient<IPickTicketService, PickTicketService>();
builder.Services.AddHttpClient<IRLCarrierService, RLCarrierService>();

// Register our main background worker service
// AddHostedService tells the host to run this service in the background
// The Worker class will start automatically when the application starts
builder.Services.AddHostedService<Worker>();

// Build the host with all our configured services
// This creates the actual application instance with all dependencies wired up
var host = builder.Build();

// Start the application and keep it running
// This will start the Worker service and keep the application alive until stopped
host.Run();

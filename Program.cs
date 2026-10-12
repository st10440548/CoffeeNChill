using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using CoffeeNChill;
var host = new HostBuilder().ConfigureFunctionsWebApplication()
.ConfigureServices(s => s.AddSingleton<Storage>()).Build();
host.Run();

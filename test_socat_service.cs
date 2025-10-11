using System;
using Microsoft.Extensions.DependencyInjection;
using S7Tools.Extensions;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Test;

class TestSocatService
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddS7ToolsFoundationServices();
        services.AddS7ToolsApplicationServices();
        services.AddS7ToolsViewModels();

        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("Testing service registration...");

        var socatProfileService = serviceProvider.GetService<ISocatProfileService>();

        if (socatProfileService != null)
        {
            Console.WriteLine("✅ ISocatProfileService is registered and can be resolved");
            Console.WriteLine($"Service type: {socatProfileService.GetType().Name}");
        }
        else
        {
            Console.WriteLine("❌ ISocatProfileService is NOT registered or cannot be resolved");
        }

        Console.WriteLine("\nAll registered services:");
        foreach (var service in services)
        {
            if (service.ServiceType.Name.Contains("Socat"))
            {
                Console.WriteLine($"  {service.ServiceType.Name} -> {service.ImplementationType?.Name}");
            }
        }
    }
}

using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Extensions;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Diagnostics;

internal static class Program
{
    private static int Main()
    {
        try
        {
            var services = new ServiceCollection();

            // Minimal logging
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddConsole());

            // Add application services
            services.AddS7ToolsServices();

            var provider = services.BuildServiceProvider();

            var profileService = provider.GetService<ISerialPortProfileService>();
            if (profileService == null)
            {
                Console.WriteLine("ISerialPortProfileService not registered");
                return 2;
            }

            // Ensure a default profile exists (will create and save if needed)
            var defaultProfile = profileService.EnsureDefaultProfileExistsAsync().GetAwaiter().GetResult();

            var info = profileService.GetStorageInfoAsync().GetAwaiter().GetResult();
            var json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("SerialPortProfileService storage info:\n" + json);

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Diagnostics failed: " + ex);
            return 1;
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Extensions;
using S7Tools.Core.Services.Interfaces;

namespace TestSocatService;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testing SocatProfileService...");

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddS7ToolsFoundationServices();
        services.AddS7ToolsApplicationServices();

        var serviceProvider = services.BuildServiceProvider();

        var socatProfileService = serviceProvider.GetService<ISocatProfileService>();

        if (socatProfileService == null)
        {
            Console.WriteLine("❌ ISocatProfileService is not registered");
            return;
        }

        Console.WriteLine("✅ ISocatProfileService found");
        Console.WriteLine($"Service type: {socatProfileService.GetType().Name}");

        try
        {
            Console.WriteLine("\n1. Initializing storage...");
            await socatProfileService.InitializeStorageAsync();
            Console.WriteLine("✅ Storage initialized");

            Console.WriteLine("\n2. Getting all profiles...");
            var profiles = await socatProfileService.GetAllProfilesAsync();
            Console.WriteLine($"✅ Found {profiles.Count()} profiles");

            Console.WriteLine("\n3. Ensuring default profile exists...");
            var defaultProfile = await socatProfileService.EnsureDefaultProfileExistsAsync();
            Console.WriteLine($"✅ Default profile: {defaultProfile.Name} (ID: {defaultProfile.Id})");

            Console.WriteLine("\n4. Getting all profiles again...");
            profiles = await socatProfileService.GetAllProfilesAsync();
            Console.WriteLine($"✅ Found {profiles.Count()} profiles");

            Console.WriteLine("\n5. Checking storage info...");
            var storageInfo = await socatProfileService.GetStorageInfoAsync();
            Console.WriteLine($"✅ Storage info:");
            Console.WriteLine($"  - Profiles directory: {storageInfo.ProfilesDirectory}");
            Console.WriteLine($"  - Profiles file: {storageInfo.ProfilesFile}");
            Console.WriteLine($"  - File exists: {storageInfo.FileExists}");
            Console.WriteLine($"  - Profile count: {storageInfo.ProfileCount}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}

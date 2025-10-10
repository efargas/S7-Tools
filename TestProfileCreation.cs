using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Extensions;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Models;

namespace S7Tools;

class TestProfileCreation
{
    static async Task Main(string[] args)
    {
        // Setup services like the main app
        var services = new ServiceCollection();
        services.AddS7ToolsFoundationServices();
        services.AddS7ToolsApplicationServices();

        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("=== Testing SocatProfileService ===");

        // Get the service
        var socatProfileService = serviceProvider.GetService<ISocatProfileService>();

        if (socatProfileService == null)
        {
            Console.WriteLine("❌ FAILED: Could not resolve ISocatProfileService");
            return;
        }

        Console.WriteLine("✅ ISocatProfileService resolved successfully");

        try
        {
            // Initialize storage
            Console.WriteLine("Initializing storage...");
            await socatProfileService.InitializeStorageAsync();
            Console.WriteLine("✅ Storage initialized");

            // Check current profiles
            var profiles = await socatProfileService.GetAllProfilesAsync();
            Console.WriteLine($"Current profiles count: {profiles.Count()}");

            // Create a test profile
            Console.WriteLine("Creating test profile...");
            var testProfile = SocatProfile.CreateUserProfile("Test Profile", "Test Description");
            testProfile.Configuration.TcpPort = 1234; // configuration lives under Configuration

            var createdProfile = await socatProfileService.CreateProfileAsync(testProfile);
            Console.WriteLine($"✅ Profile created: {createdProfile.Name} (ID: {createdProfile.Id})");

            // Check if file was created
            var storageInfo = await socatProfileService.GetStorageInfoAsync();
            Console.WriteLine($"File exists: {storageInfo["FileExists"]}");
            Console.WriteLine($"Profile count: {storageInfo.ProfileCount}");
            Console.WriteLine($"Profiles file: {storageInfo.ProfilesFile}");

            if (storageInfo.FileExists)
            {
                Console.WriteLine("✅ SUCCESS: profiles.json was created!");
            }
            else
            {
                Console.WriteLine("❌ FAILED: profiles.json was NOT created!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}

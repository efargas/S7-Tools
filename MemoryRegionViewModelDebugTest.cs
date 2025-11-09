using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;

// Direct test of the memory region logic without full app dependencies
class Program
{
    // Simplified MemoryMappingProfile just for testing
    public class TestMemoryMappingProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsReadOnly { get; set; }
        public bool IsDefault { get; set; }
        public string Description { get; set; } = "";

        public bool CanModify()
        {
            return !IsReadOnly;
        }
    }

    static void Main(string[] args)
    {
        string profilesPath = "/home/kali/WS/S7-Tools/src/S7Tools/bin/Debug/net8.0/Resources/Profiles/MemoryRegions/MemoryRegionProfiles.json";

        if (!File.Exists(profilesPath))
        {
            Console.WriteLine($"Profiles file not found: {profilesPath}");
            return;
        }

        try
        {
            // Read and deserialize JSON into our test class
            string json = File.ReadAllText(profilesPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            List<TestMemoryMappingProfile>? profiles = JsonSerializer.Deserialize<List<TestMemoryMappingProfile>>(json, options);

            if (profiles == null)
            {
                Console.WriteLine("Failed to deserialize profiles - null result");
                return;
            }

            Console.WriteLine($"=== Memory Region CanModify Debug Test ===");
            Console.WriteLine($"Successfully loaded {profiles.Count} profiles:");
            Console.WriteLine();

            foreach (var profile in profiles)
            {
                Console.WriteLine($"Profile: {profile.Name} (ID: {profile.Id})");
                Console.WriteLine($"  IsReadOnly: {profile.IsReadOnly}");
                Console.WriteLine($"  IsDefault: {profile.IsDefault}");
                Console.WriteLine($"  CanModify(): {profile.CanModify()}");
                Console.WriteLine();

                // Simulate the canModify observable logic
                bool canMod = profile?.CanModify() ?? false;
                Console.WriteLine($"  DEBUG: CanModify for profile '{profile?.Name}' (ID: {profile?.Id}): {canMod} (IsReadOnly: {profile?.IsReadOnly}, IsDefault: {profile?.IsDefault})");
                Console.WriteLine($"  --> Edit command should be {(canMod ? "ENABLED" : "DISABLED")}");
                Console.WriteLine();
            }

            // Test what happens if we simulate profile selection
            Console.WriteLine("=== Profile Selection Simulation ===");
            foreach (var profile in profiles)
            {
                Console.WriteLine($"Selecting profile: {profile.Name}");

                // This simulates the WhenAnyValue logic for SelectedProfile
                var selectedProfile = profile;
                bool canModifyResult = selectedProfile?.CanModify() ?? false;

                Console.WriteLine($"  -> SelectedProfile CanModify: {canModifyResult}");
                Console.WriteLine($"  -> Edit button should be: {(canModifyResult ? "ENABLED" : "DISABLED")}");
                Console.WriteLine();
            }

            // Test null selection (no profile selected)
            Console.WriteLine("=== Null Selection Test (No Profile Selected) ===");
            TestMemoryMappingProfile? selectedProfile = null;
            bool canModifyNull = selectedProfile?.CanModify() ?? false;
            Console.WriteLine($"SelectedProfile is null -> CanModify: {canModifyNull}");
            Console.WriteLine($"Edit button should be: {(canModifyNull ? "ENABLED" : "DISABLED")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}

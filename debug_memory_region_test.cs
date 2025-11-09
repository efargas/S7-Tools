using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

// Simple test to verify JSON deserialization without dependencies
class Program
{
    static void Main(string[] args)
    {
        // Path to the profiles file
        string profilesPath = "src/S7Tools/bin/Debug/net8.0/Resources/Profiles/MemoryRegions/MemoryRegionProfiles.json";

        if (!File.Exists(profilesPath))
        {
            Console.WriteLine($"Profiles file not found: {profilesPath}");
            return;
        }

        try
        {
            // Read and deserialize JSON as dynamic objects first
            string json = File.ReadAllText(profilesPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var profilesArray = JsonSerializer.Deserialize<JsonElement>(json);

            if (profilesArray.ValueKind != JsonValueKind.Array)
            {
                Console.WriteLine("JSON is not an array");
                return;
            }

            Console.WriteLine($"Successfully loaded {profilesArray.GetArrayLength()} profiles:");
            Console.WriteLine();

            foreach (var profileElement in profilesArray.EnumerateArray())
            {
                if (profileElement.TryGetProperty("Name", out var nameElement) &&
                    profileElement.TryGetProperty("Id", out var idElement) &&
                    profileElement.TryGetProperty("IsReadOnly", out var isReadOnlyElement))
                {
                    string name = nameElement.GetString() ?? "Unknown";
                    int id = idElement.GetInt32();
                    bool isReadOnly = isReadOnlyElement.GetBoolean();
                    bool canModify = !isReadOnly; // This is the CanModify logic

                    Console.WriteLine($"Profile: {name} (ID: {id})");
                    Console.WriteLine($"  IsReadOnly: {isReadOnly}");
                    Console.WriteLine($"  CanModify (should be !IsReadOnly): {canModify}");

                    if (profileElement.TryGetProperty("Description", out var descElement))
                    {
                        Console.WriteLine($"  Description: {descElement.GetString()}");
                    }

                    if (profileElement.TryGetProperty("Segments", out var segmentsElement) &&
                        segmentsElement.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"  Segments: {segmentsElement.GetArrayLength()}");
                    }

                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}

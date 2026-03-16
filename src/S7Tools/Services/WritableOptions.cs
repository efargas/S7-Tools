using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using S7Tools.Core.Interfaces.Services;

namespace S7Tools.Services
{
    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly string _basePath;
        private readonly IOptionsMonitor<T> _options;
        private readonly string _section;
        private readonly string _file;
        private readonly object _lock = new object();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public WritableOptions(
            string basePath,
            IOptionsMonitor<T> options,
            string section,
            string file)
        {
            _basePath = basePath;
            _options = options;
            _section = section;
            _file = file;
        }

        public T CurrentValue => _options.CurrentValue;

        public T Get(string? name) => _options.Get(name);

        public IDisposable? OnChange(Action<T, string?> listener) => _options.OnChange(listener);

        public void Update(Action<T> applyChanges)
        {
            lock (_lock)
            {
                var physicalPath = Path.IsPathRooted(_file) ? _file : Path.Combine(_basePath, _file);

                JsonNode? rootNode = null;

                if (File.Exists(physicalPath))
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(physicalPath);
                        if (!string.IsNullOrWhiteSpace(jsonContent))
                        {
                            rootNode = JsonNode.Parse(jsonContent);
                        }
                    }
                    catch
                    {
                        // File might be corrupted, start fresh
                        rootNode = null;
                    }
                }

                if (rootNode == null)
                {
                    rootNode = new JsonObject();
                }

                if (rootNode is not JsonObject jObject)
                {
                    jObject = new JsonObject();
                }

                // Get current strongly-typed settings, apply changes
                var sectionObject = CurrentValue;
                applyChanges(sectionObject);

                // Serialize the mutated section back to JsonNode
                var sectionNode = JsonSerializer.SerializeToNode(sectionObject, JsonOptions);

                // Update or Create the block in the root node (e.g., "App")
                jObject[_section] = sectionNode;

                // Ensure the directory exists before saving
                var dir = Path.GetDirectoryName(physicalPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Atomic write
                var tempPath = physicalPath + ".tmp";
                File.WriteAllText(tempPath, jObject.ToJsonString(JsonOptions));
                File.Move(tempPath, physicalPath, overwrite: true);
            }
        }

        public async Task UpdateAsync(Func<T, Task> applyChanges)
        {
            var physicalPath = Path.IsPathRooted(_file) ? _file : Path.Combine(_basePath, _file);

            JsonNode? rootNode = null;

            if (File.Exists(physicalPath))
            {
                try
                {
                    var jsonContent = await File.ReadAllTextAsync(physicalPath).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(jsonContent))
                    {
                        rootNode = JsonNode.Parse(jsonContent);
                    }
                }
                catch
                {
                    rootNode = null;
                }
            }

            if (rootNode == null)
            {
                rootNode = new JsonObject();
            }

            if (rootNode is not JsonObject jObject)
            {
                jObject = new JsonObject();
            }

            var sectionObject = CurrentValue;
            await applyChanges(sectionObject).ConfigureAwait(false);

            var sectionNode = JsonSerializer.SerializeToNode(sectionObject, JsonOptions);
            jObject[_section] = sectionNode;

            var dir = Path.GetDirectoryName(physicalPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var tempPath = physicalPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, jObject.ToJsonString(JsonOptions)).ConfigureAwait(false);
            File.Move(tempPath, physicalPath, overwrite: true);
        }
    }
}

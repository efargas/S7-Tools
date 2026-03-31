using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using S7Tools.Core.Interfaces.Services;

namespace S7Tools.Services
{
    /// <summary>
    /// Represents the WritableOptions.
    /// </summary>
    public sealed class WritableOptions<T> : IWritableOptions<T>, IDisposable where T : class, new()
    {
        private readonly string _basePath;
        private readonly IOptionsMonitor<T> _options;
        private readonly string _section;
        private readonly string _file;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="WritableOptions{T}"/> class.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the CurrentValue.
        /// </summary>
        public T CurrentValue => _options.CurrentValue;

        /// <summary>
        /// Executes the Get operation.
        /// </summary>
        public T Get(string? name) => _options.Get(name);

        /// <summary>
        /// Executes the OnChange operation.
        /// </summary>
        public IDisposable? OnChange(Action<T, string?> listener) => _options.OnChange(listener);

        /// <summary>
        /// Executes the Update operation.
        /// </summary>
        public void Update(Action<T> applyChanges)
        {
            _writeLock.Wait();
            try
            {
                string physicalPath = Path.IsPathRooted(_file) ? _file : Path.Combine(_basePath, _file);

                JsonNode? rootNode = null;

                if (File.Exists(physicalPath))
                {
                    try
                    {
                        string jsonContent = File.ReadAllText(physicalPath);
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
                T sectionObject = CurrentValue;
                applyChanges(sectionObject);

                // Serialize the mutated section back to JsonNode
                JsonNode? sectionNode = JsonSerializer.SerializeToNode(sectionObject, JsonOptions);

                // Update or Create the block in the root node (e.g., "App")
                jObject[_section] = sectionNode;

                // Ensure the directory exists before saving
                string? dir = Path.GetDirectoryName(physicalPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Atomic write
                string tempPath = physicalPath + ".tmp";
                File.WriteAllText(tempPath, jObject.ToJsonString(JsonOptions));
                File.Move(tempPath, physicalPath, overwrite: true);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// Executes the UpdateAsync operation.
        /// </summary>
        public async Task UpdateAsync(Func<T, Task> applyChanges)
        {
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                string physicalPath = Path.IsPathRooted(_file) ? _file : Path.Combine(_basePath, _file);

                JsonNode? rootNode = null;

                if (File.Exists(physicalPath))
                {
                    try
                    {
                        string jsonContent = await File.ReadAllTextAsync(physicalPath).ConfigureAwait(false);
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

                T sectionObject = CurrentValue;
                await applyChanges(sectionObject).ConfigureAwait(false);

                JsonNode? sectionNode = JsonSerializer.SerializeToNode(sectionObject, JsonOptions);
                jObject[_section] = sectionNode;

                string? dir = Path.GetDirectoryName(physicalPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string tempPath = physicalPath + ".tmp";
                await File.WriteAllTextAsync(tempPath, jObject.ToJsonString(JsonOptions)).ConfigureAwait(false);
                File.Move(tempPath, physicalPath, overwrite: true);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// Executes the Dispose operation.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _writeLock.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}

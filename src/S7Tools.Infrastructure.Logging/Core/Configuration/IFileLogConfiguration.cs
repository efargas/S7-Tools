using Microsoft.Extensions.Logging;

namespace S7Tools.Infrastructure.Logging.Core.Configuration;

public interface IFileLogConfiguration
{
    LogLevel LogLevel { get; set; }
    string GetFilePathForCategory(string category);
}

using System;

namespace S7Tools.Core.Models.Jobs
{
    /// <summary>
    /// Configuration options for the Job Manager service.
    /// </summary>
    public class JobManagerOptions
    {
        /// <summary>
        /// Gets or sets the file system path to the job profiles JSON file.
        /// </summary>
        public string ProfilesPath { get; set; } = string.Empty;
    }
}

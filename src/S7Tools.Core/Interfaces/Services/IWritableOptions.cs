using Microsoft.Extensions.Options;

namespace S7Tools.Core.Interfaces.Services
{
    /// <summary>
    /// Represents an options monitor that can also write changes back to the underlying configuration source.
    /// </summary>
    /// <typeparam name="T">The type of the options.</typeparam>
    public interface IWritableOptions<out T> : IOptionsMonitor<T> where T : class, new()
    {
        /// <summary>
        /// Updates the options value synchronously.
        /// </summary>
        /// <param name="applyChanges">The delegate that applies changes to the options.</param>
        void Update(Action<T> applyChanges);

        /// <summary>
        /// Updates the options value asynchronously.
        /// </summary>
        /// <param name="applyChanges">The asynchronous delegate that applies changes to the options.</param>
        Task UpdateAsync(Func<T, Task> applyChanges);
    }
}

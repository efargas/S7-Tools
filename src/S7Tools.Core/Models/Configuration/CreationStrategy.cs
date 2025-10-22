namespace S7Tools.Core.Models.Configuration
{
    /// <summary>
    /// Defines when and how a resource should be created
    /// </summary>
    public enum CreationStrategy
    {
        /// <summary>
        /// Create the resource during application startup
        /// </summary>
        OnStartup,

        /// <summary>
        /// Create the resource when it is first accessed or needed
        /// </summary>
        OnDemand,

        /// <summary>
        /// Never create the resource automatically - must be created externally
        /// </summary>
        Never
    }
}

using System;

namespace NewsImporterApp.Core
{
    /// <summary>
    /// Interface for handling exceptions globally
    /// </summary>
    public interface IExceptionHandler
    {
        /// <summary>
        /// Adds an exception to the global collection
        /// </summary>
        /// <param name="exception">Exception to add</param>
        void AddException(Exception exception);
    }
} 
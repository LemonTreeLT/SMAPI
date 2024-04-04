using System;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley.Logging;

namespace StardewModdingAPI.Framework
{
    /// <summary>Redirects log output from the game code to a SMAPI monitor.</summary>
    internal class SGameLogger : IGameLogger
    {
        /*********
        ** Fields
        *********/
        /// <summary>The monitor to which to log output.</summary>
        private readonly IMonitor Monitor;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">The monitor to which to log output.</param>
        public SGameLogger(IMonitor monitor)
        {
            this.Monitor = monitor;
        }

        /// <inheritdoc />
        public void Verbose(string message)
        {
            this.Monitor.Log(message);
        }

        /// <inheritdoc />
        public void Debug(string message)
        {
            this.Monitor.Log(message, LogLevel.Debug);
        }

        /// <inheritdoc />
        public void Info(string message)
        {
            this.Monitor.Log(message, LogLevel.Info);
        }

        /// <inheritdoc />
        public void Warn(string message)
        {
            this.Monitor.Log(message, LogLevel.Warn);
        }

        /// <inheritdoc />
        public void Error(string error, Exception? exception = null)
        {
            // steam not loaded
            if (error == "Error connecting to Steam." && exception?.Message == "Steamworks is not initialized.")
            {
                this.Monitor.Log(
#if SMAPI_FOR_WINDOWS
                    I18nUtilities.Get("console.sgame.steam-not-load1"),
#else
                    I18nUtilities.Get("console.sgame.steam-not-load2"),
#endif
                    LogLevel.Error
                );
            }

            // any other error
            else
            {
                string message = exception != null
                    ? $"{error}\n{exception.GetLogSummary()}"
                    : error;

                this.Monitor.Log(message, LogLevel.Error);
            }
        }
    }
}

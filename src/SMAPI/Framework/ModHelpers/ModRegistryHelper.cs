using System;
using System.Collections.Generic;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Internal;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides metadata about installed mods.</summary>
    internal class ModRegistryHelper : BaseHelper, IModRegistry
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying mod registry.</summary>
        private readonly ModRegistry Registry;

        /// <summary>Encapsulates monitoring and logging for the mod.</summary>
        private readonly IMonitor Monitor;

        /// <summary>The APIs accessed by this instance.</summary>
        private readonly Dictionary<string, object?> AccessedModApis = new();

        /// <summary>Generates proxy classes to access mod APIs through an arbitrary interface.</summary>
        private readonly IInterfaceProxyFactory ProxyFactory;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod using this instance.</param>
        /// <param name="registry">The underlying mod registry.</param>
        /// <param name="proxyFactory">Generates proxy classes to access mod APIs through an arbitrary interface.</param>
        /// <param name="monitor">Encapsulates monitoring and logging for the mod.</param>
        public ModRegistryHelper(IModMetadata mod, ModRegistry registry, IInterfaceProxyFactory proxyFactory, IMonitor monitor)
            : base(mod)
        {
            this.Registry = registry;
            this.ProxyFactory = proxyFactory;
            this.Monitor = monitor;
        }

        /// <inheritdoc />
        public IEnumerable<IModInfo> GetAll()
        {
            return this.Registry.GetAll();
        }

        /// <inheritdoc />
        public IModInfo? Get(string uniqueID)
        {
            return this.Registry.Get(uniqueID);
        }

        /// <inheritdoc />
        public bool IsLoaded(string uniqueID)
        {
            return this.Registry.Get(uniqueID) != null;
        }

        /// <inheritdoc />
        public object? GetApi(string uniqueID)
        {
            // validate ready
            if (!this.Registry.AreAllModsInitialized)
            {
                this.Monitor.LogTra("console.mod-registry-helper.access-mod-provided-api", null, LogLevel.Error);
                return null;
            }

            // get the target mod
            IModMetadata? mod = this.Registry.Get(uniqueID);
            if (mod == null)
                return null;

            // fetch API
            if (!this.AccessedModApis.TryGetValue(mod.Manifest.UniqueID, out object? api))
            {
                // if the target has a global API, this is mutually exclusive with per-mod APIs
                if (mod.Api != null)
                    api = mod.Api;

                // else try to get a per-mod API
                else
                {
                    try
                    {
                        api = mod.Mod?.GetApi(this.Mod);
                        if (api != null && !api.GetType().IsPublic)
                        {
                            api = null;
                            this.Monitor.LogTra("console.mod-registry-helper.mod-api-non-public-type", new { ModDisplayName = mod.DisplayName }, LogLevel.Warn);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.LogTra("console.mod-registry-helper.failed-loading-api-from-mod", new { LogSummary = ex.GetLogSummary(), ModDisplayName = mod.DisplayName }, LogLevel.Error);
                        api = null;
                    }
                }

                // cache & log API access
                this.AccessedModApis[mod.Manifest.UniqueID] = api;
                if (api != null)
                    this.Monitor.LogTra("console.mod-registry-helper.access-mod-api", new { ModDisplayName = mod.DisplayName, ApiName = api.GetType().FullName });
            }

            return api;
        }

        /// <inheritdoc />
        public TInterface? GetApi<TInterface>(string uniqueID)
            where TInterface : class
        {
            // get raw API
            object? api = this.GetApi(uniqueID);
            if (api == null)
                return null;

            // validate mapping
            if (!typeof(TInterface).IsInterface)
            {
                this.Monitor.LogTra("console.mod-registry-helper.mod-api-tried-to-map-type", new { ClassName = typeof(TInterface).FullName }, LogLevel.Error);
                return null;
            }
            if (!typeof(TInterface).IsPublic)
            {
                this.Monitor.LogTra("console.mod-registry-helper.mod-api-tried-to-map-interface", new { InterfaceName = typeof(TInterface).FullName }, LogLevel.Error);
                return null;
            }

            // get API of type
            return api is TInterface castApi
                ? castApi
                : this.ProxyFactory.CreateProxy<TInterface>(api, sourceModID: this.ModID, targetModID: uniqueID);
        }
    }
}

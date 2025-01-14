/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Logging;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Provides a default implementation of <see cref="IMapFileProvider"/> that reads from
    /// the local disk
    /// </summary>
    public class LocalDiskMapFileProvider : IMapFileProvider
    {
        private static int _wroteTraceStatement;
        private readonly ConcurrentDictionary<string, MapFileResolver> _cache;
        private IDataProvider _dataProvider;

        /// <summary>
        /// Creates a new instance of the <see cref="LocalDiskFactorFileProvider"/>
        /// </summary>
        public LocalDiskMapFileProvider()
        {
            _cache = new ConcurrentDictionary<string, MapFileResolver>();
        }

        /// <summary>
        /// Initializes our MapFileProvider by supplying our dataProvider
        /// </summary>
        /// <param name="dataProvider">DataProvider to use</param>
        public void Initialize(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Gets a <see cref="MapFileResolver"/> representing all the map
        /// files for the specified market
        /// </summary>
        /// <param name="market">The equity market, for example, 'usa'</param>
        /// <returns>A <see cref="MapFileRow"/> containing all map files for the specified market</returns>
        public MapFileResolver Get(string market)
        {
            // TODO: Consider using DataProvider to load in the files from disk to unify data fetching behavior
            // Reference LocalDiskFactorFile, LocalZipFactorFile, and LocalZipMapFile providers for examples.

            market = market.ToLowerInvariant();
            return _cache.GetOrAdd(market, GetMapFileResolver);
        }

        private static MapFileResolver GetMapFileResolver(string market)
        {
            var mapFileDirectory = Path.Combine(Globals.CacheDataFolder, "equity", market, "map_files");
            if (!Directory.Exists(mapFileDirectory))
            {
                // only write this message once per application instance
                if (Interlocked.CompareExchange(ref _wroteTraceStatement, 1, 0) == 0)
                {
                    Log.Error($"LocalDiskMapFileProvider.GetMapFileResolver({market}): " +
                        $"The specified directory does not exist: {mapFileDirectory}"
                    );
                }
                return MapFileResolver.Empty;
            }
            return new MapFileResolver(MapFile.GetMapFiles(mapFileDirectory, market));
        }
    }
}

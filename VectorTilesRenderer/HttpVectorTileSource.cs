using System.Net;
using System.Net.Http;
using BruTile;
using BruTile.Web;
using System.Threading.Tasks;
using BruTile.Cache;

namespace VectorTilesRenderer
{
    public class HttpVectorTileSource : IAsyncTileSource
    {
        private readonly ITileSchema _schema;
        private readonly string _urlFormatter;
        private readonly IRequest _request;
        private readonly IPersistentCache<byte[]> _cache;

        public HttpVectorTileSource(ITileSchema schema, string urlFormatter, IPersistentCache<byte[]> cache = null)
        {
            _schema = schema;
            _urlFormatter = urlFormatter;
            _request = new BasicRequest(urlFormatter);
            _cache = cache ?? new NullCache();
        }

        public Attribution Attribution { get; set; }

        public string Name { get; set; }

        public ITileSchema Schema
        {
            get { return _schema; }
        }

        public Task<byte[]> GetTileAsync(TileInfo tileInfo)
        {
            var existing = _cache.Find(tileInfo.Index);

            var cacheTask = new Task<byte[]>(() => existing);
            cacheTask.Start();

            if (existing != null)
                return cacheTask;

            var url = _request.GetUri(tileInfo);

            var gzipWebClient = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            var fetchTask = gzipWebClient.GetByteArrayAsync(url);
            fetchTask.ContinueWith(t =>
            {
                if (!t.IsFaulted)
                    _cache.Add(tileInfo.Index, t.Result);
            });

            return fetchTask;
        }
    }
}

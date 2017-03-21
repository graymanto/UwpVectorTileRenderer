using BruTile;
using System.Threading.Tasks;

namespace VectorTilesRenderer
{
    public interface IAsyncTileSource
    {
        Attribution Attribution { get; }
        string Name { get; }
        ITileSchema Schema { get; }
        Task<byte[]> GetTileAsync(TileInfo tileInfo);
    }
}

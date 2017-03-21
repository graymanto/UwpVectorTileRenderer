using BruTile;
using BruTile.Cache;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace VectorTilesRenderer.Cache
{
    // A cache that saves map files to disk.
    // Attention, only slightly thread safe.
    public class DiskCache : IPersistentCache<byte[]>
    {
        private string _directory;
        private object _writeLock = new object();

        public DiskCache(string directory)
        {
            _directory = directory;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void Add(TileIndex index, byte[] tile)
        {
            Task.Factory.StartNew(() =>
                {
                    lock (_writeLock)
                    {
                        if (Exists(index))
                        {
                            return;
                        }

                        string dir = GetDirectoryName(index);

                        try
                        {
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }

                            WriteToFile(tile, index);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("Unable to write tile cache", e);
                        }
                    }
                }
            )
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.WriteLine("Unable to write file to persistent cache {0}", t.Exception.Flatten());
            });
        }

        public byte[] Find(TileIndex index)
        {
            if (!Exists(index)) return null;
            try
            {
                return File.ReadAllBytes(GetFileName(index));
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unable to read tile cache {0}", e);
                return null;
            }
        }

        public void Remove(TileIndex index)
        {
            if (Exists(index))
            {
                File.Delete(GetFileName(index));
            }
        }

        public bool Exists(TileIndex index)
        {
            return File.Exists(GetFileName(index));
        }

        public string GetFileName(TileIndex index)
        {
            return Path.Combine(GetDirectoryName(index),
                string.Format(CultureInfo.InvariantCulture, "{0}.{1}", index.Row, "bin"));
        }

        private void WriteToFile(byte[] tile, TileIndex index)
        {
            var fileName = GetFileName(index);
            File.WriteAllBytes(fileName, tile);
        }

        private string GetDirectoryName(TileIndex index)
        {
            var level = index.Level;
            level = level.Replace(':', '_');
            return Path.Combine(_directory,
                level,
                index.Col.ToString(CultureInfo.InvariantCulture));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Data;
using System.Data.SQLite;
using log4net;

namespace ModernMinas.Update.Api
{
    public class CacheFile : IDisposable
    {
        private ILog _log;
        protected ILog Log { get { if (_log == null) _log = LogManager.GetLogger(this.GetType()); return _log; } }

        private SQLiteConnection _sqlite = new SQLiteConnection();

        public CacheFile(string file)
        {
            bool firstTimeUsage = false;

            if (!File.Exists(file))
            {
                firstTimeUsage = true;
                Log.InfoFormat("Creating sqlite database at {0}", file);
                SQLiteConnection.CreateFile(file);
            }

            Log.InfoFormat("Connecting to {0}", file);
            _sqlite.ConnectionString = string.Format("Data Source={0}", file);
            _sqlite.Open();

            if (firstTimeUsage)
            {
                Log.Debug("Creating table \"packages\" due to firstTimeUsage being true");
                _execNonQuery("CREATE TABLE IF NOT EXISTS packages (name VARCHAR(100) NOT NULL, version VARCHAR(100) NOT NULL, xml TEXT NOT NULL, PRIMARY KEY('name'));");
            }
        }

        ~CacheFile()
        {
            Dispose();
        }

        private SQLiteCommand _getSqliteCmd(string command)
        {
            return new SQLiteCommand(command, _sqlite);
        }

        private void _execNonQuery(string command)
        {
            Log.DebugFormat("_execNonQuery: {0}", command);
            _getSqliteCmd(command).ExecuteNonQuery();
        }

        private SQLiteDataReader _execReader(string command)
        {
            Log.DebugFormat("_execReader: {0}", command);
            return _getSqliteCmd(command).ExecuteReader();
        }

        public string GetCachedVersion(string package)
        {
            var version = string.Empty;
            using (var reader = _execReader(string.Format("SELECT version FROM packages WHERE name=\"{0}\"", package)))
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    version = reader.GetString(0);
                }
            }
            Log.DebugFormat("Cached version of {0} is {1}", package, version);
            return version;
        }

        public string GetCachedPackageXml(string package)
        {
            var xml = string.Empty;
            using (var reader = _execReader(string.Format("SELECT xml FROM packages WHERE name=\"{0}\"", package)))
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    xml = Encoding.UTF8.GetString(Convert.FromBase64String(reader.GetString(0)));
                }
            }
            return xml;
        }

        public void CachePackage(string package, string version, string xml)
        {
            xml = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
            Log.DebugFormat("Caching package {0} at version {1}", package, version);
            _execNonQuery(string.Format("INSERT OR REPLACE INTO packages (name, version, xml) VALUES ('{0}','{1}','{2}')", package, version, xml));
        }

        public IEnumerable<string> GetAllCachedPackageXml()
        {
            using (var reader = _execReader("SELECT xml FROM packages"))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        yield return Encoding.UTF8.GetString(Convert.FromBase64String(reader.GetString(0)));
                    }
                }
            }
        }
        public IEnumerable<string> GetAllCachedPackageIDs()
        {
            using (var reader = _execReader("SELECT name FROM packages"))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(0);
                    }
                }
            }
        }

        public void DeleteCache(string package)
        {
            Log.DebugFormat("Uncaching package {0}", package);
            _execNonQuery(string.Format("DELETE FROM packages WHERE name=\"{0}\"", package));
        }

        public bool IsSameVersion(string package, string newVersion)
        {
            return GetCachedVersion(package).Equals(newVersion);
        }

        public bool IsCached(string package)
        {
            return GetCachedVersion(package) != string.Empty;
        }

        bool finalized = false;
        public void Dispose()
        {
            Log.Debug("Closing connectiong/Disposing");
            if (!finalized)
            {
                _sqlite.Dispose();
                finalized = true;
            }
        }
    }
}

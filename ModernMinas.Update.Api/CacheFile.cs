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

namespace ModernMinas.Update.Api
{
    public class CacheFile : IDisposable
    {
        private SQLiteConnection _sqlite = new SQLiteConnection();

        public CacheFile(string file)
        {
            bool firstTimeUsage = false;

            if (!File.Exists(file))
            {
                firstTimeUsage = true;
                SQLiteConnection.CreateFile(file);
            }

            _sqlite.ConnectionString = string.Format("Data Source={0}", file);
            _sqlite.Open();

            if (firstTimeUsage)
                _execNonQuery("CREATE TABLE IF NOT EXISTS package_versions ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, name VARCHAR(100) NOT NULL, version VARCHAR(100) NOT NULL );");
        }

        ~CacheFile()
        {
            _sqlite.Close();
        }

        private SQLiteCommand _getSqliteCmd(string command)
        {
            return new SQLiteCommand(command, _sqlite);
        }

        private void _execNonQuery(string command)
        {
            _getSqliteCmd(command).ExecuteNonQuery();
        }

        private SQLiteDataReader _execReader(string command)
        {
            return _getSqliteCmd(command).ExecuteReader();
        }

        public string GetCachedVersion(string package)
        {
            var version = string.Empty;
            using (var reader = _execReader(string.Format("SELECT version FROM package_versions WHERE name=\"{0}\"", package)))
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    version = reader.GetString(0);
                }
            }
            return version;
        }

        public bool IsSameVersion(string package, string newVersion)
        {
            return GetCachedVersion(package).Equals(newVersion);
        }

        public bool IsCached(string package)
        {
            return GetCachedVersion(package) != string.Empty;
        }

        public void Dispose()
        {
            _sqlite.Dispose();
        }
    }
}

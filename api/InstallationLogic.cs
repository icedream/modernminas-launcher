using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModernMinas.Update.Api
{
    public class InstallationLogic
    {
        public InstallationLogic(string cacheFile, string repositoryUrl)
        {
            _cache = new CacheFile(cacheFile);
            _repository = new Uri(repositoryUrl);
        }

        public InstallationLogic(string cacheFile, Uri repositoryUrl)
        {
            _cache = new CacheFile(cacheFile);
            _repository = repositoryUrl;
        }

        public InstallationLogic(CacheFile cacheFile, Uri repositoryUrl)
        {
            _cache = cacheFile;
            _repository = repositoryUrl;
        }

        private CacheFile _cache;
        private Uri _repository;
        private Package[] _packages;
    }
}

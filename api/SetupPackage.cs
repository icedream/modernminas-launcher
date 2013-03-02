using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ModernMinas.Update.Api
{
    public class SetupPackage
    {
        private Repository _repository;
        private XmlNode _xml;

        public bool IsRequired { get { return _xml.Attributes["required"] == null ? true : bool.Parse(_xml.Attributes["required"].Value); } }
        public bool IsInstalled { get { return _repository.Cache.IsCached(ID); } }
        public string ID { get { return _xml.Attributes["id"].Value; } }

        public SetupPackage(XmlNode xml, Repository repository)
        {
            _repository = repository;
            _xml = xml;
        }

        public void Update()
        {
            _repository.UpdatePackage(ID);
        }

        public void Install()
        {
            _repository.InstallPackage(ID);
        }

        public void Uninstall()
        {
            _repository.UninstallPackage(ID);
        }
    }
}

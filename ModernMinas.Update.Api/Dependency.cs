using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ModernMinas.Update.Api
{
    public class Dependency
    {
        /// <summary>
        /// Dependency package name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Indicates if this package is not required to be installed
        /// </summary>
        public bool IsSoftDependency { get; private set; }

        private Dependency(string name, bool soft = false)
        {
            Name = name;
            IsSoftDependency = false;
        }

        internal static Dependency FromDependencyXmlNode(XmlNode dependencyXmlNode)
        {
            return new Dependency(
                dependencyXmlNode.Attributes["name"].Value,
                dependencyXmlNode.Attributes["required"] != null ? bool.Parse(dependencyXmlNode.Attributes["required"].Value) : false
            );
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SharpCompress;
using SharpCompress.Common;
using SharpCompress.Compressor;
using SharpCompress.Compressor.Deflate;
using SharpCompress.Compressor.Filters;
using SharpCompress.Compressor.BZip2;

namespace RepositoryGen
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlDocument singleXml = new XmlDocument();
            singleXml.Load(args.Any() ? args.First() : "data.xml");

            var installationNode = singleXml.SelectSingleNode("//setup");
            using (var s = File.Create("setup.dat"))
            using (var ds = new DeflateStream(s, CompressionMode.Compress, CompressionLevel.BestCompression, false))
            using (var sw = new StreamWriter(ds))
            {
                sw.Write(installationNode.OuterXml);
                sw.Flush();
            }

            Directory.CreateDirectory("packages");

            foreach (
                XmlNode node
                in singleXml.SelectNodes("//repository/package").OfType<XmlNode>()
            )
            {
                string packageClass = node.Attributes["class"] == null ? null : node.Attributes["class"].Value;

                if (!string.IsNullOrEmpty(packageClass))
                {
                    var templateNode = singleXml.SelectSingleNode("//templates/package[@class='" + packageClass + "']");
                    foreach (var childNode in templateNode.ChildNodes.OfType<XmlNode>())
                        node.AppendChild(childNode.CloneNode(true));
                }

                using (var s = File.Create(Path.Combine("packages", string.Format("{0}.dat", node.Attributes["id"].Value))))
                using (var ds = new DeflateStream(s, CompressionMode.Compress, CompressionLevel.BestCompression, false))
                using (var sw = new StreamWriter(ds))
                {
                    sw.Write(node.OuterXml);
                    sw.Flush();
                }
            }
        }
    }
}

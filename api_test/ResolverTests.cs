using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using ModernMinas.Update.Api;
using ModernMinas.Update.Api.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApiTest
{
    [TestClass]
    public class ResolverTests
    {
        public string ResolveTypes(ResolverBase resolver)
        {
            List<string> types = new List<string>();
            var methods = from m in resolver.GetType().GetMethods() select m;
            methods = from m in methods where m.Name.StartsWith("ResolveTo") select m;
            var methodnames = from m in methods where m.DeclaringType != m.GetBaseDefinition().DeclaringType select m.Name.Substring("ResolveTo".Length);
            return string.Join(",", methodnames);
        }

        [TestMethod]
        public void RegexResolve()
        {
            var resolver = new RegularExpressionResolver();
            var xmldoc = new XmlDocument();
            xmldoc.LoadXml("<resolver><regex>([VX]+)</regex><match>6</match></resolver>");
            resolver.Input = "XYYXXXXYXYYYYXXXXXYXYYYYXXYVXXYYXY";
            resolver.resolverNode = xmldoc.FirstChild;
            string result = resolver.ResolveToString();
            if (!result.Equals("VXX"))
                throw new Exception("Result is not VXX as expected, but " + result + ".");
        }

        [TestMethod]
        public void GetResolverByName_Regex()
        {
            var t = ResolverUtil.GetResolverByName("regex").GetType();
            if (t != typeof(RegularExpressionResolver))
                throw new Exception("Resolved to wrong type (" + t.Name + ")");
        }

        [TestMethod]
        public void GetResolverByName_Archive()
        {
            var t = ResolverUtil.GetResolverByName("archive").GetType();
            if (t != typeof(ArchiveResolver))
                throw new Exception("Resolved to wrong type (" + t.Name + ")");
        }

        [TestMethod]
        public void GetResolverByName_Mediafire()
        {
            var t = ResolverUtil.GetResolverByName("mediafire").GetType();
            if (t != typeof(MediafireResolver))
                throw new Exception("Resolved to wrong type (" + t.Name + ")");
        }

        [TestMethod]
        public void GetResolverByName_Download()
        {
            var t = ResolverUtil.GetResolverByName("download").GetType();
            if (t != typeof(DownloadResolver))
                throw new Exception("Resolved to wrong type (" + t.Name + ")");
        }

        [TestMethod]
        public void GetResolverChain()
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml("<source><resolver type=\"download\"><url>http://test.modernminas.de/</url></resolver><resolver type=\"archive\" /></source>");
            var chain = ResolverUtil.GetResolverChain(xmldoc.FirstChild);
            if (chain.Count != 2)
                throw new Exception("Count mismatch");
            if (chain.First().GetType() != typeof(DownloadResolver))
                throw new Exception("First resolver type mismatch");
            if (chain.Last().GetType() != typeof(ArchiveResolver))
                throw new Exception("Last resolver type mismatch");
        }

        /// <summary>
        /// Checks if a download-archive-chain would run without problems.
        /// </summary>
        [TestMethod]
        public void DownloadArchiveChainResolve()
        {
            var xmldoc = new XmlDocument();
            xmldoc.LoadXml(
                "<testresolvers>"
                    + "<resolver type=\"download\"><url>http://files.minecraftforge.net/minecraftforge/minecraftforge-universal-1.4.7-6.6.1.529.zip</url></resolver>"
                    + "<resolver type=\"archive\" />"
                + "</testresolvers>"
            );

            var resolvers = ResolverUtil.GetResolverChain(xmldoc.FirstChild);

            resolvers[0].Input = "http://files.minecraftforge.net/minecraftforge/minecraftforge-universal-1.4.7-6.6.1.529.zip";
            var stream = resolvers[0].ResolveToStream();

            resolvers[1].Input = stream;
            var archive = resolvers[1].ResolveToArchive();

            if (archive.GetFileEntries().Count() <= 0)
                throw new Exception("File entries validation failed");
            else
            {
                Console.WriteLine("Found files in archive:");
                foreach (string e in archive.GetFileEntries())
                    Console.WriteLine("  {0}", e);
            }
        }

        [TestMethod]
        public void VariableSetAgain()
        {
            VariableResolver.Clear();
            VariableResolver.Set("test", 1);
            VariableResolver.Set("test", 2);
        }

        [TestMethod]
        public void SelectorParsing()
        {
            var xmldoc = new XmlDocument();
            xmldoc.LoadXml("<selector id=\"awesomeselector\"><choice value=\"1\" default=\"false\">A</choice><choice value=\"2\" default=\"true\">B</choice><choice value=\"3\" description=\"test\">C</choice></selector>");
            Selector s = new Selector(xmldoc.FirstChild);
            if (s.AvailableOptions.Count != 3)
                throw new Exception("Options count mismatch");
            if (s.CurrentOption.Value != "2")
                throw new Exception("Default value mismatch");
        }
    }
}

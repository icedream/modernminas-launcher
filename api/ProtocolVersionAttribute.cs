using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModernMinas.Launcher.API
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class ProtocolVersionAttribute : Attribute
    {
        public ProtocolVersionAttribute(ulong version)
        {
            this.Version = version;
            //this.ClientVersion = Assembly.GetExecutingAssembly().GetName().Version;
        }

        public ulong Version
        { get; set; }

        /*
        public Version ClientVersion
        { get; set; }
         */
    }
}

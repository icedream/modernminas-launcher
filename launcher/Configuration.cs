using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Security;
using System.Windows;

namespace ModernMinas.Launcher
{
    [Serializable]
    public class Configuration
    {
        private static BinaryFormatter _formatterInstance;
        internal static BinaryFormatter FormatterInstance
        {
            get
            {
                if(_formatterInstance == null)
                {
                    _formatterInstance = new BinaryFormatter();
                    _formatterInstance.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full;
                    _formatterInstance.TypeFormat = System.Runtime.Serialization.Formatters.FormatterTypeStyle.TypesAlways;
                }
                return _formatterInstance;
            }
        }

        public Configuration()
        {
            try
            {
                JavaHomePath = JavaPath.GetJavaBinaryPath();
                MaximalRam = FileSize.FromGigabytes(1);
                //GamePath = Environment.CurrentDirectory;
                Username = string.Empty;
                Password = default(SecureString);
                AutoLogin = false;
            }
            catch(JavaNotFoundException)
            {
                MessageBox.Show("You need to have Java installed to run the client. Install the correct Java runtime from http://java.com/ for your machine (especially check for 64-bit or 32-bit) and try again.");    
            }
            catch (Exception error)
            {
                MessageBox.Show(string.Format("Could not load configuration: {0}", error.Message));
            }
        }

        public static Configuration LoadFromFile(string file)
        {
            FileInfo fi = new FileInfo(file);
            var fileS = fi.OpenRead();
            var obj = FormatterInstance.Deserialize(fileS);
            fileS.Close();
            fileS.Dispose();
            return (Configuration)obj;
        }

        public void SaveToFile(string file)
        {
            FileInfo fi = new FileInfo(file);
            var fileS = fi.OpenWrite();
            FormatterInstance.Serialize(fileS, this);
            fileS.Close();
            fileS.Dispose();
            //fi.Encrypt();
        }

        public FileSize MaximalRam { get; set; }
        public string JavaHomePath { get; set; }
        public string GamePath { get { return Environment.CurrentDirectory; } }
        public string Username { get; set; }
        [NonSerialized]
        public SecureString Password; // TODO: Safely save password encrypted
        public bool AutoLogin { get; set; } // TODO: Implement AutoLogin into launcher
    }
}

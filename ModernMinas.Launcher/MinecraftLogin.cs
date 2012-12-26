using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using RedCorona.Cryptography;

namespace ModernMinas.Launcher
{
    public class MinecraftLogin
    {
        public Uri LoginApiUri
        {
            get;
            set;
        }
        public string Username
        {
            get;
            set;
        }
        public SecureString Password
        {
            get;
            set;
        }
        public X509CertificateCollection Certificates
        {
            get;
            set;
        }
        public Exception LastError
        {
            get;
            set;
        }
        public int Timeout
        {
            get;
            set;
        }
        public string LatestVersion
        {
            get;
            set;
        }
        private string TicketID
        {
            get;
            set;
        }
        public string CaseCorrectUsername
        {
            get;
            set;
        }
        public string SessionId
        {
            get;
            set;
        }
        public MinecraftLogin()
        {
            this.Certificates = new X509CertificateCollection();
            this.Timeout = 3000;
            this.LoginApiUri = new Uri("https://login.minecraft.net");
        }
        public MinecraftLogin(Uri api)
            : this()
        {
            this.LoginApiUri = api;
        }
        public MinecraftLogin(string user, SecureString password)
            : this()
        {
            this.Username = user;
            this.Password = password;
        }
        public MinecraftLogin(string user, SecureString password, Uri api)
            : this()
        {
            this.Username = user;
            this.Password = password;
            this.LoginApiUri = api;
        }
        public bool Login(string user, SecureString password)
        {
            this.Username = user;
            this.Password = password;
            return this.Login();
        }
        public bool Login()
        {
            try
            {
                if (string.IsNullOrEmpty(this.Username))
                {
                    throw new ArgumentNullException("Username");
                }
                if (this.Password.Length.Equals(0))
                {
                    throw new ArgumentNullException("Password");
                }
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(this.LoginApiUri);
                httpWebRequest.ProtocolVersion = new Version(1, 0);
                httpWebRequest.Timeout = (httpWebRequest.ReadWriteTimeout = this.Timeout);
                httpWebRequest.UserAgent = "ModernMinas/" + Assembly.GetExecutingAssembly().GetName().Version;
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream());
                streamWriter.Write("?");
                streamWriter.Write("&user=" + Uri.EscapeDataString(this.Username));
                streamWriter.Write("&password=" + Uri.EscapeDataString(System.Runtime.InteropServices.Marshal.PtrToStringBSTR(System.Runtime.InteropServices.Marshal.SecureStringToBSTR(this.Password))));
                streamWriter.Write("&version=13");
                streamWriter.Flush();
                streamWriter.Close();
                HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();
                //char[] array = new char[4 * 1024];
                StreamReader streamReader = new StreamReader(response.GetResponseStream());
                //int l = streamReader.Read(array, 0, (int)response.ContentLength);
                string t = streamReader.ReadLine();
                response.Close();
                string[] array2 = t.Split(new char[]
				{
					':'
				});
                if (array2.Length < 4)
                {
                    throw new Exception(string.Join(":", array2));
                }
                this.LatestVersion = array2[0];
                this.TicketID = array2[1];
                this.CaseCorrectUsername = array2[2];
                this.SessionId = array2[3];
                return true;
            }
            catch (Exception lastError)
            {
                this.LastError = lastError;
            }
            return false;
        }
        public void SaveVersion(string where)
        {
            File.WriteAllText(where, this.LatestVersion);
        }
        public void SaveLogin(string where)
        {
            byte[] array = new byte[8];
            new Random(43287234).NextBytes(array);
            using (ICryptoTransform cryptoTransform = new PKCSKeyGenerator().Generate("passwordfile", array, 5, 1))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(this.Username + "\n");
                byte[] bytes2 = cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);
                File.WriteAllBytes(where, bytes2);
            }
        }
    }
}

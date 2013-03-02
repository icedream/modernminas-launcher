using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ModernMinas.Update.Api.Minecraft
{
    public class ServerConnection
    {
        public void Connect()
        {
            TcpClient client = new TcpClient(); // TCP layer
            client.Connect("minas.mc.modernminas.de", 25565); // Minas server => connect

            var stream = client.GetStream();
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream);
        }
    }
}

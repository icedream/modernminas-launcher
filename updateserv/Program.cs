using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ModernMinas.UpdateServer
{
    public class Program
    {
        public static ushort Port = 25555;

        public static TcpListener _tcp;
        public static TcpListener _tcp6;

        public static void Main(string[] args)
        {
            Console.WriteLine("Initializing listener ({0})...", "TCP4");
            _tcp = new TcpListener(IPAddress.Any, Port);
            _tcp.Start();

            Console.WriteLine("Initializing listener ({0})...", "TCP6");
            _tcp6 = new TcpListener(IPAddress.IPv6Any, Port);
            _tcp6.Start();

            // TCP handler
            Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Listener ready ({0})", "TCP4");
                while (true)
                {
                    while (!_tcp.Pending())
                        Thread.Sleep(50);
                    Console.WriteLine("Incoming client ({0})", "TCP4");
                    Task.Factory.StartNew(() => HandleClient(_tcp.AcceptSocket()));
                }
            });

            // TCP via IPv6 handler
            Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Listener ready ({0})", "TCP6");
                while (true)
                {
                    while (!_tcp6.Pending())
                        Thread.Sleep(50);
                    Console.WriteLine("Incoming client ({0})", "TCP6");
                    Task.Factory.StartNew(() => HandleClient(_tcp6.AcceptSocket()));
                }
            });

            Thread.Sleep(Timeout.Infinite);
        }

        public static void HandleClient(Socket s)
        {
            Console.WriteLine("[{0}] Connected.", s.RemoteEndPoint);
            try
            {
                ClientConnection cl = new ClientConnection(s);
                cl.EndPoint = (IPEndPoint)s.RemoteEndPoint;
                cl.Handle();
            }
            catch (Exception e)
            {
                Console.WriteLine("[{0}] HandleClient outer code exception: {1}", s.RemoteEndPoint, e);
            }
        }
    }
}

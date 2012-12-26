using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using ModernMinas.Launcher.API;

namespace ModernMinas.UpdateServer
{
    public class ClientConnection : Connection
    {
        public ClientConnection(NetworkStream s)
            : base(s)
        { }
        public ClientConnection(Socket s)
            : this(new NetworkStream(s))
        { }

        public IPEndPoint EndPoint { get; set; }

        public void Handle()
        {
            try
            {
                Console.WriteLine("[{0}] Handler started.", EndPoint);

                CheckProtocolVersion();

                do
                {
                    var cmd = ReadCommand();
                    Console.WriteLine("[{0}] Received {1}.", EndPoint, cmd);
                    switch (cmd)
                    {
                        case Command.KeepAlive:
                            Console.WriteLine("[{0}] Sending back OK status.", EndPoint);
                            SendCommand(Command.Status_OK);
                            break;
                        case Command.Quitting:
                            Console.WriteLine("[{0}] Client is quitting, disposing connection.", EndPoint);
                            ForceDisconnect();
                            return;
                        case Command.FileList:
                            {
                                SendCommand(Command.Status_OK);
                                Console.WriteLine("[{0}] Sending back directory info...", EndPoint);
                                Write(DirectoryInfo.FromIODirectoryInfo(new System.IO.DirectoryInfo("files")));
                                Console.WriteLine("[{0}] Done", EndPoint);
                            }
                            break;
                        case Command.GetFile:
                            {
                                Console.WriteLine("[{0}] Reading file info...", EndPoint);
                                //var file = Read<FileInfo>();
                                var file_io = new System.IO.FileInfo(System.IO.Path.Combine("files", ReadString().Replace('/', System.IO.Path.DirectorySeparatorChar)));
                                Console.WriteLine("[{0}] File is {1}", EndPoint, file_io.Name);
                                SendCommand(Command.Status_OK);
                                Console.WriteLine("[{0}] Sending file...", EndPoint);
                                WriteFile(file_io);
                                Console.WriteLine("[{0}] Send finished.", EndPoint);
                            }
                            break;
                        default:
                            {
                                Console.WriteLine("[{0}] Warning: Unexpected command.", EndPoint);
                                Error("CL_UNEXPECTED_INFO: Client sent unexpected information.");
                                Quit();
                            }
                            return;
                    }
                } while (true);
            }
            catch (Exception n)
            {
                try
                {
                    Console.WriteLine("[{0}] Warning: Internal server error: {1}", EndPoint, n.ToString());
                    Error("SV_INTERNAL: Internal server error.");
                    Quit();
                }
                catch
                {
                    try
                    {
                        this.ForceDisconnect();
                    }
                    catch
                    {
                        { } // ignore all errors
                    }
                }
            }
        }

        protected void CheckProtocolVersion()
        {
        checkVersion:
            Console.WriteLine("[{0}] Awaiting protocol version", EndPoint);
            var cmd = ReadCommand();
            switch (cmd)
            {
                case Command.KeepAlive: // First command on UDP connections, ignored when via TCP
                    Console.WriteLine("[{0}] Skipping keep-alive.", EndPoint);
                    goto checkVersion;
                case Command.ProtocolVersion:
                    ulong protocolVersion = ReadUInt64();
                    Console.WriteLine("[{0}] Got protocol version {1} from client.", EndPoint, protocolVersion);
                    if (!protocolVersion.Equals(this.ProtocolVersion))
                    {
                        Console.WriteLine("[{0}] Client is outdated (SERVER={2} not equal to CLIENT={1}).", EndPoint, protocolVersion, this.ProtocolVersion);
                        Error("CL_OUTDATED: Client is outdated, update from http://modernminas.tk/ and retry.");
                        Quit();
                        return;
                    }
                    else
                        SendCommand(Command.Status_OK);
                    break;
                default:
                    Console.WriteLine("[{0}] Client sent unexpected {1}.", EndPoint, cmd);
                    Error("CL_UNEXPECTED_INFO: Client sent unexpected information.");
                    Quit();
                    return;
            }
        }

        public void Error(string text)
        {
            Console.WriteLine("[{0}] Sending error response with content \"{1}\".", EndPoint, text);
            SendCommand(Command.Status_Error);
            WriteString(text);
        }

        public void Quit()
        {
            Console.WriteLine("[{0}] Quitting connection.", EndPoint);
                        
            //SendCommand(Command.Status_QuittingConnection);
            ForceDisconnect();
        }
    }
}

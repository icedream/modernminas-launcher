using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using ProtoBuf;
using ProtoBuf.Compiler;
using ProtoBuf.Meta;

namespace ModernMinas.Launcher.API
{
    [ProtocolVersion(1)]
    public class Connection
    {
        protected NetworkStream _nstream;
        protected BufferedStream _bstream;

        public NetworkStream BaseStream
        { get { return _nstream; } }
        public ulong ProtocolVersion
        { get { return ((ProtocolVersionAttribute)this.GetType().GetCustomAttributes(typeof(ProtocolVersionAttribute), true).First()).Version; } }

        public Connection(Socket socket)
            : this(new NetworkStream(socket))
        { }
        public Connection(NetworkStream stream)
        {
            this._nstream = stream;
            this._bstream = new BufferedStream(_nstream);
        }

        protected void WriteBytes(params Byte[] content)
        {
            _bstream.Write(content, 0, content.Length);
            _bstream.Flush();
        }
        protected void WriteByte(Byte content)
        {
            _bstream.WriteByte(content);
            _bstream.Flush();
        }
        protected void WriteString(String s)
        {
            WriteInt32(s.Length);
        }
        protected void WriteChar(Char v)
        {
            WriteBytes(BitConverter.GetBytes(v));
        }
        protected void WriteSingle(Single v)
        {
            WriteBytes(BitConverter.GetBytes(v));
        }
        protected void WriteDouble(Double v)
        {
            WriteBytes(BitConverter.GetBytes(v));
        }
        protected void WriteInt16(Int16 v)
        {
            WriteBytes(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(v)));
        }
        protected void WriteInt32(Int32 v)
        {
            WriteBytes(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(v)));
        }
        protected void WriteInt64(Int64 v)
        {
            WriteBytes(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(v)));
        }
        protected void WriteUInt16(UInt16 v)
        {
            WriteBytes(BitConverter.GetBytes((UInt16)IPAddress.HostToNetworkOrder((Int16)v)));
        }
        protected void WriteUInt32(UInt32 v)
        {
            WriteBytes(BitConverter.GetBytes((UInt32)IPAddress.HostToNetworkOrder((Int32)v)));
        }
        protected void WriteUInt64(UInt64 v)
        {
            WriteBytes(BitConverter.GetBytes((UInt64)IPAddress.HostToNetworkOrder((Int64)v)));
        }
        protected void Write<T>(T v)
        {
            Serializer.SerializeWithLengthPrefix(_bstream, v, PrefixStyle.Base128);
            _bstream.Flush();
        }
        protected void WriteFile(FileInfo fi, string prefix = null)
        {
            WriteFile(fi.GetIOFileInfo(prefix));
        }
        protected System.IO.FileInfo GetCacheFile(System.IO.FileInfo original)
        {
            Directory.CreateDirectory("cache");
            string cacheID = GetCacheID(original);
            return new System.IO.FileInfo(System.IO.Path.Combine("cache", cacheID + ".lzma"));
        }
        static string GetCacheID(System.IO.FileInfo f)
        {
            string text = "cache_" + f.FullName;
            var SHA1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();

            byte[] arrayData;
            byte[] arrayResult;
            string result = null;
            string temp = null;

            arrayData = Encoding.ASCII.GetBytes(text);
            arrayResult = SHA1.ComputeHash(arrayData);
            for (int i = 0; i < arrayResult.Length; i++)
            {
                temp = Convert.ToString(arrayResult[i], 16);
                if (temp.Length == 1)
                    temp = "0" + temp;
                result += temp;
            }
            return result;
        }
        protected void WriteFile(System.IO.FileInfo fi)
        {
            var lzma = GetCacheFile(fi);

            if (!lzma.Exists || lzma.LastWriteTimeUtc < fi.LastWriteTimeUtc)
            {
                FileStream _rstream;
                lzma.Directory.Create();
                using(var _cstream
                    = new SharpCompress.Compressor.LZMA.LzmaStream(
                        new SharpCompress.Compressor.LZMA.LzmaEncoderProperties(false),
                        false,
                        _rstream = lzma.OpenWrite()
                    ))
                using (var _fstream = fi.OpenRead())
                {
                    Console.WriteLine("Caching " + fi.Name + "...");
                    _rstream.Write(_cstream.Properties, 0, 5);
                    _rstream.Flush();
                    _fstream.CopyTo(_cstream);
                    Console.WriteLine("Finishing caching " + fi.Name + "...");
                    _fstream.Close();
                    _fstream.Dispose();
                    _cstream.Flush();
                    _cstream.Close();
                    _cstream.Dispose();
                    _rstream.Close();
                    _rstream.Dispose();
                }

                lzma.Refresh();

                if (!lzma.Exists)
                    throw new Exception("Caching error");

                Console.WriteLine("=> Cached {0} kB into {1} kB",
                    Math.Floor((decimal)fi.Length / 1024),
                    Math.Floor((decimal)lzma.Length / 1024)
                );
            }

            var _cfstream = lzma.OpenRead();
            _cfstream.CopyTo(_bstream, 4096);
            _bstream.Flush();
            _cfstream.Close();
            _cfstream.Dispose();
        }

        protected Byte ReadByte()
        {
            return (byte)_bstream.ReadByte();
        }
        protected Byte[] ReadBytes(int expectedLength)
        {
            byte[] buffer = new byte[expectedLength];
            _bstream.Read(buffer, 0, expectedLength);
            return buffer;
        }
        protected Byte[] ReadBytes()
        {
            var list = new List<ArraySegment<byte>>();
            while (_nstream.DataAvailable)
            {
                var bufferCurrent = new byte[32 * 1024];
                var buffer = new ArraySegment<byte>(bufferCurrent, 0, _bstream.Read(bufferCurrent, 0, bufferCurrent.Length));
                list.Add(buffer);
            }
            var a = from b in list.Select(c => c.Array) select b;
            var d = new byte[0];
            foreach (var e in a)
                d = d.Concat(e).ToArray<byte>();
            return d;
        }
        protected String ReadString()
        {
            var length = ReadInt16();
            var data = ReadBytes(length * Encoding.BigEndianUnicode.GetMaxByteCount(1));
            return Encoding.BigEndianUnicode.GetString(data);
        }
        protected Char ReadChar()
        {
            return BitConverter.ToChar(ReadBytes(sizeof(Char)), 0);
        }
        protected Single ReadSingle()
        {
            return BitConverter.ToSingle(ReadBytes(sizeof(Single)), 0);
        }
        protected Double ReadDouble()
        {
            return BitConverter.ToDouble(ReadBytes(sizeof(Double)), 0);
        }
        protected UInt16 ReadUInt16()
        {
            return (UInt16)IPAddress.NetworkToHostOrder((Int16)BitConverter.ToUInt16(ReadBytes(sizeof(UInt16)), 0));
        }
        protected UInt32 ReadUInt32()
        {
            return (UInt32)IPAddress.NetworkToHostOrder((Int32)BitConverter.ToUInt32(ReadBytes(sizeof(UInt32)), 0));
        }
        protected UInt64 ReadUInt64()
        {
            return (UInt64)IPAddress.NetworkToHostOrder((Int64)BitConverter.ToUInt64(ReadBytes(sizeof(UInt64)), 0));
        }
        protected Int16 ReadInt16()
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadBytes(sizeof(Int16)), 0));
        }
        protected Int32 ReadInt32()
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ReadBytes(sizeof(Int32)), 0));
        }
        protected Int64 ReadInt64()
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(ReadBytes(sizeof(Int64)), 0));
        }
        protected T Read<T>()
        {
            return Serializer.DeserializeWithLengthPrefix<T>(_bstream, PrefixStyle.Base128);
        }
        protected void ReadFile(Stream targetStream, long length)
        {
            var _cstream
                = new SharpCompress.Compressor.LZMA.LzmaStream(
                    ReadBytes(5),
                    _bstream
                  );

            byte[] buffer = new byte[32 * 1024];
            int buffRead = buffer.Length;

            do
            {
                targetStream.Write(buffer, 0, 
                    _cstream.Read(buffer, 0, (int)Math.Min(
                        // Use rest of needed bytes if smaller than buffer length
                        length != -1
                            ? length - _cstream.Position
                            : buffer.Length,

                        // Otherwise use buffer length
                        buffer.Length
                    ))
                );
                Console.Write("  {1}%/{0} kB\r", Math.Floor((decimal)_cstream.Position / 1024), Math.Floor(100 * ((double)_cstream.Position / length)));
            }
            while (_cstream.Position < length);

            targetStream.Flush();

            _cstream.Close();
            _cstream.Dispose();

            while (_nstream.DataAvailable)
                Console.WriteLine("[!] Trashing {0} bytes.", _bstream.Read(buffer, 0, buffer.Length));
        }

        protected void SendCommand(Command cmd)
        {
            WriteByte((byte)cmd);
        }
        protected Command ReadCommand()
        {
            return (Command)ReadByte();
        }

        protected void ForceDisconnect()
        {
            _bstream.Close();
            _bstream.Dispose();
            _nstream.Dispose();
        }

        public void SendProtocolVersion()
        {
            SendCommand(Command.ProtocolVersion);
            WriteUInt64(this.ProtocolVersion);
            if (ReadCommand() == Command.Status_Error)
                ThrowError();
        }

        public API.DirectoryInfo RequestFileList()
        {
            SendCommand(Command.FileList);
            if (ReadCommand() == Command.Status_Error)
                ThrowError();
            return Read<API.DirectoryInfo>();
        }

        public void RequestFile(API.FileInfo fileInfo, Stream targetStream)
        {
            if (fileInfo == null)
                throw new ArgumentNullException("fileInfo");
            if (targetStream == null)
                throw new ArgumentNullException("targetStream");
            SendCommand(Command.GetFile);
            Write(fileInfo);
            if (ReadCommand() == Command.Status_Error)
                ThrowError();
            ReadFile(targetStream, fileInfo.Length);
        }

        public void Disconnect()
        {
            SendCommand(Command.Quitting);
            ForceDisconnect();
        }

        protected void ThrowError()
        {
            string error = ReadString();
            throw new Exception(error);
        }
    }
}

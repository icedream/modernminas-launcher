using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using ProtoBuf.Meta;

namespace ModernMinas.Launcher.API
{
    [ProtoContract(UseProtoMembersOnly=true)]
    public class FileInfo
    {
        // TODO: Update protocol - DirectoryInfo should not be transmitted back to server, it just wastes upload bandwidth
        [ProtoMember(4, AsReference = true)]
        public DirectoryInfo Directory { get; set; }

        [ProtoMember(3, IsRequired = true)]
        public DateTime LastWriteTimeUtc { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public long Length { get; set; }

        [ProtoMember(1, IsRequired = true)]
        public string Name { get; set; }

        public System.IO.FileInfo GetIOFileInfo()
        {
            return new System.IO.FileInfo(GetAbsolutePath());
        }
        public System.IO.FileInfo GetIOFileInfo(string prefix)
        {
            return new System.IO.FileInfo(GetAbsolutePath(prefix));
        }

        public string GetRelativePath()
        {
            return GetAbsolutePath(".");
        }

        public string GetAbsolutePath()
        {
            return GetAbsolutePath("");
        }

        public string GetAbsolutePath(string prefix)
        {
            return System.IO.Path.Combine(Directory.GetAbsolutePath(prefix), this.Name);
        }

        public static FileInfo FromIOFileInfo(System.IO.FileInfo fi, DirectoryInfo di)
        {
            var fo = new FileInfo();
            fo.LastWriteTimeUtc = fi.LastWriteTimeUtc;
            fo.Length = fi.Length;
            fo.Name = fi.Name;
            fo.Directory = di;
            return fo;
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class DirectoryInfo
    {
        [ProtoMember(4, IsRequired = false)]
        public List<DirectoryInfo> Directories { get; set; }

        [ProtoMember(5, IsRequired = false)]
        public List<FileInfo> Files { get; set; }

        [ProtoMember(3, AsReference = true)]
        public DirectoryInfo Parent { get; set; }

        [ProtoMember(1, IsRequired = true)]
        public string Name { get; set; }

        [ProtoMember(2, IsRequired = true)]
        public DateTime LastWriteTimeUtc { get; set; }

        public System.IO.DirectoryInfo GetIODirectoryInfo()
        {
            return new System.IO.DirectoryInfo(this.GetAbsolutePath());
        }
        public System.IO.DirectoryInfo GetIODirectoryInfo(string prefix)
        {
            return new System.IO.DirectoryInfo(this.GetAbsolutePath(prefix));
        }

        public string GetRelativePath()
        {
            return GetAbsolutePath(".");
        }

        public string GetAbsolutePath()
        {
            return GetAbsolutePath("");
        }

        public string GetAbsolutePath(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                prefix = System.IO.Path.DirectorySeparatorChar.ToString(); // Root

            Stack<string> DirectoryNames = new Stack<string>();
            DirectoryInfo t = this;
            while (t != null)
            {
                if(t.Parent != null)
                    DirectoryNames.Push(t.Name);
                t = t.Parent;
            }

            StringBuilder sb = new StringBuilder(System.IO.Path.GetFullPath(prefix).TrimEnd(System.IO.Path.DirectorySeparatorChar));
            while (DirectoryNames.Any())
                sb.AppendFormat("{0}{1}", System.IO.Path.DirectorySeparatorChar, DirectoryNames.Pop());

            return sb.ToString();
        }

        public DirectoryInfo()
        {
            Directories = new List<DirectoryInfo>();
            Files = new List<FileInfo>();
        }

        public static DirectoryInfo FromIODirectoryInfo(System.IO.DirectoryInfo di, DirectoryInfo dparent = null)
        {
            DirectoryInfo dio = new DirectoryInfo();
            dio.Name = di.Name;
            dio.Parent = dparent;
            dio.LastWriteTimeUtc = di.LastWriteTimeUtc;
            foreach (var f in di.EnumerateFiles())
                dio.Files.Add(FileInfo.FromIOFileInfo(f, dio));
            foreach (var d in di.EnumerateDirectories())
                dio.Directories.Add(DirectoryInfo.FromIODirectoryInfo(d, dio));
            return dio;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpCompress;
using SharpCompress.Archive;
using SharpCompress.Archive.SevenZip;
using SharpCompress.Archive.Rar;
using SharpCompress.Archive.GZip;
using SharpCompress.Archive.Tar;
using SharpCompress.Archive.Zip;
using SharpCompress.Common;
using SharpCompress.Common.Zip;
using System.IO;

namespace ModernMinas.Update.Api.Archives
{
    public class Archive : ArchiveBase
    {
        IArchive _arch;

        public Archive(Stream fs, string password = null)
        {
            if (ZipArchive.IsZipFile(fs))
            {
                fs.Seek(0, SeekOrigin.Begin);
                _arch = (IArchive)ZipArchive.Open(fs, password);
            }
            else if (RarArchive.IsRarFile(fs))
            {
                fs.Seek(0, SeekOrigin.Begin);
                _arch = (IArchive)RarArchive.Open(fs);
            }
            else if (TarArchive.IsTarFile(fs))
            {
                fs.Seek(0, SeekOrigin.Begin);
                _arch = (IArchive)TarArchive.Open(fs);
            }
            else if (SevenZipArchive.IsSevenZipFile(fs))
            {
                fs.Seek(0, SeekOrigin.Begin);
                _arch = (IArchive)SevenZipArchive.Open(fs);
            }
            else if (GZipArchive.IsGZipFile(fs))
            {
                fs.Seek(0, SeekOrigin.Begin);
                _arch = (IArchive)GZipArchive.Open(fs);
            }
        }

        private IArchiveEntry GetEntry(string file)
        {
            var fs = from entry in _arch.Entries
                     where entry.FilePath.Replace('\\', '/').EndsWith(file, StringComparison.OrdinalIgnoreCase)
                     select entry;
            if (fs.Count() > 1)
                throw new InvalidOperationException();
            else if (!fs.Any())
                throw new InvalidOperationException();
            else
                return fs.First();
        }

        public override void ExtractFile(string file, string targetFilePath)
        {
            using (var streamToWriteTo = File.Open(targetFilePath, FileMode.OpenOrCreate))
                GetEntry(file).WriteTo(streamToWriteTo);
        }

        public override void ExtractAllFiles(string targetFolderPath)
        {
            var r = _arch.ExtractAllEntries();
            do
            {
                using (var streamToWriteTo = File.Open(Path.Combine(targetFolderPath, r.Entry.FilePath.Replace('/', Path.DirectorySeparatorChar)), FileMode.OpenOrCreate))
                    r.WriteEntryTo(streamToWriteTo);
            }
            while (r.MoveToNextEntry());
        }

        public override string[] GetFileEntries()
        {
            return (from entry in _arch.Entries select entry.FilePath.TrimStart('/', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).ToArray();
        }

        public override Stream OpenFile(string file)
        {
            return GetEntry(file).OpenEntryStream();
        }
    }
}

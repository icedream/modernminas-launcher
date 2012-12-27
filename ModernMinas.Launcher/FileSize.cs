using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModernMinas.Launcher
{
    /// <summary>
    /// Represents a file size.
    /// </summary>
    [Serializable]
    public class FileSize
    {
        public static readonly FileSize kB = 1024;
        public static readonly FileSize MB = kB * 1024;
        public static readonly FileSize GB = MB * 1024;
        public static readonly FileSize TB = GB * 1024;
        public static readonly FileSize PB = TB * 1024;
        public static readonly FileSize EB = PB * 1024;
        //public static readonly FileSize ZB = EB * 1024;
        //public static readonly FileSize YB = ZB * 1024;

        public override int GetHashCode()
        {
            return bytes.GetHashCode();
        }

        public static FileSize FromKilobytes(long a)
        {
            return kB * a;
        }
        public static FileSize FromMegabytes(long a)
        {
            return MB * a;
        }
        public static FileSize FromGigabytes(long a)
        {
            return GB * a;
        }
        public static FileSize FromTerabytes(long a)
        {
            return TB * a;
        }
        public double ToKilobytes()
        {
            return bytes / kB;
        }
        public double ToMegabytes()
        {
            return bytes / MB;
        }
        public double ToGigabytes()
        {
            return bytes / GB;
        }
        public double ToTerabytes()
        {
            return bytes / TB;
        }

        long bytes = 0;
        static readonly string[] units = { "B", "kB", "MB", "GB", "TB", "PB", "EB"/*, "ZB", "YB"*/ };
        public static implicit operator long(FileSize o)
        {
            return o.bytes;
        }
        public static implicit operator FileSize(long o)
        {
            return new FileSize(o);
        }
        public static FileSize operator +(FileSize a, FileSize b)
        {
            a.bytes += b.bytes;
            return a;
        }
        public static FileSize operator +(FileSize a, long b)
        {
            a.bytes += b;
            return a;
        }
        public static FileSize operator -(FileSize a, FileSize b)
        {
            a.bytes -= b.bytes;
            return a;
        }
        public static FileSize operator -(FileSize a, long b)
        {
            a.bytes -= b;
            return a;
        }
        public static FileSize operator %(FileSize a, FileSize b)
        {
            a.bytes %= b.bytes;
            return a;
        }
        public static FileSize operator %(FileSize a, long b)
        {
            a.bytes %= b;
            return a;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is FileSize))
                throw new InvalidOperationException("object is of type " + obj.GetType().Name);
            return ((FileSize)obj).bytes.Equals(this.bytes);
        }
        public override string ToString()
        {
            return this.ToString(2);
        }
        public string ToString(int precision = 2)
        {
            int i = 0;
            double a = bytes;
            while (a > 1024)
            {
                a /= 1024; i++;
            }
            return string.Format("{0:#." + new string('#', precision) + "} {1}", a, units[i]);
        }
        public FileSize(long bytes)
        {
            this.bytes = bytes;
        }
    }
}

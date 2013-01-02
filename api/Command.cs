using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModernMinas.Launcher.API
{
    public enum Command : byte
    {
        Status_OK = 0x00,
        Status_Error = 0x01,
        Status_FileNotFound = 0x0a,

        FileList = 0x10,
        GetFile = 0x11,

        File = 0x20,
        Directory = 0x21,
        EndOfDirectory = 0x2a,

        Quitting = 0xef,

        ProtocolVersion = 0xf0,

        KeepAlive = 0xff
    }
}

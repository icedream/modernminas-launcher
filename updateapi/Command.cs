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
        //Status_DirectoryNotFound = 0x0b,
        //Status_QuittingConnection = 0x0f,

        FileList = 0x10,
        GetFile = 0x11,

        Quitting = 0xef,

        ProtocolVersion = 0xf0,

        KeepAlive = 0xff
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModernMinas.Update.Api
{
    public enum StatusType
    {
        Ready = 0,
        Downloading = 1,
        Installing = 2,
        Uninstalling = 3,
        Finished = 4,
        Parsing = 5,
        CheckingUpdates = 6,
        Finalizing = 7,
        CheckingDependencies = 8,
        InstallingDependencies = 9
    }
}

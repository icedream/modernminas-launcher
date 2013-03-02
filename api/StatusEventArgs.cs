using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModernMinas.Update.Api
{
    public class StatusEventArgs : EventArgs
    {
        public Package Package { get; set; }
        public StatusType Status { get; set; }
        public float Progress { get; set; }

        public StatusEventArgs(Package package, StatusType status, float progress)
        {
            Package = package;
            Status = status;
            Progress = progress;
        }
    }
}

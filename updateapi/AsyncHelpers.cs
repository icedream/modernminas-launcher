using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModernMinas.Launcher.API
{
    public class AsyncStatus<T>
    {
        internal AsyncStatus()
        {
            IsError = false;
        }

        public bool IsError { get; internal set; }
        public Exception Exception { get; internal set; }
        public T Status { get; internal set; }
    }

    public class RequestFileAsyncStatus : AsyncStatus<RequestFileStatus>
    {
        public ReadFileAsyncStatus DownloadStatus { get; internal set; }
    }

    public enum RequestFileStatus
    {
        RequestingFile = 0,
        DownloadingFile,

        Finished = 0xff
    }

    public class ReadFileAsyncStatus : AsyncStatus<ReadFileStatus>
    {
        public long BytesRead { get; internal set; }
        public long BytesTotal { get; internal set; }
        public double BytesPerSecond { get; internal set; }
        
        internal ReadFileAsyncStatus() : base()
        {
            BytesRead = BytesTotal = 0;
            Status = ReadFileStatus.Ready;
        }
    }

    public enum ReadFileStatus : byte
    {
        Ready = 0,
        Preparing,
        Downloading,

        Finished = 0xff
    }
}

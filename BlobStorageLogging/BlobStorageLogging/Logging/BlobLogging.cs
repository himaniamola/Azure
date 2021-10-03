using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace BlobStorageLogging
{
    public enum LogType
    {
        ERROR,
        INFO,
        DEBUG,
        WARN,
        FATAL
    }
    public interface ILog
    {
        void Error(string msg);
        void Error(string msg, Exception ex);
        void ErrorFormat(string msg, params object[] args);
        void WriteLine(string msg);
        void Info(string msg);
        void Info(string msg, Exception ex);
        void InfoFormat(string msg, params object[] args);
        void InfoFormat(CultureInfo cultureInfo, string msg, params object[] args);
        void Debug(string msg);
        void DebugFormat(string msg, params object[] args);
        void DebugFormat(CultureInfo cultureInfo, string msg, params object[] args);
        void Warn(string msg);
        void Warn(string msg, Exception ex);
        void WarnFormat(string msg, params object[] args);
        void Fatal(string msg);
        void Fatal(string msg, Exception ex);

    }
    public class BlobLogging : ILog, IDisposable
    {
        #region Variables
        private bool disposedValue;
        private readonly string fileNm = "";
        private readonly string callingCls = "";
        private readonly string offset = "";
        private readonly string dateTimeFmt = "yyy-MM-dd HH:mm:ss";
        private readonly int level = 4;
        private readonly CloudBlobContainer blobCont = null;
        #endregion

        #region Constructor
        public BlobLogging(string logFile)
        {
            this.fileNm = ConfigurationManager.AppSettings[logFile] ?? "DefaultLog.log";
            var frame = new StackTrace().GetFrame(2);
            var method = frame.GetMethod();
            this.callingCls = method.ReflectedType.FullName;
            this.offset = Convert.ToString(frame.GetILOffset());

            try
            {
                CloudStorageAccount stAcc = CloudStorageAccount.Parse(Microsoft.WindowsAzure.CloudConfigurationManager.GetSetting("StorageConnectionString"));
                CloudBlobClient blobClnt = stAcc.CreateCloudBlobClient();
                this.blobCont = blobClnt.GetContainerReference(ConfigurationManager.AppSettings["LogsFolder"]);
                this.blobCont.CreateIfNotExistsAsync();
            }
            catch (StorageException ex)
            {
                Console.WriteLine($"Exception throw while container creation {ex}");
            }
        }
        #endregion

        #region "Public Methods"
        public void Debug(string msg)
        {
#if DEBUG
            WriteLog(msg, LogType.DEBUG);
#endif
        }

        public void DebugFormat(string msg, params object[] args)
        {
#if DEBUG
            WriteLog(string.Format(msg, args), LogType.DEBUG);
#endif
        }

        public void DebugFormat(CultureInfo cultureInfo, string msg, params object[] args)
        {
#if DEBUG
            WriteLog(string.Format(cultureInfo, msg, args), LogType.DEBUG);
#endif
        }

        public void Error(string msg)
        {
            WriteLog(msg, LogType.ERROR);
        }

        public void Error(string msg, Exception ex)
        {
            WriteLog($"{msg} \n {ex}", LogType.ERROR);
        }

        public void ErrorFormat(string msg, params object[] args)
        {
            WriteLog(string.Format(msg, args), LogType.ERROR);
        }

        public void Fatal(string msg)
        {
            WriteLog(msg, LogType.FATAL);
        }

        public void Fatal(string msg, Exception ex)
        {
            WriteLog($"{msg} \n {ex}", LogType.FATAL);
        }

        public void Info(string msg)
        {
            WriteLog(msg, LogType.INFO);
        }

        public void Info(string msg, Exception ex)
        {
            WriteLog($"{msg} \n {ex}", LogType.INFO);
        }

        public void InfoFormat(string msg, params object[] args)
        {
            WriteLog(string.Format(msg, args), LogType.INFO);
        }

        public void InfoFormat(CultureInfo cultureInfo, string msg, params object[] args)
        {
            WriteLog(string.Format(cultureInfo, msg, args), LogType.INFO);
        }

        public void Warn(string msg)
        {
            WriteLog(msg, LogType.WARN);
        }

        public void Warn(string msg, Exception ex)
        {
            WriteLog($"{msg} \n {ex}", LogType.WARN);
        }

        public void WarnFormat(string msg, params object[] args)
        {
            WriteLog(string.Format(msg, args), LogType.WARN);
        }

        public void WriteLine(string msg)
        {
            throw new NotImplementedException();
        }
        #endregion
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~BlobLogging()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        private void WriteLog(string msg, LogType lvl = LogType.INFO)
        {
            CloudAppendBlob appBlb = GetBlob(this.fileNm);

        }

        private CloudAppendBlob GetBlob(string fileNm)
        {
            string rollApp = ConfigurationManager.AppSettings["IsRollApp"];
            bool isLogRollApp = !string.IsNullOrWhiteSpace(rollApp) ? Convert.ToBoolean(rollApp) : false;
            CloudAppendBlob cab = this.blobCont.GetAppendBlobReference(fileNm);
            if (isLogRollApp)
            {
                var directry = this.blobCont.GetDirectoryReference("");
                string[] fp = fileNm.Split('.');
                string rollAppMaxFileCnt = ConfigurationManager.AppSettings["RollingAppenderMaxFileCount"];
                int maxRollAppFileCnt = !string.IsNullOrWhiteSpace(rollAppMaxFileCnt) ? Convert.ToInt32(rollAppMaxFileCnt) : 1;
                var existingLogFl = directry.ListBlobs().Select(f => f.StorageUri?.PrimaryUri?.Segments.LastOrDefault()).Where(f => Regex.IsMatch(f, $"^{fp[0]}[1-{maxRollAppFileCnt}]?.{fp[1]}$", RegexOptions.IgnoreCase));
                int existingNoOfFl = existingLogFl.Count();
                string curFNm =fileNm;
                if ((existingNoOfFl > 0))
                {
                    curFNm = existingLogFl.LastOrDefault();
                    cab = this.blobCont.GetAppendBlobReference(curFNm);
                }

                if (cab.Exists())
                {
                    cab.FetchAttributes();
                    var sizeMb = cab.Properties.Length / (1024 * 1024);
                    string maxSizeMb = ConfigurationManager.AppSettings["maxSizeMb"];
                    int maxFileSize = !string.IsNullOrWhiteSpace(maxSizeMb) ? Convert.ToInt32(maxSizeMb) : 1;
                    if (sizeMb >= maxFileSize)
                    {
                        if(existingNoOfFl < maxRollAppFileCnt)
                        {
                            cab = this.blobCont.GetAppendBlobReference(curFNm);
                            cab.CreateOrReplace(AccessCondition.GenerateIfNotExistsCondition());
                        }
                        else
                        {

                        }
                    }
                }
                else
                {
                    cab.CreateOrReplace();
                }
            }
            else 
            {
                if (!cab.Exists()) 
                {
                    cab.CreateOrReplace(AccessCondition.GenerateIfNotExistsCondition());
                }
                else 
                {
                    cab.FetchAttributes();
                    var sizeMb = cab.Properties.Length / (1024 * 1024);
                    string maxSizeMb = ConfigurationManager.AppSettings["MaxSizeMb"];
                    int maxFileSize = !string.IsNullOrWhiteSpace(maxSizeMb) ? Convert.ToInt32(maxSizeMb) : 1;
                    if(sizeMb >= maxFileSize)
                    {
                        cab.CreateOrReplace();
                    }
                }
            }

            return cab;
        }
    }

    public sealed class LogMgr
    {
        private static readonly Lazy<LogMgr> instance = new Lazy<LogMgr>(() => new LogMgr());
        private static Dictionary<string, ILog> logDict = new Dictionary<string, ILog>();
        private LogMgr()
        {

        }

        public static LogMgr Instance
        {
            get
            {
                return instance.Value;
            }
        }

        public ILog GetLogger(object type)
        {
            return GetLogger(type.ToString());
        }

        public ILog GetLogger(string message)
        {
            ILog _log = null;
            if (!logDict.ContainsKey(message))
            {
                logDict.Add(message, new BlobLogging(message));
            }

            _log = logDict[message];
            return _log;
        }
    }
}

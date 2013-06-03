using System;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.ExtraInformation;

namespace Logging
{
    class Program
    {
        static void Main()
        {
            var loggingConfig = new LoggingConfiguration();
            var customizedWriter = new LogWriter(loggingConfig);

            var logWriterFactory = new LogWriterFactory();
            var defaultWriter = logWriterFactory.Create();

            var logEntry = new LogEntry
                {
                    EventId = 100,
                    Priority = 2,
                    Message = "Informational message",
                    Categories = { "Trace", "UI Events" }
                };

            new ManagedSecurityContextInformationProvider().PopulateDictionary(logEntry.ExtendedProperties);
            new DebugInformationProvider().PopulateDictionary(logEntry.ExtendedProperties);
            new ComPlusInformationProvider().PopulateDictionary(logEntry.ExtendedProperties);
            new UnmanagedSecurityContextInformationProvider().PopulateDictionary(logEntry.ExtendedProperties);

            defaultWriter.Write(logEntry);

            var traceManager = new TraceManager(defaultWriter);
            using (traceManager.StartTrace("Trace"))
            {
                defaultWriter.Write("Operation 1");
            }

            using (traceManager.StartTrace("UI Events", Guid.NewGuid()))
            {
                defaultWriter.Write("Operation 2", "Trace");
            }

            using (traceManager.StartTrace("Trace"))
            {
                using (traceManager.StartTrace("UI Events"))
                {
                    defaultWriter.Write("Operation 3");
                }
            }
        }
    }
}

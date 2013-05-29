using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling.Configuration;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace ExceptionHandling
{
    class Program
    {
        enum MethodType
        {
            One,
            Two,
            Three,
            Four
        }

        static readonly LogWriterFactory LogWriterFactory = new LogWriterFactory();

        static void Main()
        {
            var logWriter = LogWriterFactory.Create();
            Logger.SetLogWriter(logWriter);

            var exceptionManager = CreateExceptionManager(MethodType.One);
            ExceptionPolicy.SetExceptionManager(exceptionManager);

            // Wrap into Exception and log
            exceptionManager.Process(() =>
                {
                    string a = null;
                    var length = a.Length;
                }, "Policy");

            try
            {
                throw new NullReferenceException("throw NullReferenceException");
            }
            catch (Exception e)
            {
                Exception newException;
                var rethrow = ExceptionPolicy.HandleException(e, "Policy", out newException);
                if (!rethrow) 
                    return;

                if (newException != null)
                    throw newException;
            }
        }

        private static ExceptionManager CreateExceptionManager(MethodType type)
        {
            switch (type)
            {
                case MethodType.One:
                    {
                        var section =
                            (ExceptionHandlingSettings)
                            ConfigurationManager.GetSection(ExceptionHandlingSettings.SectionName);
                        return section.BuildExceptionManager();
                    }
                case MethodType.Two:
                    {
                        var exceptionPolicy1 = new List<ExceptionPolicyEntry>
                            {
                                new ExceptionPolicyEntry(typeof (InvalidCastException),
                                                         PostHandlingAction.NotifyRethrow,
                                                         new IExceptionHandler[]
                                                             {
                                                                 new LoggingExceptionHandler("General",
                                                                                             9002, TraceEventType.Error,
                                                                                             "Salary Calculations Service",
                                                                                             5,
                                                                                             typeof (TextExceptionFormatter),
                                                                                             LogWriterFactory.Create())
                                                             }),
                                new ExceptionPolicyEntry(typeof (Exception),
                                                         PostHandlingAction.NotifyRethrow,
                                                         new IExceptionHandler[]
                                                             {
                                                                 new ReplaceHandler(
                                                             "Application error will be ignored and processing will continue.",
                                                             typeof (Exception))
                                                             })
                            };

                        var policies = new List<ExceptionPolicyDefinition>
                            {
                                new ExceptionPolicyDefinition("Policy", exceptionPolicy1)
                            };

                        return new ExceptionManager(policies);
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

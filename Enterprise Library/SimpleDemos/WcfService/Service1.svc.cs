using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling.Configuration;
using Microsoft.Practices.EnterpriseLibrary.ExceptionHandling.WCF;

namespace WcfService
{
    [ExceptionShielding("Policy")]
    public class Service1 : IService1
    {
        static Service1()
        {
            var section = (ExceptionHandlingSettings)ConfigurationManager.GetSection(ExceptionHandlingSettings.SectionName);
            var exceptionManager = section.BuildExceptionManager();
            ExceptionPolicy.SetExceptionManager(exceptionManager);
        }

        public string GetData(int value)
        {

            var e = new Exception("I broke!");
            e.Data.Add("Key", "value");
            throw e;
            return string.Format("You entered: {0}", value);
        }
    }
}

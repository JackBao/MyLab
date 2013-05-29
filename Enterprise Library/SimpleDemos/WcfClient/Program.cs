using System;
using System.ServiceModel;
using WcfClient.ServiceReference1;

namespace WcfClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var service = new Service1Client();
                var output = service.GetData(5);
                Console.WriteLine(output);
                Console.ReadLine();
            }
            catch (FaultException<WeGoofedFault> exception)
            {
                Console.WriteLine("Caught exception!");
                Console.WriteLine(exception.Detail.Message);
            }
        }
    }
}

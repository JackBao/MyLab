using System;
using System.Collections;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace WcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "Service1" in both code and config file together.
    [ServiceContract]
    public interface IService1
    {
        [OperationContract]
        [FaultContract(typeof(WeGoofedFault))]
        string GetData(int value);
    }

    [DataContract]
    public class WeGoofedFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public Guid ErrorId { get; set; }

        [DataMember]
        public IDictionary Data { get; set; }
    }

}

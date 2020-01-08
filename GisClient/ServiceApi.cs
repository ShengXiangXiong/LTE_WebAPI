using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Transport;

namespace GisClient
{
    public class ServiceApi
    {
        //private static ServiceApi _instance = new ServiceApi();   //静态私有成员变量，存储唯一实例
        private string url;
        private int port;
        public TProtocol protocol;
        public OpreateGisLayer.Client client;
        public TTransport transport;
        public static ThreadLocal<ServiceApi> gisApi = new ThreadLocal<ServiceApi>();


        public ServiceApi(string url = "localhost", int port = 8800)
        {
            this.url = url;
            this.port = port;
            this.transport = new TSocket(url, port);
            this.protocol = new TBinaryProtocol(transport);
            this.transport.Open();
            this.client = new OpreateGisLayer.Client(protocol);
        }

        //public static ServiceApi GetInstance()
        //{
        //    return _instance;
        //}

        public void setConn(string url = "localhost", int port = 8800)
        {
            this.url = url;
            this.port = port;
        }

        public TMultiplexedProtocol getServiceByMuliti(string serviceName)
        {
            return new TMultiplexedProtocol(protocol, serviceName);
        }

        public static OpreateGisLayer.Client getGisLayerService(string url = "localhost", int port = 8800)
        {
            if (!gisApi.Value.transport.IsOpen)
            {
                gisApi.Value.transport.Open();
            }
            return gisApi.Value.client;
        }
        public static void CloseConn()
        {
            if (gisApi.Value.transport.IsOpen)
            {
                gisApi.Value.transport.Close();
            }
        }

    }
}

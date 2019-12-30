using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Transport;

namespace GisClient
{
    public class ServiceApi
    {
        private static ServiceApi _instance = null;   //静态私有成员变量，存储唯一实例
        private string url;
        private int port;
        public static TProtocol protocol;
        private static OpreateGisLayer.Client client;
        private static TTransport transport;


        private ServiceApi(string url = "localhost", int port = 8800)
        {
            this.url = url;
            this.port = port;
            TTransport transport = new TSocket(url, port,0);
            protocol = new TBinaryProtocol(transport);
            transport.Open();
            client = new OpreateGisLayer.Client(protocol);
        }

        public static ServiceApi GetInstance(string url = "localhost", int port = 8800)
        {
            if (_instance == null)
            {
                _instance = new ServiceApi(url, port);
            }
            return _instance;
        }

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
            try
            {
                if (client == null)
                {
                    transport = new TSocket(url, port,1000*60*60*60);
                    protocol = new TBinaryProtocol(transport);
                    transport.Open();
                    client = new OpreateGisLayer.Client(protocol);
                }
                if (!transport.IsOpen)
                {
                    transport.Open();
                }
                return client;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public static void CloseConn()
        {
            transport.Close();
        }

    }
}

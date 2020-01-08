using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GisClient
{
    class Program
    {
        private static Object gisLock = new Object();
        static void Main(string[] args)
        {
            //创建ThreadTest类的一个实例
            Thread1 test=new Thread1();
            Thread2 t2 = new Thread2();
            //调用test实例的MyThread方法
            Thread thread1 = new Thread(new ThreadStart(test.run));
            Thread thread2 = new Thread(new ThreadStart(t2.run));
            //启动线程
            thread1.Start();
            thread2.Start();
            
            //ServiceApi.client.setLoadInfo(1, "1");
            //ServiceApi.client.setLoadInfo(1, "2");
        }
    }
    class Thread1
    {
        //private static Object gisLock = new Object();
        public void run()
        {
            //Monitor.Enter(gisLock);
            for (int i = 0; i < 3; i++)
            {
                ServiceApi rpc = new ServiceApi();
                //if (!rpc.transport.IsOpen)
                //{
                //    rpc.transport.Open();
                //}
                rpc.client.setLoadInfo(1, "1");
                ////rpc.CloseConn();
                //Console.WriteLine("Thread1_____"+i);
            }
            Thread.Sleep(2000);
        }
    }
    class Thread2
    {
        public void run()
        {
            for (int i = 0; i < 3; i++)
            {
                ServiceApi rpc = new ServiceApi();
                //if (!rpc.transport.IsOpen)
                //{
                //    rpc.transport.Open();
                //}
                rpc.client.setLoadInfo(1, "2");
                ////rpc.CloseConn();
                //Console.WriteLine("Thread2____"+i);
            }
            
        }
    }
}

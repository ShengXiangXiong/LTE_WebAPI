using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace LTE.WebAPI.Utils
{
    public class TimerHelper
    {
        private static Stopwatch stopwatch = new Stopwatch();
        public static double hours;
        public static double minutes;
        public static double seconds;
        public static double milliseconds;
        public static void start()
        {
            stopwatch.Reset();
            stopwatch.Start(); //  开始监视代码运行时间
        }
        public static void end()
        {
            stopwatch.Stop(); //  停止监视
            TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
            double hours = timespan.TotalHours; // 总小时
            double minutes = timespan.TotalMinutes;  // 总分钟
            double seconds = timespan.TotalSeconds;  //  总秒数
            double milliseconds = timespan.TotalMilliseconds;  //总毫秒数
        }
    }
}
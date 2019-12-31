using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace LTE.Win32Lib
{
    public class IPC
    {
        public const int WM_USER_NOTIFY = 0x0400;
        public const int WM_POST_NOTIFY = WM_USER_NOTIFY + 11;
        public const int WM_POST_PROGRESS = WM_USER_NOTIFY + 12;
        public const int WM_POST_TOTALRAY = WM_USER_NOTIFY + 13;
        //子进程通知父进程程序已启动
        public const int WM_POST_READY = WM_USER_NOTIFY + 14;
        //父进程通知子进程开始计算
        public const int WM_POST_STARTCALC = WM_USER_NOTIFY + 15;
        //子进程通知父进程计算结束
        public const int WM_POST_CALCDONE = WM_USER_NOTIFY + 16;
        //计算结束
        public const int WM_POST_DONE = WM_USER_NOTIFY + 17;
        //子进程通知父进程出界射线记录结束  2019.5.22
        public const int WM_POST_ReRayDONE = WM_USER_NOTIFY + 18;
        //父进程异常退出前先发送kill信号给子进程
        public const int WM_POST_Kill = WM_USER_NOTIFY + 19;


        public struct COPYDATASTRUCT
        {
            public int dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;
        }

        [DllImport("User32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        //同步方式传递消息到消息队列
        [DllImport("User32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);
        //异步方式传递消息到消息队列
        [DllImport("User32.dll", EntryPoint = "PostMessage", CharSet = CharSet.Unicode)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, int lParam);

    }
}

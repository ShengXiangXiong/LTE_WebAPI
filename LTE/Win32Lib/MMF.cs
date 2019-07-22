using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace LTE.Win32Lib
{
    public class MMF
    {
        [Flags]
        public enum FileMapAccess : uint
        {
            FileMapCopy = 0x0001,
            FileMapWrite = 0x0002,
            FileMapRead = 0x0004,
            FileMapAllAccess = 0x001f,
            fileMapExecute = 0x0020,
        }

        [Flags]
        public enum FileMapProtection : uint
        {
            PageReadonly = 0x02,
            PageReadWrite = 0x04,
            PageWriteCopy = 0x08,
            PageExecuteRead = 0x20,
            PageExecuteReadWrite = 0x40,
            SectionCommit = 0x8000000,
            SectionImage = 0x1000000,
            SectionNoCache = 0x10000000,
            SectionReserve = 0x4000000,
        }

        //内存映射(非文件映射)
        public const uint INVALID_HANDLE_VALUE = 0xFFFFFFFF;

        [DllImport("Kernel32.dll", EntryPoint = "CreateFileMapping", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFileMapping(uint hFile, IntPtr lpFileMappingAttributes, FileMapProtection flProject, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr OpenFileMapping(FileMapAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMapping, FileMapAccess dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool UnmapViewOfFile(IntPtr pvBaseAddress);

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32", EntryPoint = "GetLastError")]
        public static extern int GetLastError();

        /// <summary>
        /// 生成文件映射，失败返回IntPtr.Zero
        /// </summary>
        /// <param name="shareName"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static IntPtr CreateOrOpenMMF(string shareName, int size)
        {
            IntPtr handle = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, FileMapProtection.PageReadWrite, 0, (uint)size, shareName);
            if (handle == IntPtr.Zero)
            {
                throw new Exception("create or open memory mapping file failed");
            }
            return handle;
        }
    }
}

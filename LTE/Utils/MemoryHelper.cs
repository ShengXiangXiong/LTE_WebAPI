using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
namespace LTE.Utils
{
    /// <summary>
    /// 文件大小单位，包括从B至PB共六个单位。
    /// </summary>
    public enum FileSizeUnit
    {
        B,
        KB,
        MB,
        GB,
        TB,
        PB
    }
    public class MemoryHelper
    {
        /// <summary>
        /// 获得已使用的物理内存的大小，单位 (Byte)，如果获取失败，返回 -1.
        /// </summary>
        /// <returns></returns>
        public static long GetTotalPhysicalMemory()
        {
            long capacity = 0;
            try
            {
                foreach (ManagementObject mo1 in new ManagementClass("Win32_PhysicalMemory").GetInstances())
                    capacity += long.Parse(mo1.Properties["Capacity"].Value.ToString());
            }
            catch (Exception ex)
            {
                capacity = -1;
                Console.WriteLine(ex.Message);
            }
            return capacity;
        }


        /// <summary>
        /// 获得已使用的物理内存的大小，单位 (Byte)，如果获取失败，返回 -1.
        /// </summary>
        /// <returns></returns>
        public static long GetAvailablePhysicalMemory()
        {
            long capacity = 0;
            try
            {
                foreach (ManagementObject mo1 in new ManagementClass("Win32_PerfFormattedData_PerfOS_Memory").GetInstances())
                    capacity += long.Parse(mo1.Properties["AvailableBytes"].Value.ToString());
            }
            catch (Exception ex)
            {
                capacity = -1;
                Console.WriteLine(ex.Message);
            }
            return capacity;
        }

        /// <summary>
        /// 根据指定的文件大小单位，对输入的文件大小（字节表示）进行转换。
        /// </summary>
        /// <param name="filesize">文件文件大小，单位为字节。</param>
        /// <param name="targetUnit">目标单位。</param>
        /// <returns></returns>
        public static double ToFileFormat(long filesize, FileSizeUnit targetUnit = FileSizeUnit.MB)
        {
            double size = -1;
            switch (targetUnit)
            {
                case FileSizeUnit.KB: size = filesize / 1024.0; break;
                case FileSizeUnit.MB: size = filesize / 1024.0 / 1024; break;
                case FileSizeUnit.GB: size = filesize / 1024.0 / 1024 / 1024; break;
                case FileSizeUnit.TB: size = filesize / 1024.0 / 1024 / 1024 / 1024; break;
                case FileSizeUnit.PB: size = filesize / 1024.0 / 1024 / 1024 / 1024 / 1024; break;
                default: size = filesize; break;
            }
            return size;
        }
    }
}

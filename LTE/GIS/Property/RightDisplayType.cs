using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTE.GIS
{
    /// <summary>
    /// 右边控件显示类别
    /// </summary>
    public class RightDisplayType
    {

        static DisplayType type = DisplayType.Base;

        /// <summary>
        /// 属性显示的类别
        /// </summary>
        public static DisplayType Type
        {
            get { return type; }
            set { type = value; }
        }


    }

    /// <summary>
    /// 枚举类别
    /// </summary>
    public enum DisplayType
    {
        /// <summary>
        /// 基本信息
        /// </summary>
        Base,
        /// <summary>
        /// GSM干扰信息
        /// </summary>
        GSMDisturb,
        /// <summary>
        /// TD干扰信息
        /// </summary>
        TDDisturb

    }
}

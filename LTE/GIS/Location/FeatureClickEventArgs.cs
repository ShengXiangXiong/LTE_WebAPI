using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTE.GIS
{
    /// <summary>
    /// 要素点击事件参数
    /// </summary>
    public class FeatureClickEventArgs : EventArgs
    {
        private string m_LayerName;

        /// <summary>
        /// 图层名称
        /// </summary>
        public string LayerName
        {
            get { return m_LayerName; }
        }

        private object m_Info;

        /// <summary>
        /// 所需要的信息
        /// </summary>
        public object Info
        {
            get { return m_Info; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pLayerName">图层名称</param>
        /// <param name="pCellName">小区名称</param>
        public FeatureClickEventArgs(string pLayerName, object pInfo)
        {
            this.m_LayerName = pLayerName;
            this.m_Info = pInfo;


        }
    }
}

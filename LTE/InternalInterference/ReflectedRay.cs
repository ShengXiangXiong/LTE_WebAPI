using System;
using LTE.Geometric;

namespace LTE.InternalInterference
{
    public class ReflectedRay
    {
        private NodeInfo nodeInfo;

        public ReflectedRay(NodeInfo nodeInfo)
        {
            this.nodeInfo = nodeInfo;
        }

        // 返回反射线方向
        public Vector3D ConstructReflectedRay(ref Vector3D inDir)
        {
            // inDir 为归一化后的入射线方向向量

            // this.nodeInfo.Normal 为归一化后的反射面法向

            // 反射线方向
            Vector3D refDir = new Vector3D();
            double tmp = 2 * inDir.dotProduct(this.nodeInfo.Normal);
            refDir.XComponent = inDir.XComponent - tmp * this.nodeInfo.Normal.XComponent;
            refDir.YComponent = inDir.YComponent - tmp * this.nodeInfo.Normal.YComponent;
            refDir.ZComponent = inDir.ZComponent - tmp * this.nodeInfo.Normal.ZComponent;
            return refDir;  // 已经是归一化后的
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTE.Geometric;
using LTE.GIS;

namespace LTE.InternalInterference.Grid
{
    /// <summary>
    /// 求空间线段经过的立体网格(建筑物)，采用3D-DDA算法
    /// </summary>
    public class LineCrossGrid3D
    {
        /// <summary>
        /// 能否计算
        /// </summary>
        private bool isCalc;
        /// <summary>
        /// 直线要经过的当前空间网格
        /// </summary>
        private Grid3D cur;
        /// <summary>
        /// 当前空间网格距离线段原点的距离
        /// </summary>
        private double distance;
        /// <summary>
        /// 空间直线
        /// </summary>
        private Line line;
        /// <summary>
        /// 空间网格xoy边长，单位mi
        /// </summary>
        private double gridlength;
        /// <summary>
        /// 空间网格高度，单位米
        /// </summary>
        private double vgridsize;
        /// <summary>
        /// 空间网格平面相对每层的高度
        /// </summary>
        private double gbaseheight;
        /// <summary>
        /// 空间网格xy的最大值
        /// </summary>
        private int maxgxid;
        private int maxgyid;
        /// <summary>
        /// xoy上的步长
        /// </summary>
        private int stepx;
        private int stepy;
        private int stepz;
        /// <summary>
        /// 跨越x栅格在线段上的距离
        /// </summary>
        private double dx;
        private double dy;
        private double dz;
        /// <summary>
        /// 从线段起始点，到达下一栅格所要经过的距离
        /// </summary>
        private double tx;
        private double ty;
        private double tz;
        //防止double溢出
        private static double maxlength = double.MaxValue / 10.0;

        private bool Init()
        {
            this.isCalc = false;
            this.cur = new Grid3D();
            this.distance = 0.0;
            this.line = new Line();
            this.gridlength = GridHelper.getInstance().getGGridSize();
            this.vgridsize = GridHelper.getInstance().getGHeight();
            this.gbaseheight = GridHelper.getInstance().getGBaseHeight();
            return !(this.gridlength < 0 || this.vgridsize < 0 || this.gbaseheight < 0);
        }

        /// <summary>
        /// 构造函数，参数为大地坐标
        /// </summary>
        /// <param name="start">大地坐标</param>
        /// <param name="end">大地坐标</param>
        public LineCrossGrid3D(Point start, Point end)
        {
            this.isCalc = this.Init();
            if (!this.isCalc) return;
            GridHelper.getInstance().getMaxGGridXY(ref this.maxgxid, ref this.maxgyid);

            this.isCalc = !PointComparer.Equals1(start, end) && GridHelper.getInstance().PointXYZToGrid3D(start, ref this.cur);
            if (!this.isCalc) return;

            Vector3D dir = Vector3D.constructVector(start, end);
            this.line.setLine(start, dir);

            this.stepx = (this.line.paraEqua.X < 0 ? -1 : 1);
            this.stepy = (this.line.paraEqua.Y < 0 ? -1 : 1);
            this.stepz = (this.line.paraEqua.Z < 0 ? -1 : 1);

            Point ingrid = new Point();
            this.isCalc = GridHelper.getInstance().PointXYZInGrid3D(start, ref ingrid);
            if (!this.isCalc) return;

            if (Math.Round(this.line.paraEqua.X, 3) == 0)
            {
                this.tx = this.dx = maxlength;
            }
            else if (this.line.paraEqua.X > 0)
            {
                this.dx = this.gridlength / this.line.paraEqua.X;
                this.tx = (this.gridlength - ingrid.X) / this.line.paraEqua.X;
            }
            else
            {
                this.dx = this.gridlength / (0 - this.line.paraEqua.X);
                this.tx = ingrid.X / (0 - this.line.paraEqua.X);
            }

            if (Math.Round(this.line.paraEqua.Y, 3) == 0)
            {
                this.ty = this.dy = maxlength;
            }
            else if (this.line.paraEqua.Y > 0)
            {
                this.dy = this.gridlength / this.line.paraEqua.Y;
                this.ty = (this.gridlength - ingrid.Y) / this.line.paraEqua.Y;
            }
            else
            {
                this.dy = this.gridlength / (0 - this.line.paraEqua.Y);
                this.ty = ingrid.Y / (0 - this.line.paraEqua.Y);
            }

            if (Math.Round(this.line.paraEqua.Z, 3) == 0)
            {
                this.tz = this.dz = maxlength;//因为tz太大，dz随便定义
            }
            else if (this.line.paraEqua.Z > 0)
            {
                this.dz = this.vgridsize / this.line.paraEqua.Z;
                this.tz = (this.vgridsize - ingrid.Z) / this.line.paraEqua.Z;
            }
            else if (this.line.paraEqua.Z < 0)
            {
                this.dz = this.vgridsize / (0 - this.line.paraEqua.Z);
                this.tz = ingrid.Z / (0 - this.line.paraEqua.Z);
            }
        }

        /// <summary>
        /// 获取空间直线经过的下一个空间建筑物网格
        /// </summary>
        /// <param name="d">空间网格距离线段起点的距离</param>
        /// <returns></returns>
        public Grid3D getNextCrossGrid3D(ref double d)
        {
            if (!this.isCalc || this.cur == null) return null;
            Grid3D ret = new Grid3D(this.cur);
            d = this.distance;
            double ttx = Math.Round(this.tx, 3);
            double tty = Math.Round(this.ty, 3);
            double ttz = Math.Round(this.tz, 3);
            bool flag = false;//是否计算完成
            //先经过x方向栅格
            if (!flag && ttx < tty && ttx < ttz)
            {
                this.cur.gxid += this.stepx;
                if (this.cur.gxid < 0 || this.cur.gxid > this.maxgxid)
                {
                    this.isCalc = false;
                    this.cur.gxid -= this.stepx;//还原更改，继续计算
                    return ret;
                }
                else
                {
                    this.distance = this.tx;
                    this.tx += this.dx;
                    flag = true;
                }
            }
            //先经过y方向栅格
            if (!flag && tty < ttx && tty < ttz)
            {
                this.cur.gyid += this.stepy;
                if (this.cur.gyid < 0 || this.cur.gyid > this.maxgyid)
                {
                    this.isCalc = false;
                    this.cur.gyid -= this.stepy;
                    return ret;
                }
                else
                {
                    this.distance = this.ty;
                    this.ty += this.dy;
                    flag = true;
                }
            }
            //先经过z方向栅格
            if (!flag && ttz < ttx && ttz < tty)
            {
                this.cur.gzid += this.stepz;
                if (this.cur.gzid < 1)
                {
                    this.isCalc = false;
                    this.cur.gzid -= this.stepz;
                    return ret;
                }
                else
                {
                    this.distance = this.tz;
                    this.tz += this.dz;
                    flag = true;
                }
            }
            //下一个点是线段终点
            if (!flag && (ttx < maxlength && tty < maxlength && ttz < maxlength))
            {
                if (!this.cur.Equals(ret))
                {
                    this.tx = this.ty = this.tz = maxlength + 10;
                }
            }
            if (!flag)
            {
                this.isCalc = false;
            }

            return ret;
        }
    }
}

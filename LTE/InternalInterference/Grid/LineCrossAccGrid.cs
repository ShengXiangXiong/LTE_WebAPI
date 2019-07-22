using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LTE.Geometric;
using LTE.GIS;

namespace LTE.InternalInterference.Grid
{
    /// <summary>
    /// 空间线段
    /// </summary>
    public class Line
    {
        public Point start;
        /// <summary>
        /// 直线的参数方程
        /// x = x0 + mt;
        /// y = y0 + nt;
        /// z = z0 + pt;
        /// paraEqua.X = m;
        /// paraEqua.Y = n;
        /// paraEqua.Z = p;
        /// </summary>
        public Point paraEqua;

        public Line()
        {
            this.start = new Point();
            this.paraEqua = new Point();
        }
        /// <summary>
        /// 设置线段，参数为大地坐标
        /// </summary>
        /// <param name="start">大地坐标</param>
        /// <param name="dir">方向</param>
        public void setLine(Point start, Vector3D dir)
        {
            this.start = start;
            this.paraEqua.X = dir.XComponent;
            this.paraEqua.Y = dir.YComponent;
            this.paraEqua.Z = dir.ZComponent;
        }
    }

    /// <summary>
    /// 求空间线段经过的加速栅格(三层)，采用3D-DDA算法
    /// </summary>
    public class DDA3D
    {
        /// <summary>
        /// 能否计算
        /// </summary>
        private bool isCalc;
        /// <summary>
        /// 当前加速栅格
        /// </summary>
        private Grid3D cur;
        /// <summary>
        /// 空间直线
        /// </summary>
        private Line line;
        /// <summary>
        /// 加速栅格xoy边长，单位mi
        /// </summary>
        private double gridlength;
        /// <summary>
        /// 加速栅格z轴长度，单位米
        /// </summary>
        private double vgridsize;
        private int maxgxid;
        private int maxgyid;
        private int mingxid;
        private int mingyid;
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
            this.isCalc = true;
            this.cur = new Grid3D();
            this.line = new Line();
            this.gridlength = GridHelper.getInstance().getAGridSize();
            this.vgridsize = GridHelper.getInstance().getAGridVSize();
            return !(this.gridlength < 0 || this.vgridsize < 0);
        }

        /// <summary>
        /// 构造函数，参数为大地坐标
        /// </summary>
        /// <param name="start">大地坐标</param>
        /// <param name="dir">方向</param>
        public DDA3D(Point start, Vector3D dir)
        {
            Point tstart = start.clone();

            this.isCalc = this.Init();
            if (!this.isCalc) return;
            GridHelper.getInstance().getMaxAccGridXY(ref this.maxgxid, ref this.maxgyid);
            GridHelper.getInstance().getMinAccGridXY(ref this.mingxid, ref this.mingyid);

            this.isCalc = GridHelper.getInstance().PointXYZToAccGrid(tstart, ref this.cur);
            if (!this.isCalc) return;

            this.line.setLine(tstart, dir);

            this.stepx = (this.line.paraEqua.X < 0 ? -1 : 1);
            this.stepy = (this.line.paraEqua.Y < 0 ? -1 : 1);
            this.stepz = (this.line.paraEqua.Z < 0 ? -1 : 1);

            Point ingrid = new Point();
            this.isCalc = GridHelper.getInstance().PointXYZInAccGrid(tstart, ref ingrid);
            if (!this.isCalc) return;

            //double.MaxValue / 10.0 防止计算时溢出
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
                this.tz = this.dz = maxlength;//因为tz很大，所以dz随便定义
            }
            else
            {
                if (this.cur.gzid == 3 && this.line.paraEqua.Z > 0)
                {
                    this.tz = this.dz = maxlength;
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
        }

        /// <summary>
        /// 获取空间直线经过的下一个加速栅格
        /// </summary>
        /// <returns></returns>
        public Grid3D getNextCrossAccGrid()
        {
            if (!this.isCalc || this.cur == null) return null;
            Grid3D ret = new Grid3D(this.cur);
            double ttx = Math.Round(this.tx, 3);
            double tty = Math.Round(this.ty, 3);
            double ttz = Math.Round(this.tz, 3);
            bool flag = false;//是否计算完成
            //先经过x方向栅格
            if (!flag && ttx <= tty && ttx <= ttz)
            {
                this.cur.gxid += this.stepx;
                if (this.cur.gxid < this.mingxid || this.cur.gxid > this.maxgxid)
                {
                    this.isCalc = false;
                    this.cur.gxid -= this.stepx;//还原更改，继续计算
                    return ret;
                }
                else
                {
                    this.tx += this.dx;
                    flag = true;
                }
            }
            //先经过y方向栅格
            if (!flag && tty <= ttx && tty <= ttz)
            {
                this.cur.gyid += this.stepy;
                if (this.cur.gyid < this.mingyid || this.cur.gyid > this.maxgyid)
                {
                    this.isCalc = false;
                    this.cur.gyid -= this.stepy;
                    return ret;
                }
                else
                {
                    this.ty += this.dy;
                    flag = true;
                }
            }
            //先经过z方向栅格
            if (!flag && ttz <= ttx && ttz <= tty)
            {
                this.cur.gzid += this.stepz;
                if (this.cur.gzid < 1 || this.cur.gzid > 3)
                {
                    this.isCalc = false;
                    this.cur.gzid -= this.stepz;
                    return ret;
                }
                else
                {
                    this.tz += this.dz;
                    flag = true;
                }
            }
            return ret;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTE.Geometric
{
    enum LineType
    {
        LINE_SEGMENT,  // 线段形式
        LINE_PTNORM,   // 点法线形式
        LINE_PARAM     // 直线的参数形式
    }

    class Line2D
    {
        public Vector2D C, S,  // p(t) = S + Ct  直线的参数形式
                        A, B,  // A--B  线段形式
                        P, N;  // P为直线上的点，N为直线的法线，点法线形式

        // a为线段起点，b为线段终点
        public Line2D(Vector2D a, Vector2D b)
        {
            setVectors(a, b);
        }

        // a为线段起点，b为线段终点
        public void setVectors(Vector2D a, Vector2D b)
        {
            A = a;
            B = b;

            C = new Vector2D(b.x - a.x, b.y - a.y);
            C.unit();
            S = a;

            P = a;
            N = new Vector2D(-C.y, C.x);  // 假定多边形顶点按照顺时针排列
        }

        // 返回直线上的点，直线的法向量
        public void getPtNorm(out Vector2D a, out Vector2D b)
        {
            a = P;
            b = N;
        }
    }
}

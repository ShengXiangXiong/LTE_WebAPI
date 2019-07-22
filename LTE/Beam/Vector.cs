using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* Copyright (c) 2004-2005, Samuli Laine */
// Copyright (c) 2018-2019, 尹静萍 
// 文献：Samuli Laine, Samuel Siltanen, Tapio Lokki, Lauri Savioja. Accelerated beam tracing algorithm[J]. Applied Acoustics, 2009, 70(1): 172-181.
namespace LTE.Beam
{
    public class Vector3
    {
        public float x, y, z;
        public Vector3() { }
        public Vector3(ref Vector3 v) { x = v.x; y = v.y; z = v.z; }
        public Vector3(ref Vector4 v) { x = v.x; y = v.y; z = v.z; }
        public Vector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
        public Vector3(float fx, float fy, float fz) { x = fx; y = fy; z = fz; }
        public float this[int i] { get { if (i == 0) return x; else if (i == 1) return y; else return z; } set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; } }
        public void set(float fx, float fy, float fz) { x = fx; y = fy; z = fz; }
        public void opMultiplyAssign(float f) { x *= f; y *= f; z *= f; }
        public void opMultiplyAssign(ref Matrix3 m) { float tx = x, ty = y, tz = z; x = tx * m[0, 0] + ty * m[1, 0] + tz * m[2, 0]; y = tx * m[0, 1] + ty * m[1, 1] + tz * m[2, 1]; z = tx * m[0, 2] + ty * m[1, 2] + tz * m[2, 2]; }
        public void opAddAssign(ref Vector3 v) { x += v.x; y += v.y; z += v.z; }
        public void opAddAssign(Vector3 v) { x += v.x; y += v.y; z += v.z; }
        public void opMinusAssign(ref Vector3 v) { x -= v.x; y -= v.y; z -= v.z; }
        public void opNegative() { x = -x; y = -y; z = -z; }
        public bool normalize() { float l = x * x + y * y + z * z; if (l == 0) return false; l = 1 / (float)Math.Sqrt(l); x *= l; y *= l; z *= l; return true; }
        public float length() { return (float)Math.Sqrt(x * x + y * y + z * z); }
        public float lengthSqr() { return x * x + y * y + z * z; }
        public void scale(ref Vector3 v) { x *= v.x; y *= v.y; z *= v.z; }

        public static bool equal(ref Vector3 a, ref Vector3 b) {if(Math.Abs(a.x - b.x) < 0.001 && Math.Abs(a.x - b.x) < 0.001 && Math.Abs(a.x - b.x) < 0.001) return true; else return false;}
        public static Vector4 mirror(ref Vector4 p, ref Vector4 r) { Vector3 pn = new Vector3(p.x, p.y, p.z); Vector3 rn = new Vector3(r.x, r.y, r.z); float dpr = 2 * Vector3.dot(ref pn, ref rn); Vector3 pp = new Vector3(pn - dpr * rn); float pw = p.w - r.w * dpr; return new Vector4(pp.x, pp.y, pp.z, pw); }
        public static Vector4 mirror(ref Vector4 p, Vector4 r) { Vector3 pn = new Vector3(p.x, p.y, p.z); Vector3 rn = new Vector3(r.x, r.y, r.z); float dpr = 2 * Vector3.dot(ref pn, ref rn); Vector3 pp = new Vector3(pn - dpr * rn); float pw = p.w - r.w * dpr; return new Vector4(pp.x, pp.y, pp.z, pw); }
        //public static bool operator ==(Vector3 v1, Vector3 v2) { return (v1.x == v2.x) && (v1.y == v2.y) && (v1.z == v2.z); }
        //public static bool operator !=(Vector3 v1, Vector3 v2) { return (v1.x != v2.x) || (v1.y != v2.y) || (v1.z != v2.z); }
        public static Vector3 operator *(float f, Vector3 v) { return new Vector3(v.x * f, v.y * f, v.z * f); }
        public static Vector3 operator *(Vector3 v, float f) { return new Vector3(v.x * f, v.y * f, v.z * f); }
        public static Vector3 operator +(Vector3 v1, Vector3 v2) { return new Vector3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z); }
        public static Vector3 operator -(Vector3 v1, Vector3 v2) { return new Vector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z); }
        public static float dot(ref Vector3 v1, ref Vector3 v2) { return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z; }
        public static float dot(ref Vector3 v1, Vector3 v2) { return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z; }
        public static float dot(Vector3 v1, Vector3 v2) { return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z; }
        public static Vector3 cross(ref Vector3 v1, ref Vector3 v2) { return new Vector3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x); }
        public static Vector3 cross(Vector3 v1, Vector3 v2) { return new Vector3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x); }
        public static Vector3 normalize(Vector3 v) { float l = dot(ref v, ref v); if (l != 0) l = 1 / (float)Math.Sqrt(l); return v * l; }
        public static Vector3 normalize(ref Vector3 v) { float l = dot(ref v, ref v); if (l != 0) l = 1 / (float)Math.Sqrt(l); return v * l; }
        public static Vector3 scale(ref Vector3 v1, ref Vector3 v2) { return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z); }
        public static Vector3 operator *(Vector3 v, Matrix3 m) { return new Vector3(v.x * m[0, 0] + v.y * m[1, 0] + v.z * m[2, 0], v.x * m[0, 1] + v.y * m[1, 1] + v.z * m[2, 1], v.x * m[0, 2] + v.y * m[1, 2] + v.z * m[2, 2]); }
        public static Vector3 operator *(Matrix3 m, Vector3 v) { return new Vector3(v.x * m[0, 0] + v.y * m[0, 1] + v.z * m[0, 2], v.x * m[1, 0] + v.y * m[1, 1] + v.z * m[1, 2], v.x * m[2, 0] + v.y * m[2, 1] + v.z * m[2, 2]); }
        public static bool operator <(Vector3 a, Vector3 b) { if (a.x < b.x) return true; if (a.x > b.x) return false; if (a.y < b.y) return true; if (a.y > b.y) return false; return a.z < b.z; }
        public static bool operator >(Vector3 a, Vector3 b) { if (a.x > b.x) return true; if (a.x < b.x) return false; if (a.y > b.y) return true; if (a.y < b.y) return false; return a.z > b.z; }

        // 两向量的夹角，弧度
        public static double getAngle(ref Vector3 v1, ref Vector3 v2)
        {
            double d1 = Math.Sqrt(Math.Pow(v1.x, 2) + Math.Pow(v1.y, 2) + Math.Pow(v1.z, 2));
            double d2 = Math.Sqrt(Math.Pow(v2.x, 2) + Math.Pow(v2.y, 2) + Math.Pow(v2.z, 2));
            double angle = dot(ref v1, ref v2) / (d1 * d2);
            return Math.Acos(angle);
        }
    };
    //------------------------------------------------------------------------

    public class Matrix3
    {
        private float[,] matrix;

        public float this[int i, int j] { get { return matrix[i, j]; } set { matrix[i, j] = value; } }
        public Matrix3() { matrix = new float[3, 3]; identity(); }
        public Matrix3(ref Matrix3 m) { matrix = new float[3, 3]; for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) matrix[i, j] = m.matrix[i, j]; }
        public Matrix3(float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22) { set(m00, m01, m02, m10, m11, m12, m20, m21, m22); }
        public void assign(Matrix3 m) { for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) matrix[i, j] = m.matrix[i, j]; }
        public Vector3 this[int i] { get { return new Vector3(matrix[i, 0], matrix[i, 1], matrix[i, 2]); } }
        public void set(float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22) { matrix[0, 0] = m00; matrix[0, 1] = m01; matrix[0, 2] = m02; matrix[1, 0] = m10; matrix[1, 1] = m11; matrix[1, 2] = m12; matrix[2, 0] = m20; matrix[2, 1] = m21; matrix[2, 2] = m22; }
        public Vector3 getRow(int i) { return new Vector3(matrix[i, 0], matrix[i, 1], matrix[i, 2]); }
        public Vector3 getColumn(int i) { return new Vector3(matrix[0, i], matrix[1, i], matrix[2, i]); }
        public void setRow(int i, ref Vector3 v) { matrix[i, 0] = v.x; matrix[i, 1] = v.y; matrix[i, 2] = v.z; }
        public void setColumn(int i, ref Vector3 v) { matrix[0, i] = v.x; matrix[1, i] = v.y; matrix[2, i] = v.z; }
        public void opMultiplyAssign(float f) { for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) matrix[i, j] *= f; }
        public void identity() { for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) matrix[i, j] = (i == j) ? 1 : 0; }
        private void swap(ref float a, ref float b) { float tmp = a; a = b; b = tmp; }
        public void transpose() { swap(ref matrix[1, 0], ref matrix[0, 1]); swap(ref matrix[2, 0], ref matrix[0, 2]); swap(ref matrix[2, 1], ref matrix[1, 2]); }
        public float det() { return matrix[0, 0] * (matrix[1, 1] * matrix[2, 2] - matrix[2, 1] * matrix[1, 2]) + matrix[0, 1] * (matrix[2, 0] * matrix[1, 2] - matrix[1, 0] * matrix[2, 2]) + matrix[0, 2] * (matrix[1, 0] * matrix[2, 1] - matrix[2, 0] * matrix[1, 1]); }

        public bool invert()
        {
            float det00 = matrix[1, 1] * matrix[2, 2] - matrix[2, 1] * matrix[1, 2];
            float det01 = matrix[2, 0] * matrix[1, 2] - matrix[1, 0] * matrix[2, 2];
            float det02 = matrix[1, 0] * matrix[2, 1] - matrix[2, 0] * matrix[1, 1];

            float det = matrix[0, 0] * det00 + matrix[0, 1] * det01 + matrix[0, 2] * det02;

            if (det == 0)
                return false;

            det = 1 / det;

            float[,] t = new float[3, 3];
            t[0, 0] = det * det00;
            t[1, 0] = det * det01;
            t[2, 0] = det * det02;
            t[0, 1] = det * (matrix[2, 1] * matrix[0, 2] - matrix[0, 1] * matrix[2, 2]);
            t[1, 1] = det * (matrix[0, 0] * matrix[2, 2] - matrix[2, 0] * matrix[0, 2]);
            t[2, 1] = det * (matrix[2, 0] * matrix[0, 1] - matrix[0, 0] * matrix[2, 1]);
            t[0, 2] = det * (matrix[0, 1] * matrix[1, 2] - matrix[1, 1] * matrix[0, 2]);
            t[1, 2] = det * (matrix[1, 0] * matrix[0, 2] - matrix[0, 0] * matrix[1, 2]);
            t[2, 2] = det * (matrix[0, 0] * matrix[1, 1] - matrix[1, 0] * matrix[0, 1]);
            matrix = t;

            return true;
        }

        public void opMultiplyAssign(ref Matrix3 m)
        {
            float[,] tmp = new float[3, 3];

            float s = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        s = s + matrix[i, k] * m[k, j];
                    }
                    tmp[i, j] = s;
                    s = 0;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    matrix[i, j] = m[i, j];
                }
            }
        }

        public void rotate(float radians, ref Vector3 aboutThis)
        {
            float c = (float)Math.Cos(radians);
            float s = -(float)Math.Sin(radians);
            float t = 1 - c;
            Vector3 vn = new Vector3(ref aboutThis);
            if (!vn.normalize())
                return;

            Matrix3 m = new Matrix3(t * vn.x * vn.x + c, t * vn.x * vn.y + s * vn.z, t * vn.x * vn.z - s * vn.y,
                                t * vn.x * vn.y - s * vn.z, t * vn.y * vn.y + c, t * vn.y * vn.z + s * vn.x,
                                t * vn.x * vn.z + s * vn.y, t * vn.y * vn.z - s * vn.x, t * vn.z * vn.z + c);

            opMultiplyAssign(ref m);
        }

        public static bool operator ==(Matrix3 m1, Matrix3 m2) { for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) if (m1[i, j] != m2[i, j]) return false; return true; }
        public static bool operator !=(Matrix3 m1, Matrix3 m2) { return !(m1 == m2); }
        public static Matrix3 operator *(Matrix3 m1, Matrix3 m2) { Matrix3 t = new Matrix3(); for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) t[i, j] = m1[i, 0] * m2[0, j] + m1[i, 1] * m2[1, j] + m1[i, 2] * m2[2, j]; return t; }
        public static Matrix3 operator *(float f, Matrix3 m) { Matrix3 t = new Matrix3(ref m); ; t *= f; return t; }
        public static Matrix3 operator *(Matrix3 m, float f) { Matrix3 t = new Matrix3(ref m); ; t *= f; return t; }
        public static Matrix3 transpose(ref Matrix3 m) { Matrix3 t = new Matrix3(ref m); ; t.transpose(); return t; }
        public static Matrix3 invert(ref Matrix3 m) { Matrix3 t = new Matrix3(ref m); ; t.invert(); return t; }
    };

    //------------------------------------------------------------------------
    public class Matrix3x4
    {
        private float[,] matrix;

        public Matrix3x4() { identity(); }
        public Matrix3x4(ref Matrix3 m) { set(m[0, 0], m[0, 1], m[0, 2], 0, m[1, 0], m[1, 1], m[1, 2], 0, m[2, 0], m[2, 1], m[2, 2], 0); }
        public Matrix3x4(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13, float m20, float m21, float m22, float m23) { set(m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23); }
        public void assign(ref Matrix3x4 m) { for (int i = 0; i < 3; i++) for (int j = 0; j < 4; j++) matrix[i, j] = m.matrix[i, j]; }
        public Vector4 this[int i] { get { return new Vector4(matrix[i, 0], matrix[i, 1], matrix[i, 2], matrix[i, 3]); } }
        public float this[int i, int j] { get { return matrix[i, j]; } }
        public void set(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13, float m20, float m21, float m22, float m23) { matrix[0, 0] = m00; matrix[0, 1] = m01; matrix[0, 2] = m02; matrix[0, 3] = m03; matrix[1, 0] = m10; matrix[1, 1] = m11; matrix[1, 2] = m12; matrix[1, 3] = m13; matrix[2, 0] = m20; matrix[2, 1] = m21; matrix[2, 2] = m22; matrix[2, 3] = m23; }
        public Vector4 getRow(int i) { return new Vector4(matrix[i, 0], matrix[i, 1], matrix[i, 2], matrix[i, 3]); }
        public Vector3 getColumn(int i) { return new Vector3(matrix[0, i], matrix[1, i], matrix[2, i]); }
        public void setRow(int i, ref Vector4 v) { matrix[i, 0] = v.x; matrix[i, 1] = v.y; matrix[i, 2] = v.z; matrix[i, 3] = v.w; }
        public void setColumn(int i, ref Vector3 v) { matrix[0, i] = v.x; matrix[1, i] = v.y; matrix[2, i] = v.z; }
        public Matrix3 getRotation() { return new Matrix3(matrix[0, 0], matrix[0, 1], matrix[0, 2], matrix[1, 0], matrix[1, 1], matrix[1, 2], matrix[2, 0], matrix[2, 1], matrix[2, 2]); }
        public Vector3 getTranslation() { return new Vector3(matrix[0, 3], matrix[1, 3], matrix[2, 3]); }
        public void setRotation(ref Matrix3 m) { matrix[0, 0] = m[0, 0]; matrix[0, 1] = m[0, 1]; matrix[0, 2] = m[0, 2]; matrix[1, 0] = m[1, 0]; matrix[1, 1] = m[1, 1]; matrix[1, 2] = m[1, 2]; matrix[2, 0] = m[2, 0]; matrix[2, 1] = m[2, 1]; matrix[2, 2] = m[2, 2]; }
        public void setTranslation(ref Vector3 v) { matrix[0, 3] = v[0]; matrix[1, 3] = v[1]; matrix[2, 3] = v[2]; }
        public void opMultiplyAssign(float f) { for (int i = 0; i < 3; i++) for (int j = 0; j < 4; j++) matrix[i, j] *= f; }
        public void identity() { for (int i = 0; i < 3; i++) for (int j = 0; j < 4; j++) matrix[i, j] = (i == j) ? 1 : 0; }
        public float det() { return getRotation().det(); }
        public void translate(ref Vector3 v) { matrix[0, 3] += v.x; matrix[1, 3] += v.y; matrix[2, 3] += v.z; }

        public bool invert()
        {
            Matrix3 rotation = getRotation();
            if (!rotation.invert())
                return false;

            Vector3 translation = rotation * getTranslation();

            set(rotation[0][0], rotation[0][1], rotation[0][2], -translation[0],
            rotation[1][0], rotation[1][1], rotation[1][2], -translation[1],
            rotation[2][0], rotation[2][1], rotation[2][2], -translation[2]);

            return true;
        }

        public void opMultiplyAssign(ref Matrix3x4 m)
        {
            Matrix3 rotation = getRotation() * m.getRotation();
            Vector3 translation = getRotation() * m.getTranslation() + m.getTranslation();

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    matrix[i, j] = rotation[i, j];

            for (int i = 0; i < 3; i++)
                matrix[i, 3] = translation[i];
        }

        //------------------------------------------------------------------------

        public static Matrix3x4 operator *(Matrix3x4 m1, Matrix3x4 m2)
        {
            Matrix3 rotation = m1.getRotation() * m2.getRotation();
            Vector3 translation = m1.getRotation() * m2.getTranslation() + m1.getTranslation();

            return new Matrix3x4(rotation[0, 0], rotation[0, 1], rotation[0, 2], translation[0],
                     rotation[1, 0], rotation[1, 1], rotation[1, 2], translation[1],
                     rotation[2, 0], rotation[2, 1], rotation[2, 2], translation[2]);
        }

        public static bool operator ==(Matrix3x4 m1, Matrix3x4 m2) { for (int i = 0; i < 3; i++) for (int j = 0; j < 4; j++) if (m1[i, j] != m2[i, j]) return false; return true; }
        public static bool operator !=(Matrix3x4 m1, Matrix3x4 m2) { return !(m1 == m2); }
        public static Matrix3x4 operator *(float f, Matrix3x4 m) { Matrix3x4 t = new Matrix3x4(); t.assign(ref m); t *= f; return t; }
        public static Matrix3x4 operator *(Matrix3x4 m, float f) { Matrix3x4 t = new Matrix3x4(); t.assign(ref m); t *= f; return t; }
        public static Matrix3x4 invert(Matrix3x4 m) { Matrix3x4 t = new Matrix3x4(); t.assign(ref m); t.invert(); return t; }
    };

    //------------------------------------------------------------------------

    public class Vector4
    {
        public float x, y, z, w;		// uninitialized

        public Vector4() { }
        public Vector4(ref Vector4 v) { x = v.x; y = v.y; z = v.z; w = v.w; }
        public Vector4(Vector4 v) { x = v.x; y = v.y; z = v.z; w = v.w; }
        public Vector4(float fx, float fy, float fz, float fw) { x = fx; y = fy; z = fz; w = fw; }
        public void opAssign(ref Vector4 v) { x = v.x; y = v.y; z = v.z; w = v.w; }
        public float this[int i] { get { if (i == 0) return x; else if (i == 1) return y; else if (i == 2) return z; else return w; } set { if (i == 0)  x = value; else if (i == 1) y = value; else if (i == 2) z = value; else w = value; } }
        public void set(float fx, float fy, float fz, float fw) { x = fx; y = fy; z = fz; w = fw; }
        public void opMultiplyAssign(float f) { x *= f; y *= f; z *= f; w *= f; }
        public void opAddAssign(ref Vector4 v) { x += v.x; y += v.y; z += v.z; w += v.w; }
        public void opMiniusAssign(ref Vector4 v) { x -= v.x; y -= v.y; z -= v.z; w -= v.w; }
        public void opNegative() { x = -x; y = -y; z = -z; w = -w; }
        public bool normalize() { float len = (float)Math.Sqrt(x * x + y * y + z * z); if (len == 0) return false; len = 1 / len; x *= len; y *= len; z *= len; w *= len; return true; }
        public bool normalizeByW() { if (w == 0) return false; float iw = 1 / w; x *= iw; y *= iw; z *= iw; w = 1; return true; }
        public void scale(ref Vector4 v) { x *= v.x; y *= v.y; z *= v.z; w *= v.w; }

        public static bool operator ==(Vector4 v1, Vector4 v2) { return (v1.x == v2.x) && (v1.y == v2.y) && (v1.z == v2.z) && (v1.w == v2.w); }
        public static bool operator !=(Vector4 v1, Vector4 v2) { return (v1.x != v2.x) || (v1.y != v2.y) || (v1.z != v2.z) || (v1.w != v2.w); }
        public static Vector4 operator *(float f, Vector4 v) { return new Vector4(v.x * f, v.y * f, v.z * f, v.w * f); }
        public static Vector4 operator *(Vector4 v, float f) { return new Vector4(v.x * f, v.y * f, v.z * f, v.w * f); }
        public static Vector4 operator +(Vector4 v1, Vector4 v2) { return new Vector4(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w); }
        public static Vector4 operator -(Vector4 v1, Vector4 v2) { return new Vector4(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z, v1.w - v2.w); }
        public static float dot(ref Vector4 v1, ref Vector4 v2) { return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z + v1.w * v2.w; }
        public static float dot(ref Vector3 v, ref Vector4 p) { return v.x * p.x + v.y * p.y + v.z * p.z + p.w; }
        public static float dot(ref Vector3 v, Vector4 p) { return v.x * p.x + v.y * p.y + v.z * p.z + p.w; }
        public static float dot(Vector3 v, ref Vector4 p) { return v.x * p.x + v.y * p.y + v.z * p.z + p.w; }
        public static Vector4 normalizeByW(ref Vector4 v) { if (v.w == 0) return v; float iw = 1 / v.w; return new Vector4(v.x * iw, v.y * iw, v.z * iw, 1); }
        public static Vector3 divByW(ref Vector4 v) { if (v.w == 0) return new Vector3(0, 0, 0); float iw = 1 / v.w; return new Vector3(v.x * iw, v.y * iw, v.z * iw); }
        public static Vector4 scale(ref Vector4 v1, ref Vector4 v2) { return new Vector4(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z, v1.w * v2.w); }
        public static Vector3 mirror(ref Vector3 v, ref Vector4 p) { float d = 2 * Vector4.dot(ref v, ref p); return new Vector3(v.x - d * p.x, v.y - d * p.y, v.z - d * p.z); }
        public static Vector3 mirror(ref Vector3 v, Vector4 p) { float d = 2 * Vector4.dot(ref v, ref p); return new Vector3(v.x - d * p.x, v.y - d * p.y, v.z - d * p.z); }

    };
}

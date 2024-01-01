using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace mohaaTikToMd3Helper
{
    static class VectorStuff
    {
		// Ported from JK2
        public static Vector3 RotatePointAroundMe(this Vector3 dir,Vector3 point,float degrees)
        {
			Vector3 dst;
			float[,] m = new float[3,3];
			float[,] im = new float[3,3];
			float[,] zrot = new float[3,3];
			float[,] tmpmat = new float[3,3];
			float[,] rot = new float[3,3];
			int i;
			Vector3 vr, vup, vf;
			float rad;

			vf.X = dir.X;
			vf.Y = dir.Y;
			vf.Z = dir.Z;

			vr = dir.PerpendicularVector();
			vup = Vector3.Cross(vr, vf);

			m[0,0] = vr.X;
			m[1,0] = vr.Y;
			m[2,0] = vr.Z;

			m[0,1] = vup.X;
			m[1,1] = vup.Y;
			m[2,1] = vup.Z;

			m[0,2] = vf.X;
			m[1,2] = vf.Y;
			m[2,2] = vf.Z;

			im = (float[,])m.Clone();

			im[0,1] = m[1,0];
			im[0,2] = m[2,0];
			im[1,0] = m[0,1];
			im[1,2] = m[2,1];
			im[2,0] = m[0,2];
			im[2,1] = m[1,2];

			//memset(zrot, 0, sizeof(zrot));
			zrot[0,0] = zrot[1,1] = zrot[2,2] = 1.0F;

			rad = degrees * (float)Math.PI / 180.0f;// DEG2RAD(degrees);
			zrot[0,0] = (float)Math.Cos(rad);
			zrot[0,1] = (float)Math.Sin(rad);
			zrot[1,0] = -(float)Math.Sin(rad);
			zrot[1,1] = (float)Math.Cos(rad);

			tmpmat= MatrixMultiply(m, zrot);
			rot = MatrixMultiply(tmpmat, im);

			dst.X = rot[0, 0] * point.X + rot[0, 1] * point.Y + rot[0, 2] * point.Z;
			dst.Y = rot[1, 0] * point.X + rot[1, 1] * point.Y + rot[1, 2] * point.Z;
			dst.Z = rot[2, 0] * point.X + rot[2, 1] * point.Y + rot[2, 2] * point.Z;

			return dst;
		}

		public static Vector3 PerpendicularVector(this Vector3 src)
		{
			int pos;
			int i;
			float minelem = 1.0F;
			Vector3 tempvec;
			Vector3 dst;

			/*
			** find the smallest magnitude axially aligned vector
			*/
			tempvec.X = tempvec.Y = tempvec.Z = 0.0F;
			if (Math.Abs(src.X) < minelem)
			{
				pos = 0;
				tempvec.X = 1.0F;
				minelem = Math.Abs(src.X);
			}
			if (Math.Abs(src.Y) < minelem)
			{
				pos = 1;
				tempvec.Y = 1.0F;
				minelem = Math.Abs(src.Y);
			}
			if (Math.Abs(src.Z) < minelem)
			{
				pos = 2;
				tempvec.Z = 1.0F;
				minelem = Math.Abs(src.Z);
			}


			/*
			** project the point onto the plane defined by src
			*/
			dst = ProjectPointOnPlane(tempvec, src);

			/*
			** normalize the result
			*/
			dst = Vector3.Normalize(dst);
			return dst;
		}


		public static Vector3 ProjectPointOnPlane(Vector3 p, Vector3 normal)
		{
			Vector3 dst;
			float d;
			Vector3 n;
			float inv_denom;

			inv_denom = Vector3.Dot(normal, normal);
			inv_denom = 1.0f / inv_denom;

			d = Vector3.Dot(normal, p) * inv_denom;

			n.X = normal.X * inv_denom;
			n.Y = normal.Y * inv_denom;
			n.Z = normal.Z * inv_denom;

			dst.X = p.X - d * n.X;
			dst.Y = p.Y - d * n.Y;
			dst.Z = p.Z - d * n.Z;
			return dst;
		}

		static float [,] MatrixMultiply(float[,] in1, float[,] in2)
		{
			float[,] res = new float[3, 3];
			res[0,0] = in1[0,0] * in2[0,0] + in1[0,1] * in2[1,0] +
						in1[0,2] * in2[2,0];
			res[0,1] = in1[0,0] * in2[0,1] + in1[0,1] * in2[1,1] +
						in1[0,2] * in2[2,1];
			res[0,2] = in1[0,0] * in2[0,2] + in1[0,1] * in2[1,2] +
						in1[0,2] * in2[2,2];
			res[1,0] = in1[1,0] * in2[0,0] + in1[1,1] * in2[1,0] +
						in1[1,2] * in2[2,0];
			res[1,1] = in1[1,0] * in2[0,1] + in1[1,1] * in2[1,1] +
						in1[1,2] * in2[2,1];
			res[1,2] = in1[1,0] * in2[0,2] + in1[1,1] * in2[1,2] +
						in1[1,2] * in2[2,2];
			res[2,0] = in1[2,0] * in2[0,0] + in1[2,1] * in2[1,0] +
						in1[2,2] * in2[2,0];
			res[2,1] = in1[2,0] * in2[0,1] + in1[2,1] * in2[1,1] +
						in1[2,2] * in2[2,1];
			res[2,2] = in1[2,0] * in2[0,2] + in1[2,1] * in2[1,2] +
						in1[2,2] * in2[2,2];
			return res;
		}
	}
}

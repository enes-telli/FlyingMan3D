using UnityEngine;
using System.Collections;
using System;

namespace Dreamteck
{
    public static class DMath
    {
        public static double Sin(double a)
        {
            return Math.Sin(a);
        }

        public static double Cos(double a)
        {
            return Math.Cos(a);
        }

        public static double Tan(double a)
        {
            return Math.Tan(a);
        }

        public static double Pow(double x, double y)
        {
            return Math.Pow(x, y);
        }

        public static double Log(double a, double newBase)
        {
            return Math.Log(a, newBase);
        }

        public static double Log10(double a)
        {
            return Math.Log10(a);
        }

        public static double Clamp01(double a)
        {
            if (a > 1.0) return 1.0;
            if (a < 0.0) return 0.0;
            return a;
        }

        public static double Clamp(double a, double min, double max)
        {
            if (a > max) return max;
            if (a < min) return min;
            return a;
        }

        public static double Lerp(double a, double b, double t)
        {
            t = Clamp01(t);
            return a + (b - a) * t;
        }

        public static double InverseLerp(double a, double b, double t)
        {
            if (a == b) return 0.0;
            return Clamp01((t-a)/(b-a));
        }

        public static Vector3 LerpVector3(Vector3 a, Vector3 b, double t)
        {
            t = Clamp01(t);
            Vector3 delta = (b - a);
            double x = a.x + delta.x * t;
            double y = a.y + delta.y * t;
            double z = a.z + delta.z * t;
            return new Vector3((float)x, (float)y, (float)z);
        }

        public static double Round(double a)
        {
            return Math.Round(a);
        }

        public static int RoundInt(double a)
        {
            return (int)Math.Round(a);
        }

        public static double Ceil(double a)
        {
            return Math.Ceiling(a);
        }

        public static int CeilInt(double a)
        {
            return (int)Math.Ceiling(a);
        }

        public static double Floor(double a)
        {
            return Math.Floor(a);
        }

        public static int FloorInt(double a)
        {
            return (int)Math.Floor(a);
        }

        public static double Move(double current, double target, double amount)
        {
            if (target > current)
            {
                current += amount;
                if (current > target) return target;
            }
            else
            {
                current -= amount;
                if (current < target) return target;
            }
            return current;
        }

        public static double Abs(double a)
        {
            if (a < 0.0) return a * -1.0;
            return a;
        }
    }
}
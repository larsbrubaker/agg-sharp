﻿/*
Copyright (c) 2014, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;

namespace MatterHackers.Agg
{
    public struct Point2D
    {
        public static readonly Point2D Zero = new Point2D();
        public int x, y;

        public Point2D(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Point2D(double x, double y)
        {
            this.x = (int)Math.Round(x);
            this.y = (int)Math.Round(y);
        }

        public static double GetDeltaAngle(double StartAngle, double EndAngle)
        {
            if (StartAngle != Range0To2PI(StartAngle)) throw new Exception("StartAngle == Range0To2PI(StartAngle)");
            if (EndAngle != Range0To2PI(EndAngle)) throw new Exception("EndAngle   == Range0To2PI(EndAngle)");

            double DeltaAngle = EndAngle - StartAngle;
            if (DeltaAngle > System.Math.PI)
            {
                DeltaAngle -= 2 * Math.PI;
            }

            if (DeltaAngle < -System.Math.PI)
            {
                DeltaAngle += 2 * Math.PI;
            }

            return DeltaAngle;
        }

        public static double GetDistanceBetween(Point2D a, Point2D b)
        {
            return (double)System.Math.Sqrt(GetDistanceBetweenSquared(a, b));
        }

        public static double GetDistanceBetweenSquared(Point2D a, Point2D b)
        {
            return ((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
        }

        public static Point2D operator -(Point2D a, Point2D b)
        {
            Point2D temp = new Point2D();
            temp.x = a.x - b.x;
            temp.y = a.y - b.y;
            return temp;
        }

        public static bool operator !=(Point2D a, Point2D b)
        {
            return !a.Equals(b);
        }

        public static Point2D operator *(Point2D a, Point2D b)
        {
            Point2D temp = new Point2D();
            temp.x = a.x * b.x;
            temp.y = a.y * b.y;
            return temp;
        }

        public static Point2D operator *(Point2D a, int b)
        {
            Point2D temp = new Point2D();
            temp.x = a.x * b;
            temp.y = a.y * b;
            return temp;
        }

        public static Point2D operator *(int b, Point2D a)
        {
            Point2D temp = new Point2D();
            temp.x = a.x * b;
            temp.y = a.y * b;
            return temp;
        }

        public static Point2D operator /(Point2D a, Point2D b)
        {
            Point2D temp = new Point2D();
            temp.x = a.x / b.x;
            temp.y = a.y / b.y;
            return temp;
        }

        public static Point2D operator /(Point2D a, int b)
        {
            Point2D temp = new Point2D();
            temp.x = a.x / b;
            temp.y = a.y / b;
            return temp;
        }

        public static Point2D operator /(int b, Point2D a)
        {
            Point2D temp = new Point2D();
            temp.x = a.x / b;
            temp.y = a.y / b;
            return temp;
        }

        public static Point2D operator +(Point2D a, Point2D b)
        {
            Point2D temp = new Point2D();
            temp.x = a.x + b.x;
            temp.y = a.y + b.y;
            return temp;
        }

        public static bool operator ==(Point2D a, Point2D b)
        {
            return a.Equals(b);
        }

        public static double Range0To2PI(double Value)
        {
            if (Value < 0)
            {
                Value += 2 * Math.PI;
            }

            if (Value >= 2 * Math.PI)
            {
                Value -= 2 * Math.PI;
            }

            if (Value < 0 || Value > 2 * System.Math.PI) throw new Exception("Value >= 0 && Value <= 2 * PI");

            return Value;
        }

        public double Cross(Point2D B)
        {
            return x * B.y - y * B.x;
        }

        public double Dot(Point2D B)
        {
            return (x * B.x + y * B.y);
        }

        // are they the same within the error value?
        public bool Equals(Point2D otherVector)
        {
            if (x == otherVector.x && y == otherVector.y)
            {
                return true;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Point2D p = (Point2D)obj;
            if ((System.Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (x == p.x) && (y == p.y);
        }

        public double GetAngle0To2PI()
        {
            return (double)Range0To2PI((double)System.Math.Atan2(y, x));
        }

        public double GetDeltaAngle(Point2D point)
        {
            return (double)GetDeltaAngle(GetAngle0To2PI(), point.GetAngle0To2PI());
        }

        public override int GetHashCode()
        {
            return new { x, y }.GetHashCode();
        }

        public double GetLength()
        {
            return Math.Sqrt((x * x) + (y * y));
        }

        public double GetLengthSquared()
        {
            return Dot(this);
        }

        public Point2D GetNormal()
        {
            Point2D normal = this;
            normal.Normalize();
            return normal;
        }

        public Point2D GetPerpendicular()
        {
            Point2D temp = new Point2D(y, -x);

            return temp;
        }

        public Point2D GetPerpendicularNormal()
        {
            Point2D Perpendicular = GetPerpendicular();
            Perpendicular.Normalize();
            return Perpendicular;
        }

        public double GetSquaredDistanceTo(Point2D other)
        {
            return ((x - other.x) * (x - other.x) + (y - other.y) * (y - other.y));
        }

        public void Negate()
        {
            x = -x;
            y = -y;
        }

        public void Normalize()
        {
            double Length;

            Length = GetLength();

            if (Length == 0) throw new Exception("Length != 0.f");

            if (Length != 0.0f)
            {
                double InversLength = 1.0f / Length;
                x = Convert.ToInt32(x * InversLength);
                y = Convert.ToInt32(y * InversLength);
            }
        }

        public void Normalize(double Length)
        {
            if (Length == 0) throw new Exception("Length == 0.f");

            if (Length != 0.0f)
            {
                double InversLength = 1.0f / Length;
                x = Convert.ToInt32(x * InversLength);
                y = Convert.ToInt32(y * InversLength);
            }
        }

        public double NormalizeAndReturnLength()
        {
            double Length;

            Length = GetLength();

            if (Length != 0.0f)
            {
                double InversLength = 1.0f / Length;
                x = Convert.ToInt32(x * InversLength);
                y = Convert.ToInt32(y * InversLength);
            }

            return Length;
        }

        public void Set(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    };
}
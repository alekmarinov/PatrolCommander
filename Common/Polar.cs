using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PatrolCommander.Common
{
    public class Polar
    {
        public double Radius { get; set; }
        public double Angle { get; set; }

        public Polar(double X, double Y)
        {
            Angle = 180 * Math.Atan2(Y, X) / Math.PI;
            Radius = (double)Math.Sqrt(X * X + Y * Y);
        }

        public Polar(int X, int Y)
            : this((double)X, (double)Y)
        { }

        public Polar(Point pnt)
            : this(pnt.X, pnt.Y)
        { }

        public Point getPoint()
        {
            Point pnt = new Point();
            double rad = Math.PI * Angle / 180;
            pnt.X = Radius * Math.Cos(rad);
            pnt.Y = Radius * Math.Sin(rad);
            return pnt;
        }
    }
}

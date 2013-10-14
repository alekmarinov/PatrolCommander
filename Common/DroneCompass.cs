using CaledosLab.Portable.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PatrolCommander.Common
{
    public class DroneCompass
    {
        private int offsetCorrection = 305;
        private FrameworkElement element;
        private FrameworkElement zeroElement;
        private double value = 0;

        public DroneCompass(FrameworkElement element, FrameworkElement zeroElement)
        {
            this.element = element;
            this.zeroElement = zeroElement;
        }

        public double Value
        {
            set {
                setElementPosition(value);
            }
            get
            {
                return value;
            }
        }

        // set compass at degree 0..360
        private void setElementPosition(double angle)
        {
            value = angle;
            angle = (angle + 360) % 360;
            // if (angle < 0) angle += 360;

            /*
            int nang = (int)Math.Floor(angle);
            nang = nang / 360;
            nang = (angle > 0) ? nang : -nang;
            angle -= nang * 360;
             * */

            double offset = angle / 360 * element.ActualWidth / 2;

            // Logger.WriteLine(".setElementPosition: angle = " + angle + ", offset = " + offset);
            RectangleGeometry rectClip = new RectangleGeometry();
            rectClip.Rect = new Rect(offsetCorrection + offset, 0, 580, 100);
            element.Clip = rectClip;
            element.Margin = new Thickness(-offsetCorrection - offset, 0, 0, 0);
        }
    }
}

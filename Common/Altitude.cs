using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PatrolCommander.Common
{
    public class Altitude
    {
        private int offsetCorrection = 3695;
        private FrameworkElement element;
        private FrameworkElement zeroElement;
        private double value = 0;

        public Altitude(FrameworkElement element, FrameworkElement zeroElement)
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

        private double altitudeToPosition(double altitude)
        {
            return -altitude * 0.96 + offsetCorrection;
        }

        // set altitude position
        private void setElementPosition(double altitude)
        {
            value = altitude;
            double offset = altitudeToPosition(altitude);
            if (offset > offsetCorrection)
                offset = offsetCorrection;
            else if (offset < 0)
                offset = 0;

            RectangleGeometry rectClip = new RectangleGeometry();
            rectClip.Rect = new Rect(0, offset, 110, 350);
            element.Clip = rectClip;
            element.Margin = new Thickness(0, -offset, 0, 0);
        }
    }
}

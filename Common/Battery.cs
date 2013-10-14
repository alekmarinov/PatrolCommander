using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PatrolCommander.Common
{
    class Battery
    {
        private FrameworkElement batteryElement;
        private FrameworkElement batteryRect;
        private TextBlock batteryText;
        private double value = 0;

        public Battery(FrameworkElement batteryElement, FrameworkElement batteryRect, TextBlock batteryText)
        {
            this.batteryElement = batteryElement;
            this.batteryRect = batteryRect;
            this.batteryText = batteryText;
        }

        public double Value
        {
            get { return value;  }
            set { setBatteryLevel(value); }
        }

        private void setBatteryLevel(double level)
        {
            if (level < 0) level = 0; else if (level > 100) level = 100;
            value = level;
            batteryText.Text = (int)Math.Floor(level) + "%";
            double width = batteryElement.ActualWidth-15;
            double rightMargin = 10 + width * (100 - level) / 100;
            Thickness margin = new Thickness(0, 64, rightMargin, 65);
            this.batteryRect.Margin = margin;
        }
    }
}

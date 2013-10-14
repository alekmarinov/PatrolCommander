using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;
using System.Windows.Threading;
using System.Windows.Media;
using CaledosLab.Portable.Logging;

namespace PatrolCommander.Common
{
    class PhoneCompass
    {
        private FrameworkElement compassImage;
        private Compass compass;
        private DispatcherTimer timer;

        private double magneticHeading;
        private double trueHeading;
        private double headingAccuracy;
        private Vector3 rawMagnetometerReading;
        private bool isDataValid;
        private bool isNeedCalibration = false;
        private bool isWorking = false;
        private PlaneProjection projection = new PlaneProjection();

        public PhoneCompass(FrameworkElement compassImage)
        {
            this.compassImage = compassImage;
            // Initialize the timer and add Tick event handler, but don't start it yet.
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += new EventHandler(timer_Tick);

            // Instantiate the compass.
            compass = new Compass();

            // Specify the desired time between updates. The sensor accepts
            // intervals in multiples of 20 ms.
            compass.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);

            compass.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<CompassReading>>(compass_CurrentValueChanged);
            compass.Calibrate += new EventHandler<CalibrationEventArgs>(compass_Calibrate);

            compassImage.Projection = projection;
            
        }

        public bool NeedCalibration
        {
            get { return isNeedCalibration; }
        }

        public bool Working
        {
            get { return isWorking; }
        }

        public double Value
        {
            get { return 90 + trueHeading; }
        }

        private void compass_CurrentValueChanged(object sender, SensorReadingEventArgs<CompassReading> e)
        {
            isDataValid = compass.IsDataValid;

            trueHeading = e.SensorReading.TrueHeading;
            magneticHeading = e.SensorReading.MagneticHeading;
            headingAccuracy = Math.Abs(e.SensorReading.HeadingAccuracy);
            rawMagnetometerReading = e.SensorReading.MagnetometerReading;
        }

        private void compass_Calibrate(object sender, CalibrationEventArgs e)
        {
            isNeedCalibration = true;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (!isNeedCalibration)
            {
                // Update compass direction
                projection.RotationZ = Value;
            }
            else
            {
                projection.RotationZ = 0;
                if (headingAccuracy <= 10)
                {
                    isNeedCalibration = false;
                }
            }
        }

        public bool Start()
        {
            try
            {
                Logger.WriteLine("PhoneCompass: Start");
                compass.Start();
                timer.Start();
                isWorking = true;
                return true;
            }
            catch (InvalidOperationException)
            {
            }
            return false;
        }

        public void Stop()
        {
            // Stop data acquisition from the compass.
            if (isWorking)
            {
                Logger.WriteLine("PhoneCompass: Stop");
                compass.Stop();
                timer.Stop();
            }
            isWorking = false;
        }


    }
}

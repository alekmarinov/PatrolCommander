using ARDrone2Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ARDrone2Client.Common.Navigation;
using ARDrone2Client.Common.Configuration;
using System.ComponentModel;
using Logger = CaledosLab.Portable.Logging.Logger;

namespace PatrolCommander.Common
{
    public class Drone : INotifyPropertyChanged
    {
        private bool enabled = true;
        public DroneClient droneClient;
        private DispatcherTimer detectFlyStatusTimer = null;
        private DispatcherTimer commandTimer = new DispatcherTimer();
        public delegate void OnFlyStopStartHandler(bool isFlying);
        public event OnFlyStopStartHandler OnFlyStopStart;

        public event PropertyChangedEventHandler PropertyChanged;
        private double lastVeloX = 0;
        private double lastVeloY = 0;
        private long lastVeloTime = 0;
        private double roll, pitch, yaw, gaz;
        private bool commandConsumed = false;
        
        public delegate void DroneMessageEvent(string message);
        public event DroneMessageEvent OnDroneMessage;

        private static Drone instance;

        public static Drone Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Drone();
                }
                return instance;
            }
        }

        private Drone()
        {
            Logger.WriteLine("DroneClient()");
            if (!enabled)
                return;
            droneClient = DroneClient.Instance;
            droneClient.NavigationDataViewModel.PropertyChanged += NavigationDataViewModel_PropertyChanged;
            droneClient.Messages.CollectionChanged += Messages_CollectionChanged;

            // commandTimer.Interval = TimeSpan.FromMilliseconds(50);
            // commandTimer.Tick += OnCommandTimerTick;
        }

        void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (OnDroneMessage != null)
                OnDroneMessage(droneClient.Messages.ElementAt(droneClient.Messages.Count-1).Content);
        }

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        private void NavigationDataViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Debug.WriteLine("NavigationDataViewModel_PropertyChanged:" + e.PropertyName);
            switch (e.PropertyName)
            {
                case "State":
                case "Yaw":
                case "Roll":
                case "Pitch":
                case "Altitude":
                case "VelocityX":
                case "VelocityY":
                    NotifyPropertyChanged(e.PropertyName);
                    break;
                case "BatteryPercentage":
                    NotifyPropertyChanged("Battery");
                    break;
                case "WifiLinkQuality":
                    NotifyPropertyChanged("Wifi");
                    break;
            }
        }

        public async Task<bool> Connect()
        {
            if (!enabled)
                return false;
            Logger.WriteLine("Drone.Connect()");
            bool connected =  await droneClient.ConnectAsync();
            return connected;
        }

        public void Disconnect()
        {
            if (!enabled)
                return ;
            Logger.WriteLine("Drone.Disconnect()");
            droneClient.Close();
        }

        private void OnCommandTimerTick(Object sender, EventArgs args)
        {
            if (!commandConsumed)
                DirectCommand(roll, pitch, yaw, gaz);
            commandConsumed = true;
        }

        public bool Land()
        {
            if (!enabled)
                return false;
            Logger.WriteLine("Drone.Land: IsActive = " + droneClient.IsActive + ", IsFlying = " + droneClient.IsFlying);
            if (droneClient.IsActive && droneClient.IsFlying)
            {
                if (detectFlyStatusTimer != null)
                    detectFlyStatusTimer.Stop();
                detectFlyStatusTimer = new DispatcherTimer();
                detectFlyStatusTimer.Interval = TimeSpan.FromMilliseconds(200);
                detectFlyStatusTimer.Tick += (object sender, EventArgs e) =>
                {
                    if (!droneClient.IsFlying)
                    {
                        if (OnFlyStopStart != null)
                            OnFlyStopStart(false);
                        detectFlyStatusTimer.Stop();
                    }
                };
                detectFlyStatusTimer.Start();

                droneClient.Land();
                // commandTimer.Stop();
                return true;
            }
            return false;
        }

        public bool TakeOff()
        {
            if (!enabled)
                return false;
            if (droneClient.IsActive && !droneClient.IsFlying)
            {
                Logger.WriteLine("TakeOff");

                if (detectFlyStatusTimer != null)
                    detectFlyStatusTimer.Stop();
                detectFlyStatusTimer = new DispatcherTimer();
                detectFlyStatusTimer.Interval = TimeSpan.FromMilliseconds(200);
                detectFlyStatusTimer.Tick += (object sender, EventArgs e) => {
                    if (droneClient.IsFlying)
                    {
                        if (OnFlyStopStart != null)
                            OnFlyStopStart(true);
                        detectFlyStatusTimer.Stop();
                    }
                };
                detectFlyStatusTimer.Start();

                droneClient.TakeOff();
                // commandTimer.Start();
                return true;
            }
            return false;
        }

        public bool IsFlying()
        {
            if (!enabled)
                return false;

            return droneClient.IsFlying;
        }

        public NavigationState State
        { 
            get {
                return (NavigationState)Enum.Parse(typeof(NavigationState), droneClient.NavigationDataViewModel.State);
            }            
        }

        public double Altitude
        {
            get
            {
                if (enabled && droneClient.NavigationDataViewModel.Altitude != null)
                {
                    try
                    {
                        double altitude = Double.Parse(droneClient.NavigationDataViewModel.Altitude);
                        return 100 * altitude;
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine(e.StackTrace);
                    }
                }
                return 0;
            }
        }

        public double Yaw
        {
            get
            {
                if (enabled && droneClient.NavigationDataViewModel.Yaw != null)
                {
                    try
                    {
                        double yaw = Double.Parse(droneClient.NavigationDataViewModel.Yaw);
                        return yaw * 180 / Math.PI;
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine(e.StackTrace);
                    }
                }
                return 0;
            }
        }

        public double Roll
        {
            get
            {
                if (enabled && droneClient.NavigationDataViewModel.Roll != null)
                {
                    try
                    {
                        double roll = Double.Parse(droneClient.NavigationDataViewModel.Roll);
                        return 180 + roll * 180 / Math.PI;
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine(e.StackTrace);
                    }
                }
                return 0;
            }
        }

        public double Pitch
        {
            get
            {
                if (enabled && droneClient.NavigationDataViewModel.Roll != null)
                {
                    try
                    {
                        double pitch = Double.Parse(droneClient.NavigationDataViewModel.Pitch);
                        return 180 + pitch * 180 / Math.PI;
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine(e.StackTrace);
                    }
                }
                return 0;
            }
        }
        
        public double VelocityX
        {
            get
            {
                if (enabled && droneClient.NavigationDataViewModel.VelocityX != null)
                {
                    try
                    {
                        return Double.Parse(droneClient.NavigationDataViewModel.VelocityX);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine(e.StackTrace);
                    }
                }
                return 0;
            }
        }

        public double VelocityY
        {
            get
            {
                if (enabled && droneClient.NavigationDataViewModel.VelocityY != null)
                {
                    try
                    {
                        return Double.Parse(droneClient.NavigationDataViewModel.VelocityY);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine(e.StackTrace);
                    }
                }
                return 0;
            }
        }

        public double Battery
        {
            get
            {
                if (enabled && droneClient.NavigationDataViewModel.BatteryPercentage != null)
                {
                    try
                    {
                        return Double.Parse(droneClient.NavigationDataViewModel.BatteryPercentage);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine(e.StackTrace);
                    }
                }
                return 0;
            }
        }

        public double Wifi
        {
            get
            {
                if (enabled && droneClient.NavigationDataViewModel.WifiLinkQuality != null)
                {
                    try
                    {
                        return Double.Parse(droneClient.NavigationDataViewModel.WifiLinkQuality);
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine(e.StackTrace);
                    }
                }
                return 0;
            }
        }

        private void Rotate(double speed)
        {
            if (speed > 1)
                speed = 1;
            else
                if (speed < -1)
                    speed = -1;
            // Debug.WriteLine("Rotating: " + speed);
            commandConsumed = false;
            yaw = speed;
            DirectCommand(0, 0, speed, 0);
        }

        private void UpDown(double speed)
        {
            if (speed > 1)
                speed = 1;
            else
                if (speed < -1)
                    speed = -1;
            // Debug.WriteLine("UpDown: " + speed);
            commandConsumed = false;
            gaz = speed;
            DirectCommand(0, 0, 0, speed);
        }

        private void Move(double speedRoll, double speedPitch)
        {
            speedRoll = inRange(speedRoll, -1, 1);
            speedPitch = inRange(speedPitch, -1, 1);
            // Debug.WriteLine("Rolling: " + speedRoll + ", Pitching: " + speedPitch);
            roll = speedRoll;
            pitch = speedPitch;
            commandConsumed = false;

            DirectCommand(speedRoll, speedPitch, 0, 0);
        }

        public void DirectCommand(double roll, double pitch, double yaw, double gaz)
        {
            // Logger.WriteLine("DirectCommand: " + roll + ", " + pitch + ", " + yaw + ", " + gaz);
            if (droneClient.IsFlying)
            {
                droneClient.InputState.Update((float)roll, (float)pitch, (float)yaw, (float)gaz);
            }
        }

        public void Stop()
        {
            // Debug.WriteLine("Stopping");
            DirectCommand(0, 0, 0, 0);
            roll = pitch = yaw = gaz = 0;
            commandConsumed = false;
        }

        public bool Emergency()
        {
            Logger.WriteLine("Emergency");
            if (droneClient.IsActive)
            {
                droneClient.Emergency();
                return true;
            }
            return false;
        }

        private double inRange(double x, double minX, double maxX)
        {
            if (x < minX) x = minX;
            else if (x > maxX) x = maxX;
            return x;
        }

        internal bool WatchDog()
        {
            if (IsFlying())
            {
                if (lastVeloTime == 0)
                {
                    lastVeloX = VelocityX;
                    lastVeloY = VelocityY;
                    lastVeloTime = DateTime.Now.Millisecond;
                }
                else if (DateTime.Now.Millisecond - lastVeloTime > 1000)
                {
                    if (lastVeloX == VelocityX && lastVeloY == VelocityY)
                    {
                        // drone is in the air but not moving, maybe need to reconnect
                        return true;
                    }
                    lastVeloX = VelocityX;
                    lastVeloY = VelocityY;
                    lastVeloTime = DateTime.Now.Millisecond;
                }
            }
            return false;
        }

        public bool IsActive 
        {
            get
            {
                return droneClient.IsActive;
            }
        }

        public bool OutdoorFlightConfig
        {
            get
            {
                return droneClient.Configuration.Control.outdoor.Value;
            }
            set
            {
                droneClient.SetConfiguration(droneClient.Configuration.Control.outdoor.Set(value).ToCommand());
            }
        }

        public bool OutdoorHullConfig
        {
            get
            {
                return droneClient.Configuration.Control.flight_without_shell.Value;
            }
            set
            {
                droneClient.SetConfiguration(droneClient.Configuration.Control.flight_without_shell.Set(value).ToCommand());
            }
        }

        public int AltitudeMax
        {
            get
            {
                return droneClient.Configuration.Control.altitude_max.Value;
            }
            set
            {
                droneClient.SetConfiguration(droneClient.Configuration.Control.altitude_max.Set(value).ToCommand());
            }
        }

        public float AngleMax
        {
            get
            {
                return droneClient.Configuration.Control.euler_angle_max.Value;
            }
            set
            {
                droneClient.SetConfiguration(droneClient.Configuration.Control.euler_angle_max.Set(value).ToCommand());
            }
        }

        public void Calibrate()
        {
            droneClient.PostCommand(Command.Calibration());
        }

        public void FlatTrim()
        {
            droneClient.PostCommand(Command.FlatTrim());
        }
    }
}

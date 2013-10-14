using ARDrone2Client.Common.Navigation;
using PatrolCommander.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logger = CaledosLab.Portable.Logging.Logger;

namespace PatrolCommander.Model
{
    class DroneModel : INotifyPropertyChanged
    {
        private Drone drone;

        public event PropertyChangedEventHandler PropertyChanged;

        public DroneModel(Drone drone)
        {
            this.drone = drone;
            this.drone.PropertyChanged += drone_PropertyChanged;
        }

        void drone_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "State":
                    NotifyPropertyChanged("TakeOffLandingSource");
                    break;
                case "Altitude":
                case "VelocityX":
                case "VelocityY":
                case "Yaw":
                case "Roll":
                case "Pitch":
                case "Battery":
                case "Wifi":
                    NotifyPropertyChanged(e.PropertyName);
                    break;
            }
        }

        public Uri TakeOffLandingSource
        {
            get {
                if (drone.State.Equals(NavigationState.Landed))
                {
                    return new Uri("/Assets/Buttons/Land.png", UriKind.Relative);
                }
                else
                {
                    return new Uri("/Assets/Buttons/TakeOff.png", UriKind.Relative);
                }
            }
        }

        public double Altitude
        {
            get
            {
                return drone.Altitude;
            }
        }

        public double Yaw
        {
            get
            {
                return drone.Yaw;
            }
        }

        public double Roll
        {
            get
            {
                return drone.Roll;
            }
        }

        public double Pitch
        {
            get
            {
                return drone.Pitch;
            }
        }

        public double Battery
        {
            get
            {
                return drone.Battery;
            }
        }

        public double VelocityX
        {
            get
            {
                return drone.VelocityX;
            }
        }

        public double VelocityY
        {
            get
            {
                return drone.VelocityY;
            }
        }

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}

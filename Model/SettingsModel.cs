using PatrolCommander.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using CaledosLab.Portable.Logging;

namespace PatrolCommander.Model
{
    class SettingsModel : INotifyPropertyChanged
    {
        private IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        public static int MIN_ALTITUDEMAX = 2;
        public static int MAX_ALTITUDEMAX = 99;
        public static float MIN_ANGLEMAX = 0;
        public static float MAX_ANGLEMAX = 30;

        public event PropertyChangedEventHandler PropertyChanged;

        private static SettingsModel instance;

        private SettingsModel()
        {
        }

        public static SettingsModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SettingsModel();
                }
                return instance;
            }
        }

        private string onOff(bool isOn)
        {
            return "/Assets/Buttons/" + (isOn ? "on" : "off") + ".png";
        }

        #region PathRecorder
        public bool PathRecorder
        {
            get
            {
                return settings.Contains("PathRecorder");
            }
            set
            {
                if (value)
                    settings["PathRecorder"] = Boolean.TrueString.ToString();
                else
                    settings.Remove("PathRecorder");
                NotifyPropertyChanged("PathRecorder");
                NotifyPropertyChanged("PathRecorderSource");
            }
        }

        public string PathRecorderSource
        {
            get
            {
                return onOff(PathRecorder);
            }
        }

        #endregion

        #region TiltToSteer
        public bool TiltToSteer
        {
            get
            {
                return settings.Contains("TiltToSteer");
            }
            set
            {
                if (value)
                    settings["TiltToSteer"] = Boolean.TrueString.ToString();
                else
                    settings.Remove("TiltToSteer");
                NotifyPropertyChanged("TiltToSteer");
                NotifyPropertyChanged("TiltToSteerSource");
            }
        }

        public string TiltToSteerSource
        {
            get
            {
                return onOff(TiltToSteer);
            }
        }

        #endregion

        #region SteerRelativeToYou
        public bool SteerRelativeToYou
        {
            get
            {
                return settings.Contains("SteerRelativeToYou");
            }
            set
            {
                if (value)
                    settings["SteerRelativeToYou"] = Boolean.TrueString.ToString();
                else
                    settings.Remove("SteerRelativeToYou");

                NotifyPropertyChanged("SteerRelativeToYou");
                NotifyPropertyChanged("SteerRelativeToYouSource");
            }
        }

        public string SteerRelativeToYouSource
        {
            get
            {
                return onOff(SteerRelativeToYou);
            }
        }

        #endregion

        #region OutdoorHull
        public bool OutdoorHull
        {
            get
            {
                return settings.Contains("OutdoorHull");
            }
            set
            {
                if (value)
                    settings["OutdoorHull"] = Boolean.TrueString.ToString();
                else
                    settings.Remove("OutdoorHull");
                NotifyPropertyChanged("OutdoorHull");
                NotifyPropertyChanged("OutdoorHullSource");
            }
        }

        public string OutdoorHullSource
        {
            get
            {
                return onOff(OutdoorHull);
            }
        }

        #endregion

        #region OutdoorFlight
        public bool OutdoorFlight
        {
            get
            {
                return settings.Contains("OutdoorFlight");
            }
            set
            {
                if (value)
                    settings["OutdoorFlight"] = Boolean.TrueString.ToString();
                else
                    settings.Remove("OutdoorFlight");
                NotifyPropertyChanged("OutdoorFlight");
                NotifyPropertyChanged("OutdoorFlightSource");
            }
        }

        public string OutdoorFlightSource
        {
            get
            {
                return onOff(OutdoorFlight);
            }
        }

        #endregion

        #region AltitudeMax
        public int AltitudeMax
        {
            get
            {
                if (settings.Contains("AltitudeMax"))
                {
                    return int.Parse(settings["AltitudeMax"].ToString());
                }
                else
                {
                    return MIN_ALTITUDEMAX;
                }
            }
            set
            {
                if (value < MIN_ALTITUDEMAX)
                    value = MIN_ALTITUDEMAX;
                else
                    if (value > MAX_ALTITUDEMAX)
                        value = MAX_ALTITUDEMAX;

                settings["AltitudeMax"] = value;
                NotifyPropertyChanged("AltitudeMax");
            }
        }

        #endregion

        #region AngleMax
        public float AngleMax
        {
            get
            {
                if (settings.Contains("AngleMax"))
                {
                    return float.Parse(settings["AngleMax"].ToString());
                }
                else
                {
                    return MIN_ANGLEMAX;
                }
            }
            set
            {
                if (value < MIN_ANGLEMAX)
                    value = MIN_ANGLEMAX;
                else
                    if (value > MAX_ANGLEMAX)
                        value = MAX_ANGLEMAX;

                settings["AngleMax"] = value;
                NotifyPropertyChanged("AngleMax");
            }
        }

        #endregion

        public void ApplyConfiguration(Drone drone)
        {
            /*
            drone.OutdoorFlightConfig = OutdoorFlight;
            drone.OutdoorHullConfig = OutdoorHull;
            drone.AltitudeMax = 1000 * AltitudeMax;
            drone.AngleMax = MathHelper.ToRadians(AngleMax);
             * */
        }

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

    }

}

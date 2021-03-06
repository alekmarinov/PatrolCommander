﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;

using ARDrone2Client.Common;
using ARDrone2Client.Common.Navigation;
using ARDrone2Client.Common.Input;

namespace ARDrone2Client.Common.Input
{
    public class DroneInputProvider : IInputProvider
    {
        private DroneClient _DroneClient = null;
        public DroneInputProvider(DroneClient droneClient, IJoystickControl rollPitchThumb, IJoystickControl yawGazThumb)
        {
            if (droneClient == null)
                throw new ArgumentNullException("DroneClient");
            _DroneClient = droneClient;
            _RollPitchThumb = rollPitchThumb;
            _YawGazThumb = yawGazThumb;
        }
        public DroneClient DroneClient
        {
            get
            {
                return _DroneClient;
            }
        }
        public string Name
        {
            get
            {
                return "Patrol Commander joystick";
            }
        }
        private IJoystickControl _RollPitchThumb;
        public IJoystickControl RollPitchThumb
        {
            get
            {
                return _RollPitchThumb;
            }
            set
            {
                if (value == _RollPitchThumb)
                    return;
                _RollPitchThumb = value;
            }
        }
        private IJoystickControl _YawGazThumb;
        public IJoystickControl YawGazThumb
        {
            get
            {
                return _YawGazThumb;
            }
            set
            {
                if (value == _YawGazThumb)
                    return;
                _YawGazThumb = value;
            }
        }
        public void Update()
        {
            float pitch = 0, roll = 0, yaw = 0, gaz = 0;
            if (_DroneClient == null)
                return;
            if (RollPitchThumb != null)
            {
                pitch = RollPitchThumb.Y;
                roll = RollPitchThumb.X;
            }
            if (YawGazThumb != null)
            {
                yaw = YawGazThumb.X;
                gaz = YawGazThumb.Y;
            }
            //Debug.WriteLine(string.Format("roll={0}, pitch={1}, yaw={2}, gaz={3}", roll, pitch, yaw, gaz));
            DroneClient.InputState.Update(roll, pitch, yaw, gaz);
        }
        public void Dispose()
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatrolCommander.Common
{
    class NavigationCommand
    {
        internal long when;
        internal double roll = 0;
        internal double pitch = 0;
        internal double yaw = 0;
        internal double gaz = 0;

        public NavigationCommand(long when, double roll, double pitch, double yaw, double gaz)
        {
            this.when = when;
            this.roll = roll;
            this.pitch = pitch;
            this.yaw = yaw;
            this.gaz = gaz;
        }
    }
}

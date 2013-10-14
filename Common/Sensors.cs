using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatrolCommander.Common
{
    class Sensors
    {
        internal long when;
        internal double rotation;
        internal double altitude;
        internal double velocityX;
        internal double velocityY;

        public Sensors(long when, double rotation, double altitude, double velocityX, double velocityY)
        {
            this.when = when;
            this.rotation = rotation;
            this.altitude = altitude;
            this.velocityX = velocityX;
            this.velocityY = velocityY;
        }
    }
}

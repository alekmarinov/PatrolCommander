using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Logger = CaledosLab.Portable.Logging.Logger;

namespace PatrolCommander.Common
{
    class AutoPilot
    {
        private List<NavigationCommand> navigationCommands;
        private Drone drone;
        private bool isPaused = false;
        private long pausedTime = 0;
        private long ticksPaused;
        private long ticksStarted;
        private DispatcherTimer timer = new DispatcherTimer();
        private int commandIndex = 0;

        public delegate void OnNavigationCommand(NavigationCommand navigationCommand);
        public event OnNavigationCommand onNavigationCommand;
        public delegate void OnMessageEvent(string message);
        public event OnMessageEvent OnMessage;

        public AutoPilot(Drone drone)
        {
            this.drone = drone;
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += OnTimerTick;
        }

        private void TriggerMessage(string message)
        {
            if (OnMessage != null)
                OnMessage(message);
        }

        private void OnTimerTick(Object sender, EventArgs args)
        {
            NavigationCommand nextCommand;
            if (isPaused)
                return;

            while (commandIndex < navigationCommands.Count)
            {
                nextCommand = navigationCommands.ElementAt(commandIndex);
                if (nextCommand.when < Elapsed())
                {
                    // Execute command
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        drone.DirectCommand(nextCommand.roll, nextCommand.pitch, nextCommand.yaw, nextCommand.gaz);
                        if (onNavigationCommand != null)
                            onNavigationCommand(nextCommand);
                    });
                    commandIndex++;
                }
                else
                {
                    break;
                }
            }

            if (commandIndex == navigationCommands.Count)
            {
                drone.Land();
                Stop();
            }
        }

        public long Elapsed()
        {
            if (ticksStarted > 0)
                return DateTime.Now.Ticks - ticksStarted - pausedTime;
            else
                return 0;
        }

        public bool Started()
        {
            return ticksStarted > 0;
        }

        public void Start(List<NavigationCommand> navigationCommands)
        {
            this.navigationCommands = navigationCommands;
            ticksStarted = DateTime.Now.Ticks;
            commandIndex = 0;
            pausedTime = 0;
            isPaused = false;
            timer.Start();
            TriggerMessage("Autopilot started with " + navigationCommands.Count + " queued commands");
        }


        public void Stop()
        {
            timer.Stop();
            ticksStarted = 0;
            isPaused = false;
            TriggerMessage("Autopilot stopped");
        }

        public void Pause()
        {
            TriggerMessage("Autopilot paused");
            isPaused = true;
            ticksPaused = DateTime.Now.Ticks;
        }

        public void Resume()
        {
            TriggerMessage("Autopilot resumed");
            isPaused = false;
            pausedTime += DateTime.Now.Ticks - ticksPaused;
        }

        public bool IsPaused()
        {
            return isPaused;
        }
    }
}

using CaledosLab.Portable.Logging;
using Microsoft.Phone.Net.NetworkInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PatrolCommander.Common
{
    class Wifi
    {
        private TextBlock wifiTextBlock;
        private FrameworkElement[] wifi = new FrameworkElement[5];
        private DispatcherTimer timer = new DispatcherTimer();

        public Wifi(TextBlock wifiName)
        {
            this.wifiTextBlock = wifiName;
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += OnTimerTick;
            updateStatus();
            DeviceNetworkInformation.NetworkAvailabilityChanged += DeviceNetworkInformation_NetworkAvailabilityChanged;
        }

        void DeviceNetworkInformation_NetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {
            updateStatus();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            updateStatus();
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        public string Name { get; set; }

        private void updateStatus()
        {
            NetworkInterfaceList networkInterfaceList = new NetworkInterfaceList();
            NetworkInterfaceInfo wifiInfo = null;
            foreach (NetworkInterfaceInfo netInfo in networkInterfaceList)
            {
                if (netInfo.InterfaceSubtype == NetworkInterfaceSubType.WiFi && netInfo.InterfaceState == ConnectState.Connected)
                {
                    wifiInfo = netInfo;
                    break;
                }
            }
            if (wifiInfo != null)
            {
                Name = wifiInfo.InterfaceName;
            }
            else
            {
                Name = "";
            }
            if (wifiTextBlock != null)
                wifiTextBlock.Text = Name;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Devices.Sensors;
using PatrolCommander.Model;
using PatrolCommander.Common;
using System.Collections;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using CaledosLab.Portable.Logging;
using ARDrone2Client.WP8.Video;
using HashTable = System.Collections.Generic.Dictionary<object, object>;
using System.Threading.Tasks;
using ARDrone2Client.Common.Input;
using Microsoft.Phone.Marketplace;
using Microsoft.Phone.Tasks;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using Microsoft.Xna.Framework.Media;

namespace PatrolCommander
{
    public partial class Main : PhoneApplicationPage
    {
        private ViewModel viewModel;
        private ScreenElement launchPadScreen;
        private ScreenElement cockpitScreen;
        private ScreenElement settingsScreen;
        private static LicenseInformation _licenseInfo = new LicenseInformation();

        private bool IsTrial
        {
#if DEBUG
            get { return true; }
#else
            get { return _licenseInfo.IsTrial(); }
#endif
        }

        #region LaunchPad Variables
        private Wifi wifiLaunchPad;
        #endregion

        #region Cockpit Variables
        private TranslateTransform dirHandlerActualTranslation = new TranslateTransform();
        private TranslateTransform dirHandlerExpectedTranslation = new TranslateTransform();
        private Manipulator directionsManipulator;
        private Manipulator compassManipulator;
        private Manipulator altitudeManipulator;
        private DroneCompass droneCompass;
        private PhoneCompass phoneCompass;
        private Altitude altitude;
        private Battery battery;
        private FlightTracker flightTracker = new FlightTracker();
        private Drone drone;
        private AutoPilot autoPilot;
        private DroneModel droneModel;
        private PlaneProjection projection = new PlaneProjection();
        private DispatcherTimer timer;
        private BitmapImage calibrateBmp = new BitmapImage();
        private BitmapImage compassBmp = new BitmapImage();
        private DisplayMessage displayMessage = new DisplayMessage();
        private TrajectoryRenderer trajectoryRenderer;
        private Accelerometer accelerometer;
        private bool allowMoveByTilt = false;
        private XYControl rollPitchThumb;
        private XYControl yawGazThumb;

        #endregion

        #region Settings Variables
        private Wifi wifiSettings;
        private Manipulator maxAltitudeManipulator;
        private Manipulator maxAngleManipulator;
        #endregion

        class XYControl : IJoystickControl
        {
            public XYControl()
            {
                X = Y = 0;
            }
            public float X { get; set; }
            public float Y { get; set; }
        }

        public Main()
        {
            viewModel = new ViewModel();

            InitializeComponent();

            #region LaunchPad Initialization
            wifiLaunchPad = new Wifi(WifiNameLaunchPad);

            new ActionButton(NewMission, NewMissionPushed, new Action(() =>
            {
                ScreenManager.Instance.Show(cockpitScreen, null);
            }));

            new ActionButton(SettingsButton, SettingsButtonPushed, new Action(() =>
            {
                ScreenManager.Instance.Show(settingsScreen, null);
            }));

            #endregion

            #region Cockpit Initialization
            directionsManipulator = new Manipulator(DirectionsHandler, DirectionsHandlerPushed, DirectionsHandlerShadow, Directions);
            directionsManipulator.PropertyChanged += directionsManipulator_PropertyChanged;
            compassManipulator = new Manipulator(CompassHandler, CompassHandlerPushed, CompassHandlerShadow, true, -220, 215);
            compassManipulator.PropertyChanged += compassManipulator_PropertyChanged;
            altitudeManipulator = new Manipulator(AltitudeHandler, AltitudeHandlerPushed, AltitudeHandlerShadow, false, -135, 135);
            altitudeManipulator.PropertyChanged += altitudeManipulator_PropertyChanged;
            droneCompass = new DroneCompass(HorizontalCompass, CompassZero);
            droneCompass.Value = 0;
            phoneCompass = new PhoneCompass(Directions);
            altitude = new Altitude(AltitudeMeter, CompassZero);
            altitude.Value = 0;
            battery = new Battery(BatteryImage, BatteryRect, BatteryText);
            rollPitchThumb = new XYControl();
            yawGazThumb = new XYControl();
            drone = Drone.Instance;
            drone.droneClient.InputProviders.Add(new DroneInputProvider(drone.droneClient, rollPitchThumb, yawGazThumb));
            drone.OnFlyStopStart += drone_OnFlyStopStart;

            droneModel = new DroneModel(drone);
            droneModel.PropertyChanged += droneModel_PropertyChanged;
            autoPilot = new AutoPilot(drone);
            autoPilot.onNavigationCommand += autoPilot_OnNavigationCommand;
            DroneIcon.Projection = projection;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += new EventHandler(timer_Tick);
            calibrateBmp.SetSource(Application.GetResourceStream(new Uri(@"Assets/Cockpit/Calibrate.png", UriKind.Relative)).Stream);
            compassBmp.SetSource(Application.GetResourceStream(new Uri(@"Assets/Cockpit/Compass.png", UriKind.Relative)).Stream);
            displayMessage.addMessageElement(Message1);
            displayMessage.addMessageElement(Message2);
            displayMessage.addMessageElement(Message3);
            displayMessage.addMessageElement(Message4);
            // drone.OnDroneMessage += ShowMessage;
            autoPilot.OnMessage += ShowMessage;
            flightTracker.OnMessage += ShowMessage;
            accelerometer = new Accelerometer();
            accelerometer.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);
            accelerometer.CurrentValueChanged += accelerometer_CurrentValueChanged;

            new ActionButton(TakeOff_Image, TakeOff_ImagePushed, new Action(async () =>
            {
                await TakeOffOrLand(true);
            }));

            new ActionButton(Land_Image, Land_ImagePushed, new Action(async () =>
            {
                await TakeOffOrLand(false);
            }));

            new ActionButton(Pause_Image, Pause_ImagePushed, new Action(() =>
            {
                PauseOrResume(true);
            }));

            new ActionButton(Continue_Image, Continue_ImagePushed, new Action(() =>
            {
                PauseOrResume(false);
            }));

            new ActionButton(Camera_Image, Camera_ImagePushed, new Action(() =>
            {
                // Switch camera
                drone.droneClient.SwitchVideoChannel();
                if (drone.droneClient.Configuration.Video.Channel.Value == ARDrone2Client.Common.Configuration.Sections.VideoChannelType.Horizontal)
                {
                    ShowMessage("Switching to horizontal camera");
                }
                else
                {
                    ShowMessage("Switching to vertical camera");
                }
            }));

            new ActionButton(Photo_Image, Photo_ImagePushed, new Action(() =>
            {
                string imageFileName = MissionManager.MakeMissionName(DateTime.Now) + ".jpg";
                WriteableBitmap bitmap = new WriteableBitmap(video, null);

                if (SavePhotoToImageHub(bitmap, imageFileName))
                {
                    ShowMessage("Image " + imageFileName + " saved");
                }
                else
                {
                    ShowMessage("Failed saving image " + imageFileName);
                }
            }));

            new ActionButton(Emergency_Image, Emergency_ImagePushed, new Action(() =>
            {
                if (drone.Emergency())
                {
                    ShowMessage("Emergency...");
                    stopFlying();
                }
            }));

            #endregion

            #region Settings Initialization
            wifiSettings = new Wifi(WifiNameSettings);
            maxAltitudeManipulator = new Manipulator(MaxAltitudeHandle, MaxAltitudeHandlePushed, null, true, 10, 365);
            maxAltitudeManipulator.Sticky = true;
            maxAltitudeManipulator.PropertyChanged += maxAltitudeManipulator_PropertyChanged;
            maxAngleManipulator = new Manipulator(MaxAngleHandle, MaxAngleHandlePushed, null, true, 10, 365);
            maxAngleManipulator.Sticky = true;
            maxAngleManipulator.PropertyChanged += maxAngleManipulator_PropertyChanged;
            MinMaxAltitude.Text = ViewModel.MIN_ALTITUDEMAX.ToString();
            MaxMaxAltitude.Text = ViewModel.MAX_ALTITUDEMAX.ToString();
            MinMaxAngle.Text = ViewModel.MIN_ANGLEMAX.ToString();
            MaxMaxAngle.Text = ViewModel.MAX_ANGLEMAX.ToString();
            maxAltitudeManipulator.Linear = -1 + 2 * ((double)viewModel.AltitudeMax - ViewModel.MIN_ALTITUDEMAX) / (ViewModel.MAX_ALTITUDEMAX - ViewModel.MIN_ALTITUDEMAX);
            maxAngleManipulator.Linear = -1 + 2 * (viewModel.AngleMax - ViewModel.MIN_ANGLEMAX) / (ViewModel.MAX_ANGLEMAX - ViewModel.MIN_ANGLEMAX);
            #endregion

            launchPadScreen = new ScreenElement(LaunchPad);
            cockpitScreen = new ScreenElement(Cockpit);
            settingsScreen = new ScreenElement(Settings);

            cockpitScreen.OnShow += cockpitScreen_OnShow;
            cockpitScreen.OnHide += cockpitScreen_OnHide;

            launchPadScreen.OnShow += launchPadScreen_OnShow;
            launchPadScreen.OnHide += launchPadScreen_OnHide;

            settingsScreen.OnShow += settingsScreen_OnShow;
            settingsScreen.OnHide += settingsScreen_OnHide;

            ScreenManager.Instance.Screens.Add(launchPadScreen);
            ScreenManager.Instance.Screens.Add(cockpitScreen);
            ScreenManager.Instance.Screens.Add(settingsScreen);

            ScreenManager.Instance.Show(launchPadScreen, null);


            MissionManager.Instance.LoadMissions(viewModel);

            DataContext = viewModel;
        }

        void launchPadScreen_OnShow()
        {
            wifiLaunchPad.Start();

            if (IsTrial)
            {
                if (ReviewBugger.IsTimeForReview())
                {
                    RateMessage.Visibility = Visibility.Visible;
                    TrialMessage.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TrialMessage.Visibility = Visibility.Visible;
                    RateMessage.Visibility = Visibility.Collapsed;
                }
                ContentPanel.Visibility = Visibility.Collapsed;
                ButtonsPanel.Visibility = Visibility.Collapsed;
                PageTitleLaunchPad.Visibility = Visibility.Collapsed;
            }
            else
            {
                TrialMessage.Visibility = Visibility.Collapsed;
                RateMessage.Visibility = Visibility.Collapsed;
                ContentPanel.Visibility = Visibility.Visible;
                ButtonsPanel.Visibility = Visibility.Visible;
                PageTitleLaunchPad.Visibility = Visibility.Visible;
            }
        }

        private void Button_ClickTry(object sender, RoutedEventArgs e)
        {
            if (ReviewBugger.IsTimeForReview())
            {
                ReviewBugger.NotNow();
            }

            TrialMessage.Visibility = Visibility.Collapsed;
            RateMessage.Visibility = Visibility.Collapsed;
            ContentPanel.Visibility = Visibility.Visible;
            ButtonsPanel.Visibility = Visibility.Visible;
            PageTitleLaunchPad.Visibility = Visibility.Visible;
        }

        private void Button_ClickBuy(object sender, RoutedEventArgs e)
        {
            new MarketplaceDetailTask().Show();
        }

        private void Button_ClickRate(object sender, RoutedEventArgs e)
        {
            new MarketplaceReviewTask().Show();
        }

        void launchPadScreen_OnHide()
        {
            wifiLaunchPad.Stop();
        }

        void settingsScreen_OnShow()
        {
            wifiSettings.Start();
        }

        void settingsScreen_OnHide()
        {
            wifiSettings.Stop();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private async void MainLoaded(object sender, RoutedEventArgs e)
        {
            trajectoryRenderer = new TrajectoryRenderer(TrajectoryCanvas, 550, 250);
            if (await ConnectDrone(false))
            {
                video.SetSource(new ARDroneStreamSource("192.168.1.1"));
            }
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (object sender2, EventArgs e2) =>
            {
                if (Application.Current.RootVisual != null)
                {
                    ChangeOrientation(((PhoneApplicationFrame)Application.Current.RootVisual).Orientation);
                    timer.Stop();
                }
            };
            timer.Start();
        }

        private void MainUnloaded(object sender, RoutedEventArgs e)
        {
            drone.Disconnect();
        }

        void cockpitScreen_OnShow()
        {
            trajectoryRenderer = new TrajectoryRenderer(TrajectoryCanvas, 550, 250);
            flightTracker.NewMission();
            phoneCompass.Start();
            timer.Start();
            accelerometer.Start();

            if (cockpitScreen.param != null)
            {
                if (cockpitScreen.param.ContainsKey("mission"))
                {
                    CockpitMission(cockpitScreen.param["mission"].ToString(), cockpitScreen.param["title"].ToString());
                }
                else if (cockpitScreen.param.ContainsKey("calibration"))
                {
                    CockpitCalibrate();
                }
            }
        }

        void cockpitScreen_OnHide()
        {
            accelerometer.Stop();
            timer.Stop();
            phoneCompass.Stop();
        }

        private async void CockpitCalibrate()
        {
            // start calibration
            AutoPilot calibrateAutoPilot = new AutoPilot(Drone.Instance);
            List<NavigationCommand> navCommands = new List<NavigationCommand>();
            navCommands.Add(new NavigationCommand(10 * 10000000, 0, 0, 0, 0));
            calibrateAutoPilot.Start(navCommands);

            ShowMessage("Flat trim");
            Drone.Instance.FlatTrim();
            await TakeOffOrLand(true);
            ShowMessage("Calibrate");
            Drone.Instance.Calibrate();
        }

        private void CockpitMission(string missionName, string missionTitle)
        {
            flightTracker.LoadMission(missionName, missionTitle);
            trajectoryRenderer.StartPath(new SolidColorBrush(Colors.Black), new SolidColorBrush(Colors.White));
            flightTracker.RenderTrajectory(trajectoryRenderer);
            trajectoryRenderer.EndPath();
        }

        #region LaunchPad

        private void MissionStart_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var missionItem = menuItem.CommandParameter as MissionItem;

            HashTable hashTable = new HashTable();
            hashTable.Add("mission", missionItem.Name);
            hashTable.Add("title", missionItem.Title);
            ScreenManager.Instance.Show(cockpitScreen, hashTable);
        }

        private void MissionStart_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Image image = e.OriginalSource as Image;
            var missionItem = image.Tag as MissionItem;

            HashTable hashTable = new HashTable();
            hashTable.Add("mission", missionItem.Name);
            hashTable.Add("title", missionItem.Title);
            ScreenManager.Instance.Show(cockpitScreen, hashTable);
        }

        private void MissionDelete_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var missionItem = menuItem.CommandParameter as MissionItem;
            viewModel.MissionItems.Remove(missionItem);
            MissionManager.Instance.DeleteMission(missionItem);
        }

        #endregion

        #region Cockpit

        void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            Dispatcher.BeginInvoke(() => updateAccelerometerUI(e.SensorReading));
        }

        private void updateAccelerometerUI(AccelerometerReading accelerometerReading)
        {
            Microsoft.Xna.Framework.Vector3 acceleration = accelerometerReading.Acceleration;
            // Logger.WriteLine("acceleration: X=" + acceleration.X + ", Y=" + acceleration.Y + ", Z=" + acceleration.Z);
            double tiltMin = 0.1;
            double tiltMax = 0.9;

            double moveX = -acceleration.Y;
            if (moveX < -tiltMax)
                moveX = -tiltMax;
            else if (moveX > tiltMax)
                moveX = tiltMax;
            else if (moveX < tiltMin && moveX > -tiltMin)
                moveX = 0;

            double moveY = -acceleration.X;
            if (moveY < -tiltMax)
                moveY = -tiltMax;
            else if (moveY > tiltMax)
                moveY = tiltMax;
            else if (moveY < tiltMin && moveY > -tiltMin)
                moveY = 0;

            if (moveX > 0)
                moveX -= tiltMin;
            else if (moveX < 0)
                moveX += tiltMin;

            if (moveY > 0)
                moveY -= tiltMin;
            else if (moveY < 0)
                moveY += tiltMin;

            // X.Text = acceleration.Z.ToString(); // moveX.ToString();
            // Y.Text = moveY.ToString();
            if (allowMoveByTilt)
            {
                MoveDrone(new Polar(moveX, moveY));
                directionsManipulator.MoveShadowCircular(moveX, moveY);
            }
        }

        private void autoPilot_OnNavigationCommand(NavigationCommand navigationCommand)
        {
            // Logger.WriteLine("autoPilot_OnNavigationCommand: {0} {1} {2} {3}", navigationCommand.roll, navigationCommand.pitch, navigationCommand.yaw, navigationCommand.gaz);
            directionsManipulator.MoveShadowCircular(navigationCommand.roll, navigationCommand.pitch);
            compassManipulator.MoveShadowLinear(navigationCommand.yaw);
            altitudeManipulator.MoveShadowLinear(-navigationCommand.gaz);
        }

        private async void timer_Tick(object sender, EventArgs e)
        {

            // Update battery status
            battery.Value = droneModel.Battery;

            // Phone compass calibration
            if (phoneCompass.NeedCalibration)
            {
                if (Directions.Source != calibrateBmp)
                    Directions.Source = calibrateBmp;
                Directions.Margin = new Thickness(0, 0, 0, 0);
            }
            else
            {
                if (phoneCompass.Working)
                {
                    if (Directions.Source != compassBmp)
                        Directions.Source = compassBmp;
                    Directions.Margin = new Thickness(-3, -3, 0, 0);
                }
            }

            // Watchdog connection
            bool needReconnect = drone.WatchDog();
            if (needReconnect)
            {
                await ConnectDrone(true);
            }

            DroneIcon.Visibility = drone.IsActive ? Visibility.Visible : Visibility.Collapsed;

            // Update TakeOff/Land button state
            if (drone.IsFlying())
            {
                TakeOff_Image.Visibility = Visibility.Collapsed;
                Land_Image.Visibility = Visibility.Visible;
                TakeOff_Landing.Text = "Land";
            }
            else
            {
                TakeOff_Image.Visibility = Visibility.Visible;
                Land_Image.Visibility = Visibility.Collapsed;
                if (flightTracker.Flyable())
                    TakeOff_Landing.Text = "Autopilot";
                else
                    TakeOff_Landing.Text = "Take Off";
            }

            // update flight time
            long elapsed = 0;

            if (autoPilot.Started())
                elapsed = autoPilot.Elapsed();
            else
                elapsed = flightTracker.Elapsed();
            DateTime timePassed = new DateTime(elapsed);
            TimeText.Text = String.Format("{0:D2}:{1:D2}.{2:D3}", timePassed.Minute, timePassed.Second, timePassed.Millisecond);
        }

        public void ShowMessage(string text)
        {
            displayMessage.ShowMessage(text);
        }

        private async Task<bool> ConnectDrone(bool isReconnect)
        {
            if (isReconnect)
            {
                ShowMessage("Reconnecting...");
                drone.Disconnect();
            }
            else
            {
                ShowMessage("Connecting to " + wifiSettings.Name);
            }

            bool connected = await drone.Connect();
            if (connected)
            {
                ShowMessage("Connected");
                return true;
            }
            else
            {
                ShowMessage("Unable to connect. Verify your wireless connection in settings.");
            }
            return false;
        }

        private void stopFlying()
        {
            if (flightTracker.Started())
            {
                flightTracker.EndRecording();
                MissionItem missionItem = flightTracker.SaveMission(260, 164);
                if (missionItem != null)
                    viewModel.MissionItems.Add(missionItem);
            }
            else
            {
                if (autoPilot.Started())
                {
                    autoPilot.Stop();
                }
            }
            trajectoryRenderer.EndPath();
            UpdatePauseButton();
        }

        void drone_OnFlyStopStart(bool isFlying)
        {
            if (isFlying)
            {
                if (flightTracker.Flyable())
                {
                    TrajectoryCanvas.Children.Clear();
                    TrajectoryCanvas.Width = 50;
                    TrajectoryCanvas.Height = 20;

                    trajectoryRenderer = new TrajectoryRenderer(TrajectoryCanvas, 550, 250);
                    trajectoryRenderer.StartPath(new SolidColorBrush(Colors.Black), new SolidColorBrush(Colors.White));
                    flightTracker.LoadMission();
                    flightTracker.RenderTrajectory(trajectoryRenderer);
                    trajectoryRenderer.EndPath();

                    autoPilot.Start(flightTracker.NavigationCommands);
                }
                else
                {
                    flightTracker.StartRecording();
                }
                trajectoryRenderer.StartPath(new SolidColorBrush(Colors.Green), new SolidColorBrush(Colors.White));
            }
            else
            {
                stopFlying();
            }
        }

        private async Task<bool> TakeOffOrLand(bool isTakeOff, int recurseCounter)
        {
            if (isTakeOff)
            {
                if (!drone.IsFlying())
                {
                    // ViewModel.Instance.ApplyConfiguration(drone);
                    if (drone.TakeOff())
                    {
                        Logger.WriteLine("****** TAKING OFF ******** (" + recurseCounter + ")");
                        return true;
                    }
                    else
                    {
                        Logger.WriteLine("****** CAN'T TAKE OFF ******** (" + recurseCounter + ")");
                        if (await ConnectDrone(true))
                        {
                            if (recurseCounter < 5)
                            {
                                await TakeOffOrLand(true, recurseCounter + 1);
                            }
                        }
                    }
                }
                else
                {
                    Logger.WriteLine("Land the drone before taking off");
                }
            }
            else
            {
                if (drone.IsFlying())
                {
                    if (drone.Land())
                    {
                        Logger.WriteLine("****** LANDING ********");
                        return true;
                    }
                    else
                    {
                        Logger.WriteLine("****** CAN'T LAND!!! ******** (" + recurseCounter + ")");
                        if (await ConnectDrone(true))
                        {
                            if (recurseCounter < 5)
                            {
                                await TakeOffOrLand(false, recurseCounter + 1);
                            }
                        }
                    }
                }
                else
                {
                    Logger.WriteLine("Take the drone off before land");
                }
            }
            return false;
        }

        private async Task<bool> TakeOffOrLand(bool isTakeOff)
        {
            return await TakeOffOrLand(isTakeOff, 0);
        }

        void droneModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Altitude":
                    altitude.Value = droneModel.Altitude;
                    break;
                case "Yaw":
                    // Logger.WriteLine("Yaw = " + droneModel.Yaw);
                    droneCompass.Value = droneModel.Yaw;
                    projection.RotationZ = droneModel.Yaw;
                    break;
                case "Roll":
                    double roll = droneModel.Roll;
                    projection.RotationY = -roll;
                    break;
                case "Pitch":
                    double pitch = droneModel.Pitch;
                    projection.RotationX = pitch;
                    break;
                case "Battery":
                    battery.Value = droneModel.Battery;
                    break;
                case "VelocityX":
                case "VelocityY":
                    double velocityX = droneModel.VelocityX;
                    double velocityY = droneModel.VelocityY;

                    // update flight activity
                    if (flightTracker.Started())
                    {
                        flightTracker.AddActivity(drone.Yaw, drone.Altitude, velocityX, velocityY);
                    }
                    if (trajectoryRenderer.Started)
                    {
                        trajectoryRenderer.addVelocity(velocityX, velocityY);
                    }
                    break;
            }
        }

        void directionsManipulator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        void compassManipulator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        void altitudeManipulator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void Cockpit_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Cockpit_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void MoveDrone(Polar polar)
        {
            if (viewModel.SteerRelativeToYou)
            {
                // Adjust direction according to local compass
                polar.Angle += phoneCompass.Value - droneCompass.Value;
            }

            Point direction = polar.getPoint();
            // drone.Move(direction.X, direction.Y);
            rollPitchThumb.X = (float)direction.X;
            rollPitchThumb.Y = (float)direction.Y;
            flightTracker.OperationMove(direction.X, direction.Y);
            PauseOrResume(true);
        }

        private void RotateDrone(double speed)
        {
            // drone.Rotate(speed);
            yawGazThumb.X = (float)speed;
            flightTracker.OperationRotate(speed);
            PauseOrResume(true);
        }

        private void AltitudeDrone(double speed)
        {
            // drone.UpDown(speed);
            yawGazThumb.Y = (float)speed;
            flightTracker.OperationUpDown(-altitudeManipulator.Linear);
            PauseOrResume(true);
        }

        private void Directions_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (!viewModel.TiltToSteer)
            {
                directionsManipulator.Translating(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);
                Polar polar = directionsManipulator.Polar;
                MoveDrone(polar);
            }
            /*
            else
            {
                Point directions = polar.getPoint();
                RotateDrone(directions.X);
                AltitudeDrone(-directions.Y);
            }
            */
        }

        private void Directions_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            // drone.Stop();
            if (!viewModel.TiltToSteer)
            {
                MoveDrone(new Polar(0, 0));
                flightTracker.OperationStop();
                directionsManipulator.Completed();
            }
        }

        private void Compass_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            compassManipulator.Translating(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);
            CompassMinHandler.Visibility = Visibility.Visible;
            CompassMaxHandler.Visibility = Visibility.Visible;
            RotateDrone(compassManipulator.Linear);
        }

        private void Compass_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            // drone.Stop();
            RotateDrone(0);
            flightTracker.OperationStop();
            compassManipulator.Completed();
            CompassMinHandler.Visibility = Visibility.Collapsed;
            CompassMaxHandler.Visibility = Visibility.Collapsed;
        }

        private void Altitude_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            altitudeManipulator.Translating(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);
            AltitudeMinHandler.Visibility = Visibility.Visible;
            AltitudeMaxHandler.Visibility = Visibility.Visible;
            AltitudeDrone(-altitudeManipulator.Linear);
        }

        private void Altitude_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            AltitudeDrone(0);
            // drone.Stop();
            flightTracker.OperationStop();
            altitudeManipulator.Completed();
            AltitudeMinHandler.Visibility = Visibility.Collapsed;
            AltitudeMaxHandler.Visibility = Visibility.Collapsed;
        }

        private void PauseOrResume(bool isPause)
        {
            if (autoPilot.Started())
            {
                if (isPause)
                {
                    if (!autoPilot.IsPaused())
                    {
                        autoPilot.Pause();
                        directionsManipulator.HideShadow();
                        altitudeManipulator.HideShadow();
                        compassManipulator.HideShadow();
                    }
                }
                else
                {
                    autoPilot.Resume();
                }
                UpdatePauseButton();
            }
        }

        private void UpdatePauseButton()
        {
            if (autoPilot.Started() && autoPilot.IsPaused())
            {
                Pause_Image.Visibility = Visibility.Collapsed;
                Continue_Image.Visibility = Visibility.Visible;
                Pause_Continue.Text = "Continue";
            }
            else
            {
                Pause_Image.Visibility = Visibility.Visible;
                Continue_Image.Visibility = Visibility.Collapsed;
                Pause_Continue.Text = "Pause";
            }
        }

        private void Image_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            allowMoveByTilt = viewModel.TiltToSteer;
        }

        private void Image_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            allowMoveByTilt = false;
            if (viewModel.TiltToSteer)
            {
                directionsManipulator.HideShadow();
                MoveDrone(new Polar(0, 0));
                flightTracker.OperationStop();
            }
        }
        #endregion

        #region Settings
        private void Tap_TiltToSteer(object sender, System.Windows.Input.GestureEventArgs e)
        {
            viewModel.TiltToSteer = !viewModel.TiltToSteer;
        }

        private void Tap_SteerRelativeToYou(object sender, System.Windows.Input.GestureEventArgs e)
        {
            viewModel.SteerRelativeToYou = !viewModel.SteerRelativeToYou;
        }

        private void Tap_OutdoorHull(object sender, System.Windows.Input.GestureEventArgs e)
        {
            viewModel.OutdoorHull = !viewModel.OutdoorHull;
        }

        private void Tap_OutdoorFlight(object sender, System.Windows.Input.GestureEventArgs e)
        {
            viewModel.OutdoorFlight = !viewModel.OutdoorFlight;
        }

        private void MaxAltitude_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            maxAltitudeManipulator.Translating(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);
        }

        private void MaxAltitude_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            maxAltitudeManipulator.Completed();
        }

        private void maxAltitudeManipulator_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            int maxAltitude = (int)Math.Floor(ViewModel.MIN_ALTITUDEMAX + (ViewModel.MAX_ALTITUDEMAX - ViewModel.MIN_ALTITUDEMAX) * (1 + maxAltitudeManipulator.Linear) / 2);
            MaxAltitudeValue.Text = String.Format("{0}", maxAltitude);
            viewModel.AltitudeMax = maxAltitude;
        }

        private void MaxAngle_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            maxAngleManipulator.Translating(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);
        }

        private void MaxAngle_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            maxAngleManipulator.Completed();
        }

        private void maxAngleManipulator_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            float maxAngle = (float)(ViewModel.MIN_ANGLEMAX + (ViewModel.MAX_ANGLEMAX - ViewModel.MIN_ANGLEMAX) * (1 + maxAngleManipulator.Linear) / 2);
            MaxAngleValue.Text = String.Format("{0:0.#}", maxAngle);
            viewModel.AngleMax = maxAngle;
        }

        private void Tap_PathRecorder(object sender, System.Windows.Input.GestureEventArgs e)
        {
            viewModel.PathRecorder = !viewModel.PathRecorder;
        }

        private void Tap_CalibrationFlight(object sender, System.Windows.Input.GestureEventArgs e)
        {
            HashTable hashTable = new HashTable();
            hashTable.Add("calibration", "1");
            ScreenManager.Instance.Show(cockpitScreen, hashTable);
        }
        #endregion

        private void PhoneApplicationPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            if (!ScreenManager.Instance.isScreen(launchPadScreen))
            {
                e.Cancel = true;
                ScreenManager.Instance.Show(launchPadScreen, null);
            }
        }

        private void ChangeOrientation(PageOrientation orientation)
        {
            double rotateAngle = 0;
            if (orientation == PageOrientation.LandscapeLeft)
                rotateAngle = 180;

            if (LayoutRoot != null)
            {
                RotateTransform transform = new RotateTransform();
                transform.CenterX = LayoutRoot.ActualWidth / 2;
                transform.CenterY = LayoutRoot.ActualHeight / 2;
                rotateAngle = (rotateAngle + 180) % 360;
                transform.Angle = rotateAngle;
                LayoutRoot.RenderTransform = transform;
            }
        }

        private void OnOrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            ChangeOrientation(e.Orientation);
        }

        private bool SavePhotoToImageHub(WriteableBitmap bmp, string fileName)
        {
            using (var mediaLibrary = new MediaLibrary())
            {
                using (var stream = new MemoryStream())
                {
                    bmp.SaveJpeg(stream, bmp.PixelWidth, bmp.PixelHeight, 0, 100);
                    stream.Seek(0, SeekOrigin.Begin);
                    var picture = mediaLibrary.SavePicture(fileName, stream);
                    if (picture.Name.Contains(fileName)) return true;
                }
            }
            return false;
        }
    }
}

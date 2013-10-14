using PatrolCommander.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using Logger = CaledosLab.Portable.Logging.Logger;

namespace PatrolCommander.Common
{
    class FlightTracker
    {
        private long ticksStarted = 0;
        private List<Sensors> activities = new List<Sensors>();
        private List<NavigationCommand> navigationCommands = new List<NavigationCommand>();
        private XDocument missionDoc;
        private XElement rootElement;
        private XElement activitiesElement;
        private DateTime missionDateTime;
        private string missionName;
        private string missionTitle;
        private Sensors prevActivity = null;
        private bool isPaused = false;
        private long ticksPaused;
        private long pausedTime = 0;

        public delegate void OnMessageEvent(string message);
        public event OnMessageEvent OnMessage;

        public FlightTracker()
        {
            NewMission();
        }

        public void NewMission()
        {
            ticksStarted = 0;
            activities = new List<Sensors>();
            navigationCommands = new List<NavigationCommand>();
            missionName = null;
            missionTitle = null;
            prevActivity = null;
            isPaused = false;
            pausedTime = 0;
            ticksPaused = 0;
        }

        private void TriggerMessage(string message)
        {
            if (OnMessage != null)
                OnMessage(message);
        }

        public List<NavigationCommand> NavigationCommands
        {
            get { return navigationCommands; }
        }

        public List<Sensors> Activities
        {
            get { return activities; }
        }


        public void LoadMission()
        {
            LoadMission(missionName, missionTitle);
        }

        public void LoadMission(string missionName, string missionTitle)
        {
            this.missionName = missionName;
            this.missionTitle = missionTitle;

            // Load XML
            IsolatedStorageFileStream xmlFileStream = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(missionName + @".xml", FileMode.Open);
            missionDoc = XDocument.Load(xmlFileStream);
            xmlFileStream.Close();

            XElement activitiesElement = missionDoc.Root.Element("activities");
            // parse navigation commands
            var items = from activityElement in activitiesElement.Elements("o") select activityElement.Value;
            navigationCommands.Clear();
            foreach (string item in items)
            {
                string[] elems = item.Split(new char[] { ' ' });
                navigationCommands.Add(new NavigationCommand(long.Parse(elems[0]), Double.Parse(elems[1]), Double.Parse(elems[2]), Double.Parse(elems[3]), Double.Parse(elems[4])));
            }

            // parse activities
            items = from activityElement in activitiesElement.Elements("a") select activityElement.Value;
            activities.Clear();
            foreach (string item in items)
            {
                string[] elems = item.Split(new char[] { ' ' });
                activities.Add(new Sensors(long.Parse(elems[0]), Double.Parse(elems[1]), Double.Parse(elems[2]), Double.Parse(elems[3]), Double.Parse(elems[4])));
            }

            TriggerMessage("Loaded mission since " + missionTitle);
        }

        public MissionItem SaveMission(int imageWidth, int imageHeight)
        {
            if (!SettingsModel.Instance.PathRecorder)
                return null;

            if (this.missionName == null)
            {
                Debug.WriteLine("Attempt to save undefined mission");
                return null;
            }

           
            // Saving XML
            IsolatedStorageFileStream xmlFileStream = IsolatedStorageFile.GetUserStoreForApplication().CreateFile(missionName + ".xml");
            missionDoc.Save(xmlFileStream);
            xmlFileStream.Close();

            Canvas canvas = new Canvas();
            ImageBrush imgBrush = new ImageBrush();
            BitmapImage bmp = new BitmapImage();
            bmp.SetSource(Application.GetResourceStream(new Uri(@"Assets/Missions/WhiteBoard.png", UriKind.Relative)).Stream);
            imgBrush.ImageSource = bmp;
            // canvas.Background = imgBrush;
            Image image = new Image();
            image.Source = bmp;
            // canvas.Children.Add(image);
            canvas.Width = 5;
            canvas.Height = 2;

            TrajectoryRenderer trajectoryRenderer = new TrajectoryRenderer(canvas, (int)Math.Floor(0.8 * imageWidth), (int)Math.Floor(0.8 * imageHeight));
            trajectoryRenderer.StartPath(new SolidColorBrush(Colors.Black), new SolidColorBrush(Colors.White));
            RenderTrajectory(trajectoryRenderer);
            trajectoryRenderer.EndPath();

            TransformGroup tg = new TransformGroup();
            tg.Children.Add(trajectoryRenderer.TransformGroup);
            TranslateTransform tt = new TranslateTransform();
            tt.X = imageWidth / 4;
            tt.Y = imageHeight / 4;
            tg.Children.Add(tt);

            WriteableBitmap wb = new WriteableBitmap(canvas, tg);
            Canvas canvas2 = new Canvas();
            Color bckColor = new Color();
            bckColor.R = 24;
            bckColor.G = 41;
            bckColor.B = 76;
            canvas2.Background = new SolidColorBrush(bckColor);
            Image image2 = new Image();
            image2.Source = wb;
            canvas2.Width = imageWidth;
            canvas2.Width = imageHeight;
            Canvas.SetLeft(canvas, 0.1 * imageWidth);
            Canvas.SetTop(canvas, 0.1 * imageHeight);
            canvas2.Children.Add(image);
            canvas2.Children.Add(canvas);

            WriteableBitmap wb2 = new WriteableBitmap(canvas2, null);

            // Save to IsolatedStorage
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            using (var writeStream = new IsolatedStorageFileStream(this.missionName + ".jpg", FileMode.Create, store))
            {
                wb2.SaveJpeg(writeStream, imageWidth, imageHeight, 0, 100);
            }
            TriggerMessage("Mission saved as " + missionTitle);
            MissionItem missionItem = new MissionItem(this.missionName);
            missionItem.When = this.missionDateTime;
            return missionItem;
        }

        public void RenderTrajectory(TrajectoryRenderer renderer)
        {
            foreach (Sensors sensors in activities)
            {
                renderer.addVelocity(sensors.velocityX, sensors.velocityY);
            }
        }

        public bool Flyable()
        {
            return navigationCommands.Count > 0;
        }

        public void StartRecording()
        {
            if (!SettingsModel.Instance.PathRecorder)
                return;
            isPaused = false;
            pausedTime = 0;
            missionDateTime = DateTime.Now;
            missionName = MissionManager.MakeMissionName(missionDateTime);
            missionTitle = MissionManager.MakeMissionTitle(missionDateTime);
            Logger.WriteLine("Start mission: " + missionName);
            ticksStarted = missionDateTime.Ticks;
            activities.Clear();
            navigationCommands.Clear();
            missionDoc = new XDocument();
            rootElement = new XElement("flight");
            missionDoc.Add(rootElement);
            rootElement.Add(new XElement("common",
                new XElement("datetime", missionDateTime), 
                new XElement("latitude", 0),
                new XElement("longitude", 0)));
            activitiesElement = new XElement("activities");
            rootElement.Add(activitiesElement);

            TriggerMessage("Flight recording started");
        }

        public void EndRecording()
        {
            if (!SettingsModel.Instance.PathRecorder)
                return;
            isPaused = false;
            pausedTime = 0;
            OperationStop();

            TriggerMessage("Flight recording stopped");
            ticksStarted = 0;
        }

        public bool Started()
        {
            return ticksStarted > 0;
        }

        public long Elapsed()
        {
            if (ticksStarted > 0)
                return DateTime.Now.Ticks - ticksStarted - pausedTime;
            else
                return 0;
        }

        public void Pause()
        {
            if (!SettingsModel.Instance.PathRecorder)
                return;
            TriggerMessage("Flight recording paused");
            isPaused = true;
            ticksPaused = DateTime.Now.Ticks;
        }

        public void Resume()
        {
            if (!SettingsModel.Instance.PathRecorder)
                return;
            TriggerMessage("Flight recording resumed");
            isPaused = false;
            pausedTime += DateTime.Now.Ticks - ticksPaused;
        }

        public bool IsPaused()
        {
            return isPaused;
        }

        public void OperationMove(double roll, double pitch)
        {
            if (Started())
            {
                NavigationCommand o = new NavigationCommand(Elapsed(), roll, pitch, 0, 0);
                navigationCommands.Add(o);
                addOperationElement(o);
            }
        }

        public void OperationRotate(double speed)
        {
            if (Started())
            {
                NavigationCommand o = new NavigationCommand(Elapsed(), 0, 0, speed, 0);
                navigationCommands.Add(o);
                addOperationElement(o);
            }
        }

        public void OperationUpDown(double speed)
        {
            if (Started())
            {
                NavigationCommand o = new NavigationCommand(Elapsed(), 0, 0, 0, speed);
                navigationCommands.Add(o);
                addOperationElement(o);
            }
        }

        public void OperationStop()
        {
            if (Started())
            {
                NavigationCommand o = new NavigationCommand(Elapsed(), 0, 0, 0, 0);
                navigationCommands.Add(o);
                addOperationElement(o);
            }
        }

        public void AddActivity(double rotation, double altitude, double velocityX, double velocityY)
        {
            if (Started() && !IsPaused())
            {
                Sensors a = new Sensors(Elapsed(), rotation, altitude, velocityX, velocityY);
                activities.Add(a);
                addActivityElement(a);
            }
        }

        private void addActivityElement(Sensors a)
        {
            if (prevActivity == null || prevActivity.rotation != a.rotation || prevActivity.altitude != a.altitude || 
                prevActivity.velocityX != a.velocityX || prevActivity.velocityY != a.velocityY)
            {
                prevActivity = a;
                activitiesElement.Add(new XElement("a", String.Format("{0} {1} {2} {3} {4}", a.when, a.rotation, a.altitude, a.velocityX, a.velocityY)));
            }
        }

        private void addOperationElement(NavigationCommand o)
        {
            activitiesElement.Add(new XElement("o", String.Format("{0} {1} {2} {3} {4}", o.when, o.roll, o.pitch, o.yaw, o.gaz)));
        }
    }
}

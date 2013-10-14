using PatrolCommander.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Storage;

namespace PatrolCommander.Model
{
    class MissionItem : INotifyPropertyChanged
    {
        private string name;
        private DateTime when;
        private Image image;
        public event PropertyChangedEventHandler PropertyChanged;

        public MissionItem(string name)
        {
            this.name = name;

            // load image file
            image = new Image();

            // Load from IsolatedStorage
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string fileName = name + ".jpg";
                if (store.FileExists(fileName))
                {
                    using (var readStream = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.SetSource(readStream);

                        image.Source = bmp;
                    }
                }
                else
                {
                    BitmapImage defaultImage = new BitmapImage(new Uri("/Assets/Missions/WhiteBoard.png", UriKind.Relative));
                    image.Source = defaultImage;
                }
            }
        }

        public string Name
        {
            get { return name; }
            set { name = value; NotifyPropertyChanged("name"); }
        }

        public DateTime When
        {
            get { return when; }
            set { when = value; NotifyPropertyChanged("when"); }
        }

        public string Title
        {
            get 
            {
                return MissionManager.MakeMissionTitle(When);
            }
        }

        public ImageSource ImageSource
        {
            get { return image.Source; }
        }

        public object Tag
        {
            get
            {
                return this;
            }
        }

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}

using PatrolCommander.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Logger = CaledosLab.Portable.Logging.Logger;

namespace PatrolCommander.Common
{
    class MissionManager
    {
        private static MissionManager instance;

        private MissionManager()
        {
        }

        public static MissionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MissionManager();
                }
                return instance;
            }
        }

        public void LoadMissions(ViewModel viewModel)
        {
            IsolatedStorageFile isoFile = IsolatedStorageFile.GetUserStoreForApplication();
            viewModel.MissionItems.Clear();
            foreach (string fileName in isoFile.GetFileNames())
            {
                string ext = Path.GetExtension(fileName);
                if (@".xml".Equals(ext) && !String.Empty.Equals(Path.GetFileNameWithoutExtension(fileName)))
                {
                    IsolatedStorageFileStream xmlFileStream = isoFile.OpenFile(fileName, FileMode.Open);
                    XDocument missionDoc = XDocument.Load(xmlFileStream);
                    xmlFileStream.Close();
                    XElement commonElement = missionDoc.Descendants("common").First();
                    XElement dateTimeElement = commonElement.Descendants("datetime").First();
                    DateTime dateTime = DateTime.Parse(dateTimeElement.Value);
                    MissionItem missionItem = new MissionItem(MakeMissionName(dateTime));
                    missionItem.When = dateTime;
                    Logger.WriteLine("MissionManager.LoadMissions: Loaded " + missionItem.Name);
                    viewModel.MissionItems.Add(missionItem);
                }
            }
        }

        public void DeleteMission(MissionItem missionItem)
        {
            IsolatedStorageFile isoFile = IsolatedStorageFile.GetUserStoreForApplication();
            isoFile.DeleteFile(missionItem.Name + @".xml");
            isoFile.DeleteFile(missionItem.Name + @".jpg");
        }

        public static string MakeMissionName(DateTime when)
        {
            return String.Format("{0:yyyyMMdd_HHmmss}", when);
        }

        public static string MakeMissionTitle(DateTime when)
        {
            return String.Format("{0:yy-MM-dd HH:mm}", when);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashTable = System.Collections.Generic.Dictionary<object, object>;

namespace PatrolCommander
{
    class ScreenManager
    {
        public List<ScreenElement> Screens = new List<ScreenElement>();
        public ScreenElement currentScreen;
        
        private static ScreenManager instance;

        private ScreenManager()
        {
        }

        public static ScreenManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ScreenManager();
                }
                return instance;
            }
        }

        public void Show(ScreenElement screen, HashTable param)
        {
            if (currentScreen != null)
                currentScreen.Hide();
            screen.Show(param);
            currentScreen = screen;
        }

        public bool isScreen(ScreenElement screen)
        {
            return currentScreen.Equals(screen);
        }
    }
}

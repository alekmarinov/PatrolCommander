using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using HashTable = System.Collections.Generic.Dictionary<object, object>;

namespace PatrolCommander
{
    class ScreenElement
    {
        private FrameworkElement frameworkElement;

        public delegate void OnShowHide();

        public event OnShowHide OnShow;
        public event OnShowHide OnHide;
        public HashTable param;

        public ScreenElement(FrameworkElement frameworkElement)
        {
            this.frameworkElement = frameworkElement;
            frameworkElement.Visibility = Visibility.Collapsed;
        }

        virtual public void Show(HashTable param)
        {
            frameworkElement.Visibility = Visibility.Visible;
            this.param = param;
            if (OnShow != null)
                OnShow();
        }

        virtual public void Hide()
        {
            frameworkElement.Visibility = Visibility.Collapsed;
            if (OnHide != null)
                OnHide();
        }
    }
}

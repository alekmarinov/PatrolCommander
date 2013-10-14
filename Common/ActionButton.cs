using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PatrolCommander.Common
{
    public class ActionButton
    {
        private FrameworkElement button;
        private FrameworkElement pushed;
        private Action callback;
        private int mouseLeavedTime = 0;

        public ActionButton(FrameworkElement button, FrameworkElement pushed, Action callback)
        {
            this.button = button;
            this.pushed = pushed;
            this.callback = callback;

            this.button.Tap += button_Tap;
            this.button.MouseEnter += button_MouseEnter;
            this.button.MouseLeave += button_MouseLeave;
        }

        void button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            callback.Invoke();
        }

        void button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (System.Environment.TickCount - mouseLeavedTime > 100)
                this.pushed.Visibility = Visibility.Visible;
        }

        void button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            mouseLeavedTime = System.Environment.TickCount;
            this.pushed.Visibility = Visibility.Collapsed;
        }
    }
}

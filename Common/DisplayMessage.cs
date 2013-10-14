using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PatrolCommander.Common
{
    class DisplayMessage
    {
        private List<TextBlock> messageElements = new List<TextBlock>();

        public DisplayMessage()
        { 
        }

        public void addMessageElement(TextBlock messageElement)
        {
            messageElements.Add(messageElement);
            messageElement.RenderTransform = new TranslateTransform();
        }

        public void ShowMessage(string messageText)
        {
            rotateElements();

            Storyboard storyboard = new Storyboard();
            Duration duration = new Duration(TimeSpan.FromSeconds(5));
            storyboard.Duration = duration;
            DoubleAnimation alphaAnimation = new DoubleAnimation();
            alphaAnimation.Duration = duration;
            storyboard.Children.Add(alphaAnimation);
            alphaAnimation.From = 1;
            alphaAnimation.To = 0;
            Storyboard.SetTarget(alphaAnimation, messageElements.ElementAt(0));
            Storyboard.SetTargetProperty(alphaAnimation, new PropertyPath(TextBlock.OpacityProperty));
            storyboard.Begin();

            TextBlock firstElement = messageElements.ElementAt(0);
            firstElement.Text = messageText;
        }

        void rotateElements()
        {  
            TextBlock wrapElement = messageElements.ElementAt(messageElements.Count - 1);
            messageElements.Insert(0, wrapElement);
            messageElements.RemoveAt(messageElements.Count - 1);

            Storyboard storyboard = new Storyboard();
            Duration duration = new Duration(TimeSpan.FromSeconds(1));
            storyboard.Duration = duration;

            for (int i = 0; i < messageElements.Count; i++)
            {
                DoubleAnimation translateAnimation = new DoubleAnimation();
                translateAnimation.Duration = duration;
                storyboard.Children.Add(translateAnimation);
                translateAnimation.From = (i - 1) * 55;
                translateAnimation.To = i * 55;
                Storyboard.SetTarget(translateAnimation, messageElements.ElementAt(i).RenderTransform);
                Storyboard.SetTargetProperty(translateAnimation, new PropertyPath(TranslateTransform.YProperty));
            }
            storyboard.Begin();
        }
    }
}

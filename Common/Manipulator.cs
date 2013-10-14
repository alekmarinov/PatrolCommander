using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PatrolCommander.Common
{
    public class Manipulator : INotifyPropertyChanged
    {
        private FrameworkElement element;
        private FrameworkElement pushed;
        private FrameworkElement relative;
        private FrameworkElement shadow;
        private BoundaryType boundaryType;
        private TranslateTransform actualTranslation = new TranslateTransform();
        private TranslateTransform expectedTranslation = new TranslateTransform();
        private TranslateTransform shadowTranslation = new TranslateTransform();
        private Storyboard storyboard;
        private Polar expectedPositionCircular = new Polar(0, 0);
        private double min = 0;
        private double max = 0;
        private double value = 0;

        public enum BoundaryType
        {
            CIRCULAR,
            HORIZONTAL,
            VERTICAL
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Polar Polar
        {
            get { return expectedPositionCircular; }
        }

        public double Linear
        {
            get { return value; }
            set { 
                SetLinearValue(value);
                NotifyPropertyChanged("Linear");
            }
        }

        public bool Sticky { get; set; }

        public Manipulator(FrameworkElement element, FrameworkElement pushed, FrameworkElement shadow, FrameworkElement relative)
        {
            this.element = element;
            this.pushed = pushed;
            this.relative = relative;
            this.shadow = shadow;
            element.RenderTransform = actualTranslation;
            pushed.RenderTransform = actualTranslation;
            if (shadow != null)
                shadow.RenderTransform = shadowTranslation;
            boundaryType = BoundaryType.CIRCULAR;
        }

        public Manipulator(FrameworkElement element, FrameworkElement pushed, FrameworkElement shadow, bool isHorizontal, double min, double max)
        {
            this.element = element;
            this.pushed = pushed;
            this.shadow = shadow;
            this.min = min;
            this.max = max;
            element.RenderTransform = actualTranslation;
            pushed.RenderTransform = actualTranslation;
            if (shadow != null)
                shadow.RenderTransform = shadowTranslation;
            boundaryType = isHorizontal ? BoundaryType.HORIZONTAL : BoundaryType.VERTICAL;
        }

        public void Translating(double deltaX, double deltaY)
        {
            //this.element.Visibility = Visibility.Collapsed;
            this.pushed.Visibility = Visibility.Visible;
            if (storyboard != null)
            {
                storyboard.Stop();
                storyboard = null;
            }

            switch (boundaryType)
            {
                case BoundaryType.CIRCULAR:
                    expectedTranslation.X += deltaX;
                    expectedTranslation.Y += deltaY;
                    ClipCircular();
                    break;
                case BoundaryType.HORIZONTAL:
                    expectedTranslation.X += deltaX;
                    ClipLinear();
                    break;
                case BoundaryType.VERTICAL:
                    expectedTranslation.Y += deltaY;
                    ClipLinear();
                    break;
            }
        }

        private void SetLinearValue(double value)
        {
            this.value = value;
            double translation = min + (max - min) * (value + 1) / 2;
            if (boundaryType == BoundaryType.HORIZONTAL)
                expectedTranslation.X = actualTranslation.X = translation;
            else
                expectedTranslation.Y = actualTranslation.Y = translation;
        }

        public void Completed()
        {
            this.pushed.Visibility = Visibility.Collapsed;
            if (Sticky)
                return;
            Duration duration = new Duration(TimeSpan.FromSeconds(0.2));
            storyboard = new Storyboard();
            storyboard.Duration = duration;
            DoubleAnimation paX = new DoubleAnimation();
            DoubleAnimation paY = new DoubleAnimation();
            paX.Duration = duration;
            paY.Duration = duration;
            storyboard.Children.Add(paX);
            storyboard.Children.Add(paY);
            paX.From = actualTranslation.X;
            paY.From = actualTranslation.Y;
            paX.To = 0;
            paY.To = 0;
            Storyboard.SetTarget(paX, actualTranslation);
            Storyboard.SetTarget(paY, actualTranslation);
            Storyboard.SetTargetProperty(paX, new PropertyPath(TranslateTransform.XProperty));
            Storyboard.SetTargetProperty(paY, new PropertyPath(TranslateTransform.YProperty));
            storyboard.Completed += AnimationCompleted;
            storyboard.Begin();
        }

        public void MoveShadowLinear(double position)
        {
            if (shadow != null)
                shadow.Visibility = (position != 0) ? Visibility.Visible : Visibility.Collapsed;
            double translation = min + (max - min) * (position + 1) / 2;
            if (boundaryType == BoundaryType.HORIZONTAL)
                shadowTranslation.X = translation;
            else
                shadowTranslation.Y = translation;
        }

        public void MoveShadowCircular(double posX, double posY)
        {
            if (shadow != null)
                shadow.Visibility = (posX != 0 || posY != 0) ? Visibility.Visible : Visibility.Collapsed;
            double allowedRadius = this.relative.ActualWidth / 2;
            shadowTranslation.X = posX * allowedRadius;
            shadowTranslation.Y = posY * allowedRadius;
        }

        public void HideShadow()
        {
            if (shadow != null)
                shadow.Visibility = Visibility.Collapsed;
        }

        private void AnimationCompleted(object sender, EventArgs e)
        {
            actualTranslation.X = actualTranslation.Y = 0;
            expectedTranslation.X = expectedTranslation.Y = 0;
        }

        private void ClipCircular()
        {
            double allowedRadius = this.relative.ActualWidth / 2;
            expectedPositionCircular = new Polar(expectedTranslation.X, expectedTranslation.Y);
            if (expectedPositionCircular.Radius <= allowedRadius)
            {
                actualTranslation.X = expectedTranslation.X;
                actualTranslation.Y = expectedTranslation.Y;
            }
            else
            {
                expectedPositionCircular.Radius = allowedRadius;
                Point cart = expectedPositionCircular.getPoint();
                actualTranslation.X = cart.X;
                actualTranslation.Y = cart.Y;
            }
            // normalize radius
            expectedPositionCircular.Radius = expectedPositionCircular.Radius / allowedRadius;
            NotifyPropertyChanged("Polar");
        }

        private void ClipLinear()
        {
            double translation = boundaryType == BoundaryType.HORIZONTAL ? expectedTranslation.X : expectedTranslation.Y;
            if (translation < min)
            {
                translation = min;
            }
            else if (translation > max)
            {
                translation = max;
            }
            if (boundaryType.Equals(BoundaryType.HORIZONTAL))
            {
                actualTranslation.X = translation;
            }
            else
            {
                actualTranslation.Y = translation;
            }

            // normalize position in range -1, 1
            value = -1 + 2 * (translation - min) / (max - min);
            NotifyPropertyChanged("Linear");
        }

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}

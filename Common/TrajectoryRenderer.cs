using CaledosLab.Portable.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PatrolCommander.Common
{
    class TrajectoryRenderer
    {
        private PathSegmentCollection pathSegmentCollection1;
        private PathSegmentCollection pathSegmentCollection2;
        private PathFigure pathFigure1;
        private PathFigure pathFigure2;
        private Path path1;
        private Path path2;
        private Canvas canvas;
        private double scale, minX, minY, maxX, maxY, ptX, ptY;
        private bool isStarted = false;
        private double strokeThickness;
        private int canvasWidth;
        private int canvasHeight;
        private TransformGroup transformGroup = new TransformGroup();

        public TrajectoryRenderer(Canvas canvas, int canvasWidth, int canvasHeight)
        {
            this.canvas = canvas;
            this.canvasWidth = canvasWidth;
            this.canvasHeight = canvasHeight;
            strokeThickness = 2;
            scale = 1;
            minX = minY = maxX = maxY;
        }

        public int CanvasWidth
        {
            get { return canvasWidth; }
        }

        public int CanvasHeight
        {
            get { return canvasHeight; }
        }

        public void StartPath(Brush brush1, Brush brush2)
        {
            isStarted = true;
            pathFigure1 = new PathFigure();
            pathFigure2 = new PathFigure();
            pathSegmentCollection1 = new PathSegmentCollection();
            pathSegmentCollection2 = new PathSegmentCollection();
            pathFigure1.Segments = pathSegmentCollection1;
            pathFigure2.Segments = pathSegmentCollection2;
            PathFigureCollection pathFigureCollection1 = new PathFigureCollection();
            PathFigureCollection pathFigureCollection2 = new PathFigureCollection();
            pathFigureCollection1.Add(pathFigure1);
            pathFigureCollection2.Add(pathFigure2);
            PathGeometry pathGeometry1 = new PathGeometry();
            PathGeometry pathGeometry2 = new PathGeometry();
            pathGeometry1.Figures = pathFigureCollection1;
            pathGeometry2.Figures = pathFigureCollection2;
            path1 = new Path();
            path1.Stroke = brush1;
            path2 = new Path();
            path2.Stroke = brush2;

            DoubleCollection strokeDashArray1 = new DoubleCollection();
            strokeDashArray1.Add(1000 / scale);
            strokeDashArray1.Add(500 / scale);

            path1.StrokeDashArray = strokeDashArray1;

            DoubleCollection strokeDashArray2 = new DoubleCollection();
            strokeDashArray2.Add(1000 / scale);
            strokeDashArray2.Add(500 / scale);
            path2.StrokeDashArray = strokeDashArray2;

            path1.StrokeThickness = 2*strokeThickness;
            path1.Data = pathGeometry1;
            path2.StrokeThickness = strokeThickness;
            path2.Data = pathGeometry2;
            canvas.Children.Add(path1);
            canvas.Children.Add(path2);
            ptX = ptY = 0;
        }

        public void addVelocity(double X, double Y)
        {
            if (!Started) return;
            LineSegment lineSegment1 = new LineSegment();
            LineSegment lineSegment2 = new LineSegment();
            ptX += X;
            ptY += Y;
            lineSegment1.Point = new Point(ptX, ptY);
            lineSegment2.Point = new Point(ptX, ptY);
            if (pathFigure1.StartPoint == null)
            {
                pathFigure1.StartPoint = lineSegment1.Point;
                pathFigure2.StartPoint = lineSegment2.Point;
            }
            pathSegmentCollection1.Add(lineSegment1);
            pathSegmentCollection2.Add(lineSegment2);
            if (ptX < minX) { minX = ptX; transformCanvas(); }
            if (ptX > maxX) { maxX = ptX; transformCanvas(); }
            if (ptY < minY) { minY = ptY; transformCanvas(); }
            if (ptY > maxY) { maxY = ptY; transformCanvas(); }
        }

        public void EndPath()
        {
            isStarted = false;
        }

        public Boolean Started
        {
            get { return isStarted; }
        }

        public TransformGroup TransformGroup
        {
            get { return transformGroup; }
        }

        private void transformCanvas()
        {
            double width = maxX - minX;
            double height = maxY - minY;
            if (width > this.canvas.ActualWidth)
                this.canvas.Width = width;
            else
                width = this.canvas.ActualWidth;
            if (height > this.canvas.ActualHeight)
                this.canvas.Height = height;
            else
                height = this.canvas.ActualHeight;
            transformGroup = new TransformGroup();
            double scaleX = width > 0 ? this.canvasWidth / width : 1;
            double scaleY = height > 0 ? this.canvasHeight / height : 1;
            scale = Math.Max(scaleX, scaleY);
            ScaleTransform scaleTransform = new ScaleTransform();
            scaleTransform.ScaleX = scaleX;
            scaleTransform.ScaleY = scaleY;

            TranslateTransform translateTransform = new TranslateTransform();
            translateTransform.X = -minX;
            translateTransform.Y = -minY;
            strokeThickness = 2 / scale;
            path1.StrokeThickness = 2*strokeThickness;
            path2.StrokeThickness = strokeThickness;
            DoubleCollection strokeDashArray1 = new DoubleCollection();
            strokeDashArray1.Add(1000 / scale);
            strokeDashArray1.Add(500 / scale);
            path1.StrokeDashArray = strokeDashArray1;

            DoubleCollection strokeDashArray2 = new DoubleCollection();
            strokeDashArray2.Add(1000 / scale);
            strokeDashArray2.Add(500 / scale);
            path2.StrokeDashArray = strokeDashArray2;

            transformGroup.Children.Add(translateTransform);
            transformGroup.Children.Add(scaleTransform);

            canvas.RenderTransform = transformGroup;
        }
    }
}

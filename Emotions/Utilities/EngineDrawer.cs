﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Caliburn.Micro;
using Emotions.KinectTools.Tracking;
using Emotions.Modules.Engine;

namespace Emotions.Utilities
{
    class EngineDrawer : Canvas, IDisposable
    {
        private EngineInputFrame _engineInputFrame;
        private readonly Brush _brush = Brushes.Cyan;
        private readonly Pen _pen = new Pen(Brushes.Cyan, 1);

        public EngineDrawer()
        {
            IoC.Get<IEngineService>().Updated += EngineOnUpdated;
        }

        private void EngineOnUpdated(IEngineService engineService, EngineInputFrame engineInputFrame)
        {
            _engineInputFrame = engineInputFrame;
            Dispatcher.Invoke(InvalidateVisual);
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            if (_engineInputFrame != null && _engineInputFrame.FeaturePoints != null)
            {
                var faceModelGroup = new GeometryGroup();
                foreach (var point in _engineInputFrame.FeaturePoints)
                {
                    faceModelGroup.Children.Add(new EllipseGeometry(Project(point), 1, 1));
                }

                faceModelGroup.Children.Add(new EllipseGeometry(Project(_engineInputFrame.HeadPosition), 2, 2));
                faceModelGroup.Children.Add(new EllipseGeometry(Project(_engineInputFrame.ShoulderCenter), 2, 2));
                faceModelGroup.Children.Add(new EllipseGeometry(Project(_engineInputFrame.ShoulderLeft), 2, 2));
                faceModelGroup.Children.Add(new EllipseGeometry(Project(_engineInputFrame.ShoulderRight), 2, 2));
                drawingContext.DrawGeometry(_brush, _pen, faceModelGroup);
            }

            base.OnRender(drawingContext);
        }

        public void Dispose()
        {
            IoC.Get<IEngineService>().Updated -= EngineOnUpdated;
        }

        private Point Project(EngineInputFrame.Point3 point)
        {
            const double xf = 0.75f;
            const double yf = 1.1f;
            var x = point.X * (xf / point.Z);
            var y = point.Y * (yf / point.Z);

            return new Point(
                 x * ActualWidth + ActualWidth / 2, 
                -y * ActualHeight + ActualHeight / 2);
        }
    }
}

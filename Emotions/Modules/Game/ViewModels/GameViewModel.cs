﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Caliburn.Micro;
using Emotions.Modules.Kinect.ViewModels;
using Gemini.Framework;
using Gemini.Framework.Services;

namespace Emotions.Modules.Game.ViewModels
{
    [Export(typeof(GameViewModel))]
    public class GameViewModel : Document, IGameFrameProvider
    {
        class Circle
        {
            public readonly long CreationTime;
            public int TTL;
            public readonly bool IsGood;
            public readonly Ellipse Ellipse;
            private double _x;
            private double _y;

            public Circle(long time, Ellipse ellipse, int ttl, bool isgood)
            {
                CreationTime = time;
                Ellipse = ellipse;
                _x = Canvas.GetLeft(Ellipse);
                _y = Canvas.GetTop(Ellipse);
                TTL = ttl;
                IsGood = isgood;
            }

            public void Update(int delay)
            {
                TTL -= delay;
                Ellipse.Width += 2;
                Ellipse.Height += 2;
                Canvas.SetLeft(Ellipse, _x -= 1);
                Canvas.SetTop(Ellipse, _y -= 1);
            }
        }

        public event Action<object, GameFrame> GameFrameReady;

        private Canvas _canvas;
        private bool _autoRec;
        private Random _random;
        private int _scored;
        private int _failed;
        private int _missed;
        private int _missclicks;
        private int _reactionTime;
        private bool _showScoreboard;
        private int _totalScore;
        private readonly List<Circle> _circles = new List<Circle>();
        private readonly ILog _log = LogManager.GetLog(typeof(GameViewModel));
        private int _frame;
        private KinectOutputViewModel _kinectVm;
        private int _totalTime;

        // GAME PARAMS
        private const int FrameDelay = 1000 / 15;
        private const int MinSize = 20;
        private const int MaxSize = 50;
        private const int StartDelay= 1000;
        private const int TargetDelay = 100;
        private const int TTL = 1300; // Time to live in ms
        private const double GoodCirclePropability = 0.5;
        public const long TargetTime = 3 * 60 * 1000; // minutes * (sec/min) * (ms / min)

        
        public bool ShowScoreboard
        {
            get { return _showScoreboard; }
            set
            {
                _showScoreboard = value;
                NotifyOfPropertyChange(() => ShowScoreboard);
            }
        }
        
        public int TotalScore
        {
            get { return _totalScore; }
            set
            {
                _totalScore = value;
                NotifyOfPropertyChange(() => TotalScore);
            }
        }

        public int Missclicks
        {
            get { return _missclicks; }
            set
            {
                _missclicks = value;
                NotifyOfPropertyChange(() => Missclicks);
            }
        }

        public int Missed
        {
            get { return _missed; }
            set
            {
                _missed = value;
                NotifyOfPropertyChange(() => Missed);
            }
        }

        public int Failed
        {
            get { return _failed; }
            set
            {
                _failed = value;
                NotifyOfPropertyChange(() => Failed);
            }
        }

        public int Scored
        {
            get { return _scored; }
            set
            {
                _scored = value;
                NotifyOfPropertyChange(() => Scored);
            }
        }

        public int ReactionTime
        {
            get { return _reactionTime; }
            set
            {
                _reactionTime = value;
                NotifyOfPropertyChange(() => ReactionTime);
            }
        }

        public bool AutoRec
        {
            get { return _autoRec; }
            set
            {
                _autoRec = value;
                NotifyOfPropertyChange(() => AutoRec);
            }
        }

        public GameViewModel()
        {
            DisplayName = "Game";
            
        }

        public void OnCanvasLoaded(object sender, object context)
        {
            _canvas = sender as Canvas;
        }

        public void OnStartClicked(int n)
        {
            Scored = 0;
            Failed = 0;
            Missed = 0;
            Missclicks = 0;
            ReactionTime = 0;
            _frame = 0;
            _random = new Random(n * 1234576);

            if (AutoRec)
            {
                var shell = IoC.Get<IShell>();
                var kinectVm = shell.Documents.FirstOrDefault(
                            d => d is KinectOutputViewModel && ((KinectOutputViewModel) d).IsEngineEnabled);
                if (kinectVm == null)
                {
                    _log.Warn("Can't start recording, no active kinect viewers with engine tracking found");
                    return;
                }
                _kinectVm = (KinectOutputViewModel) kinectVm;
                _kinectVm.StartRecording(this);
            }
            ShowScoreboard = false;
            Task.Factory.StartNew(UpdateCycle);
        }
        
        private void SpawnCircle(long time)
        {
            var isGood = _random.NextDouble() < GoodCirclePropability;
            var ellipse = new Ellipse();

            var size = _random.Next(MinSize, MaxSize);
            ellipse.Width = size;
            ellipse.Height = size;
            ellipse.Fill = isGood ? Brushes.Blue : Brushes.Red;

            //TODO prevent overlapping
            Canvas.SetLeft(ellipse, _random.Next(0, (int)_canvas.Width - (int)ellipse.Width));
            Canvas.SetTop(ellipse, _random.Next(0, (int)_canvas.Height - (int)ellipse.Height));

            _circles.Add(new Circle(time, ellipse, TTL, isGood));
            _canvas.Children.Add(ellipse);            
        }

        private void UpdateCycle()
        {
            _totalTime = 0;
            var startFrameTime = DateTime.Now;
            var lastCircleSpawnTime = DateTime.Now;
            var spawnCicrleDelegate = new Action<long>(SpawnCircle);
            var updateCirclesDelegate = new Action<double, int>(UpdateCircles);

            while (_totalTime < TargetTime)
            {
                var progress = (double)_totalTime / TargetTime;
                var spawnDelay = StartDelay - (int)(progress * (StartDelay - TargetDelay));
                
                if (DateTime.Now.Subtract(lastCircleSpawnTime).Milliseconds > spawnDelay)
                {
                    _canvas.Dispatcher.Invoke(spawnCicrleDelegate, new object[] { _totalTime });
                    lastCircleSpawnTime = DateTime.Now;
                }

                _canvas.Dispatcher.Invoke(updateCirclesDelegate, new object[]{ progress, FrameDelay });
                startFrameTime = startFrameTime.Add(TimeSpan.FromMilliseconds(FrameDelay));
                Thread.Sleep(FrameDelay);
                _totalTime += FrameDelay;
            }

            if (_kinectVm != null && _kinectVm.IsRecording)
                _kinectVm.StopRecording();
            _canvas.Dispatcher.Invoke(RemoveCircles);
            TotalScore = Scored * 3 - Failed * 2 - Missed * 2 - Missclicks;
            ShowScoreboard = true;
        }

        private void UpdateCircles(double progress, int delta)
        {
            foreach (var circle in _circles.ToList())
            {
                circle.Update(delta);
                if (circle.TTL < 0)
                {
                    if (circle.IsGood)
                    {
                        Missed++;
                        if (GameFrameReady != null)
                            GameFrameReady(this, GetFrame());
                    }
                    _canvas.Children.Remove(circle.Ellipse);
                    _circles.Remove(circle);
                }
            }
        }

        private void RemoveCircles()
        {
            foreach (var circle in _circles.ToList())
            {
                _canvas.Children.Remove(circle.Ellipse);
                _circles.Remove(circle);
            }
        }

        public void OnCanvasMouseLeftButtonUp(object argsRaw)
        {
            var args = argsRaw as MouseButtonEventArgs;
            var circle = _circles.FirstOrDefault((e) => e.Ellipse.IsMouseOver);
            if (circle != null)
            {
                if (circle.IsGood)
                {
                    Scored += 1;
                    ReactionTime =(int)(_totalTime - circle.CreationTime);
                }
                else
                    Failed += 1;

                _canvas.Children.Remove(circle.Ellipse);
                _circles.Remove(circle);
            }
            else
            {
                Missclicks += 1;
            }

            if (GameFrameReady != null)
                GameFrameReady(this, GetFrame());
        }

        private GameFrame GetFrame()
        {
            var frame = new GameFrame()
            {
                Missed = Missed,
                FrameNumber = _frame++,
                Missclicks = Missclicks,
                Failed = Failed,
                ReactionTime = ReactionTime,
                Scored = Scored,
                Time = DateTime.Now.Ticks
            };

            return frame;
        }
    }
}

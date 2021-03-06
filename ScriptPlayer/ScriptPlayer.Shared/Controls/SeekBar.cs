﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ScriptPlayer.Shared.Converters;

namespace ScriptPlayer.Shared
{
    public class SeekBar : Control
    {
        public delegate void ClickedEventHandler(object sender, double relative, TimeSpan absolute, int downMoveUp);

        public static readonly DependencyProperty OverlayProperty = DependencyProperty.Register(
            "Overlay", typeof(Brush), typeof(SeekBar),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OverlayOpacityProperty = DependencyProperty.Register(
            "OverlayOpacity", typeof(Brush), typeof(SeekBar),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(TimeSpan), typeof(SeekBar),
            new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(SeekBar),
            new PropertyMetadata(default(TimeSpan), OnVisualPropertyChanged));

        public static readonly DependencyProperty HoverPositionProperty = DependencyProperty.Register(
            "HoverPosition", typeof(TimeSpan), typeof(SeekBar), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan HoverPosition
        {
            get { return (TimeSpan) GetValue(HoverPositionProperty); }
            set { SetValue(HoverPositionProperty, value); }
        }

        private bool _down;
        private Popup _popup;
        private TextBlock _txt;

        static SeekBar()
        {
            BackgroundProperty.OverrideMetadata(typeof(SeekBar),
                new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));
        }

        public SeekBar()
        {
            InitializePopup();
        }

        public Brush Overlay
        {
            get => (Brush) GetValue(OverlayProperty);
            set => SetValue(OverlayProperty, value);
        }

        public Brush OverlayOpacity
        {
            get => (Brush) GetValue(OverlayOpacityProperty);
            set => SetValue(OverlayOpacityProperty, value);
        }

        public TimeSpan Duration
        {
            get => (TimeSpan) GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public TimeSpan Progress
        {
            get => (TimeSpan) GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public event ClickedEventHandler Seek;

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SeekBar) d).InvalidateVisual();
        }

        private void InitializePopup()
        {
            _txt = new TextBlock
            {
                Padding = new Thickness(3)
            };

            BindText(_txt);

            Border border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.Black,
                Child = _txt
            };

            _popup = new Popup
            {
                IsOpen = false,
                Placement = PlacementMode.Top,
                PlacementTarget = this,
                Child = border
            };
        }

        private void BindText(TextBlock txt)
        {
            MultiBinding binding = new MultiBinding{ Converter = new SeekBarPositionConverter()};
            binding.Bindings.Add(new Binding { Source = this, Path = new PropertyPath(ProgressProperty) });
            binding.Bindings.Add(new Binding { Source = this, Path = new PropertyPath(DurationProperty) });
            binding.Bindings.Add(new Binding { Source = this, Path = new PropertyPath(HoverPositionProperty) });

            BindingOperations.SetBinding(txt, TextBlock.TextProperty, binding);

            //TimeSpan progress = (TimeSpan)values[0];
            //TimeSpan duration = (TimeSpan)values[1];
            //TimeSpan hoverposition = (TimeSpan)values[2];

            /*_txt.Text = Duration >= TimeSpan.FromHours(1) ?
                $"{absolutePosition.Hours:00}:{absolutePosition.Minutes:00}:{absolutePosition.Seconds:00}" :
                $"{absolutePosition.Minutes:00}:{absolutePosition.Seconds:00}";*/
        }

        protected override void OnRender(DrawingContext dc)
        {
            Rect rect = new Rect(new Point(0, 0), new Size(ActualWidth, ActualHeight));

            dc.PushClip(new RectangleGeometry(rect));

            dc.DrawRectangle(Background, null, rect);
            dc.PushOpacityMask(OverlayOpacity);

            dc.DrawRectangle(Overlay, null, rect);

            dc.Pop();

            if (Duration == TimeSpan.Zero) return;

            double linePosition = Progress.Divide(Duration) * ActualWidth;

            linePosition = Math.Round(linePosition - 0.5) + 0.5;

            dc.DrawLine(new Pen(Brushes.Black, 3), new Point(linePosition, 0), new Point(linePosition, ActualHeight));
            dc.DrawLine(new Pen(Brushes.White, 1), new Point(linePosition, 0), new Point(linePosition, ActualHeight));

            dc.Pop();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (Duration == TimeSpan.Zero) return;
            if (!_down) return;

            ReleaseMouseCapture();
            _down = false;
            Point p = e.GetPosition(this);

            if (IsMouseOver)
                UpdatePopup(p);
            else
                ClosePopup();

            double relativePosition = GetRelativePosition(p.X);
            TimeSpan absolutePosition = Duration.Multiply(relativePosition);

            OnSeek(relativePosition, absolutePosition, 2);
        }

        private void ClosePopup()
        {
            _popup.IsOpen = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point p = e.GetPosition(this);
            UpdatePopup(p);

            if (!_down) return;
            if (Duration == TimeSpan.Zero) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            double relativePosition = GetRelativePosition(p.X);
            TimeSpan absolutePosition = Duration.Multiply(relativePosition);

            OnSeek(relativePosition, absolutePosition, 1);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            UpdatePopup(e.GetPosition(this));
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (_down)
                return;

            ClosePopup();
        }

        private void UpdatePopup(Point point)
        {
            if (Duration == TimeSpan.Zero) return;

            double relativePosition = GetRelativePosition(point.X);
            TimeSpan absolutePosition = Duration.Multiply(relativePosition);

            HoverPosition = absolutePosition;

            _popup.IsOpen = true;
            _popup.HorizontalOffset = relativePosition * ActualWidth -
                                      ((FrameworkElement) _popup.Child).ActualWidth / 2.0;
        }

        private double GetRelativePosition(double x)
        {
            double progress = x / Math.Max(1, ActualWidth);
            progress = Math.Min(1.0, Math.Max(0, progress));
            return progress;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (Duration == TimeSpan.Zero) return;

            _down = true;
            CaptureMouse();

            Point p = e.GetPosition(this);
            UpdatePopup(p);

            double relativePosition = GetRelativePosition(p.X);
            TimeSpan absolutePosition = Duration.Multiply(relativePosition);

            OnSeek(relativePosition, absolutePosition, 0);
        }

        protected virtual void OnSeek(double relative, TimeSpan absolute, int downMoveUp)
        {
            Seek?.Invoke(this, relative, absolute, downMoveUp);
        }
    }
}
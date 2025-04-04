using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace RainningkeyProgram
{
    public class BarEffector
    {
        public class BarEffectInfo
        {
            public Rectangle Bar { get; set; } = null!;
            public DateTime StartTime { get; set; }
            public EventHandler RenderingHandler { get; set; } = null!;
        }

        private static readonly Dictionary<string, BarEffectInfo> activeBarEffects = new();
        private static readonly Queue<Rectangle> barPool = new();

        private static Rectangle GetBarFromPool()
        {

            if (barPool.Count > 0)
            {
                Console.WriteLine("풀에서 바 가져옴");
                var bar = barPool.Dequeue();
                bar.Visibility = Visibility.Visible;
                bar.Opacity = 1;
                return bar;
            }
            // 새로 생성
            return new Rectangle
            {
                Visibility = Visibility.Visible,
                Opacity = 1
            };
        }

        private static void ReturnBarToPool(Canvas canvas, Rectangle bar)
        {
            if (canvas.Children.Contains(bar))
                canvas.Children.Remove(bar);

            bar.BeginAnimation(UIElement.OpacityProperty, null);
            bar.BeginAnimation(Canvas.TopProperty, null);

            bar.Visibility = Visibility.Hidden;
            bar.Opacity = 0;
            barPool.Enqueue(bar);
        }


        public static void CreateAndStartBarEffect(Canvas canvas, Border block, KeyItem item, double top)
        {
            string barKey = item.Key + canvas.Name;
            if (activeBarEffects.ContainsKey(barKey))
                return;

            double initialBarHeight = 0;
            double left = Canvas.GetLeft(block);

            var bar = GetBarFromPool();
            bar.Width = block.Width;
            bar.Height = initialBarHeight;
            bar.Fill = new SolidColorBrush(item.RainColor);

            Canvas.SetLeft(bar, left);
            Canvas.SetTop(bar, top);

            if (!canvas.Children.Contains(bar))
            {
                canvas.Children.Add(bar);
            }
            Panel.SetZIndex(bar, item.ShowRainEffectInFront ? 1000 : -1);

            DateTime startTime = DateTime.Now;

            EventHandler renderingHandler = (s, e) =>
            {
                double elapsed = (DateTime.Now - startTime).TotalSeconds;
                double newHeight = initialBarHeight + elapsed * Constants._barUpwardSpeed;
                bar.Height = newHeight;
                double newTop = top - elapsed * Constants._barUpwardSpeed;
                Canvas.SetTop(bar, newTop);
            };
            CompositionTarget.Rendering += renderingHandler;

            activeBarEffects[barKey] = new BarEffectInfo
            {
                Bar = bar,
                StartTime = startTime,
                RenderingHandler = renderingHandler
            };
        }

        public static void AnimateBarAfterKeyRelease(Canvas canvas, string keyName)
        {
            string barKey = keyName + canvas.Name;
            if (activeBarEffects.TryGetValue(barKey, out var effect))
            {
                var bar = effect.Bar;
                CompositionTarget.Rendering -= effect.RenderingHandler;

                double currentTop = Canvas.GetTop(bar);
                double animationDurationSeconds = 1.0;
                double displacement = Constants._barUpwardSpeed * animationDurationSeconds;
                double newTop = currentTop - displacement;

                var moveAnimation = new DoubleAnimation
                {
                    From = currentTop,
                    To = newTop,
                    Duration = TimeSpan.FromSeconds(animationDurationSeconds),
                    FillBehavior = FillBehavior.HoldEnd
                };
                Storyboard.SetTarget(moveAnimation, bar);
                Storyboard.SetTargetProperty(moveAnimation, new PropertyPath("(Canvas.Top)"));

                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(0.5),
                    BeginTime = TimeSpan.FromSeconds(animationDurationSeconds),
                    FillBehavior = FillBehavior.HoldEnd
                };
                Storyboard.SetTarget(fadeOutAnimation, bar);
                Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));

                var storyboard = new Storyboard();
                storyboard.Children.Add(moveAnimation);
                storyboard.Children.Add(fadeOutAnimation);

                storyboard.Completed += (s, e) =>
                {
                    ReturnBarToPool(canvas, bar);
                };

                storyboard.Begin();
                activeBarEffects.Remove(barKey);
            }
        }
    }
}

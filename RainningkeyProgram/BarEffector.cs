using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RainningkeyProgram
{
    public class BarEffector
    {
        public class BarEffectInfo
        {
            public Rectangle Bar { get; set; } = null!;
            public DateTime StartTime { get; set; }
            public DispatcherTimer Timer { get; set; } = null!;
        }
        // 각 키에 대해 활성화된 바 효과 정보를 저장합니다.
        private static readonly System.Collections.Generic.Dictionary<string, BarEffectInfo> activeBarEffects = new();
        // 바 효과 생성: Bar는 블럭의 x 좌표와 동일하게, y는 DividerLine(스냅 적용된 값)에서 시작하도록 생성
        public static void CreateAndStartBarEffect(Canvas canvas, Border block, KeyItem item)
        {
            if(activeBarEffects.ContainsKey(item.Key + canvas.Name)) return;
            double outlineThickness = 2; // 필요에 따라 조정
            double initialBarHeight = 10;

            // X 좌표: 블럭의 왼쪽 좌표
            double left = Canvas.GetLeft(block);
            // y 좌표는 DividerLine의 위치 (스냅 적용)
            double dividerY = canvas.ActualHeight / 2;
            dividerY = Math.Round(dividerY / Constants.SnapSize) * Constants.SnapSize;
            double top = dividerY;

            // Bar의 너비는 블럭과 동일하게 (원하는 경우 outlineThickness 적용 가능)
            var bar = new Rectangle
            {
                Width = block.Width,
                Height = initialBarHeight,
                Fill = new SolidColorBrush(item.RainColor),
                Opacity = 0.8
            };

            Canvas.SetLeft(bar, left);
            Canvas.SetTop(bar, top);

            // 레인 효과 우선순위에 따라 z-index 설정
            if (item.ShowRainEffectInFront)
                Panel.SetZIndex(bar, 1000);
            else
                Panel.SetZIndex(bar, -1);

            canvas.Children.Add(bar);

            // DispatcherTimer 간격 16ms (약 60fps)로 부드럽게 업데이트
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            DateTime startTime = DateTime.Now;

            timer.Tick += (s, e) =>
            {
                double elapsed = (DateTime.Now - startTime).TotalSeconds;
                // 초당 _barUpwardSpeed 픽셀씩 증가
                double growth = elapsed * Constants._barUpwardSpeed;
                double newHeight = initialBarHeight + growth;
                bar.Height = newHeight;
                // Bar는 DividerLine에서 위로 이동: 새로운 top = dividerY - growth
                Canvas.SetTop(bar, top - growth);
            };

            timer.Start();

            activeBarEffects[item.Key + canvas.Name] = new BarEffectInfo
            {
                Bar = bar,
                StartTime = startTime,
                Timer = timer
            };
        }
        // 키가 떼어진 후, 2초 동안 _barUpwardSpeed에 따라 위로 이동 후 0.5초 페이드아웃 (DoubleAnimation 사용)
        public static void AnimateBarAfterKeyRelease(Canvas canvas,String keyName)
        {

            if (activeBarEffects.TryGetValue(keyName + canvas.Name, out var effect))
            {
                effect.Timer.Stop();
                var bar = effect.Bar;
                double currentTop = Canvas.GetTop(bar);
                double animationDuration = 1f; // 2초 동안 이동
                double displacement = Constants._barUpwardSpeed * animationDuration; // 2초 동안 이동할 거리
                double newTop = currentTop - displacement;

                var upwardAnimation = new DoubleAnimation
                {
                    From = currentTop,
                    To = newTop,
                    Duration = TimeSpan.FromSeconds(animationDuration),
                    FillBehavior = FillBehavior.Stop
                };

                upwardAnimation.Completed += (s, e) =>
                {
                    Canvas.SetTop(bar, newTop);
                    var fadeOutAnimation = new DoubleAnimation
                    {
                        From = bar.Opacity,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.5),
                        FillBehavior = FillBehavior.Stop
                    };
                    fadeOutAnimation.Completed += (s2, e2) =>
                    {
                        canvas.Children.Remove(bar);
                    };
                    bar.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
                };

                bar.BeginAnimation(Canvas.TopProperty, upwardAnimation);
                activeBarEffects.Remove(keyName + canvas.Name);
            }
            
        }
    }
}

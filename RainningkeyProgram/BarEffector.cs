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
        // 각 키의 바 효과 정보를 저장하는 클래스
        public class BarEffectInfo
        {
            public Rectangle Bar { get; set; } = null!;
            public DateTime StartTime { get; set; }
            public EventHandler RenderingHandler { get; set; } = null!;
        }

        // 활성화된 바 효과를 저장하는 딕셔너리 (키: item.Key + canvas.Name)
        private static readonly Dictionary<string, BarEffectInfo> activeBarEffects = new();

        // 키 누르는 동안 사각형(바)을 실시간으로 업데이트하는 메서드
        public static void CreateAndStartBarEffect(Canvas canvas, Border block, KeyItem item, double top)
        {
            if (activeBarEffects.ContainsKey(item.Key + canvas.Name))
                return;

            double initialBarHeight = 10;
            double left = Canvas.GetLeft(block);

            var bar = new Rectangle
            {
                Width = block.Width,
                Height = initialBarHeight,
                Fill = new SolidColorBrush(item.RainColor),
                Opacity = 1
            };

            Canvas.SetLeft(bar, left);
            Canvas.SetTop(bar, top);

            // 레인 효과 우선순위에 따라 z-index 설정
            if (item.ShowRainEffectInFront)
                Panel.SetZIndex(bar, 1000);
            else
                Panel.SetZIndex(bar, -1);

            canvas.Children.Add(bar);
            DateTime startTime = DateTime.Now;

            // Rendering 이벤트를 사용하여 매 프레임마다 바의 높이와 위치를 업데이트합니다.
            EventHandler renderingHandler = (s, e) =>
            {
                double elapsed = (DateTime.Now - startTime).TotalSeconds;
                // 키가 눌린 시간에 비례해 높이가 증가
                double newHeight = initialBarHeight + elapsed * Constants._barUpwardSpeed;
                bar.Height = newHeight;
                // 바는 동일한 속도로 위로 이동
                double newTop = top - elapsed * Constants._barUpwardSpeed;
                Canvas.SetTop(bar, newTop);
            };
            CompositionTarget.Rendering += renderingHandler;

            activeBarEffects[item.Key + canvas.Name] = new BarEffectInfo
            {
                Bar = bar,
                StartTime = startTime,
                RenderingHandler = renderingHandler
            };
        }

        // 키 릴리즈 시 호출되어 Rendering 업데이트를 중단하고 추가 애니메이션을 실행하는 메서드
        public static void AnimateBarAfterKeyRelease(Canvas canvas, string keyName)
        {
            if (activeBarEffects.TryGetValue(keyName + canvas.Name, out var effect))
            {
                var bar = effect.Bar;
                // Rendering 이벤트 핸들러 제거하여 업데이트 중단
                CompositionTarget.Rendering -= effect.RenderingHandler;

                double currentTop = Canvas.GetTop(bar);
                double animationDurationSeconds = 1.0; // 1초 동안 추가 이동
                double displacement = Constants._barUpwardSpeed * animationDurationSeconds;
                double newTop = currentTop - displacement;

                // 동일한 속도로 위로 이동하는 애니메이션
                var upwardAnimation = new DoubleAnimation
                {
                    From = currentTop,
                    To = newTop,
                    Duration = TimeSpan.FromSeconds(animationDurationSeconds),
                    FillBehavior = FillBehavior.Stop
                };

                upwardAnimation.Completed += (s, e) =>
                {
                    Canvas.SetTop(bar, newTop);
                    // 페이드 아웃 애니메이션 실행
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

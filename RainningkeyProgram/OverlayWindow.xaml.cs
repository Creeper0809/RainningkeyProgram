using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static RainningkeyProgram.MainWindow;

namespace RainningkeyProgram
{
    public partial class OverlayWindow : Window
    {
        private ObservableCollection<KeyItem> _layoutSettings;

        public OverlayWindow(ObservableCollection<KeyItem> layoutSettings)
        {
            InitializeComponent();
            _layoutSettings = layoutSettings;
            // 창이 완전히 로드된 후 Loaded 이벤트에서 블럭들을 추가합니다.
            Loaded += OverlayWindow_Loaded;
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var setting in _layoutSettings)
            {
                Border block = CreateBlock(setting);
                // KeyItem의 X, Y 값을 이용해 보더의 위치 설정
                Canvas.SetLeft(block, setting.x);
                Canvas.SetTop(block, setting.y);
                OverlayCanvas.Children.Add(block);
            }
        }

        private Border CreateBlock(KeyItem setting)
        {
            var block = new Border
            {
                Width = setting.Width * setting.sellSize,   // 셀 사이즈 40 기준
                Height = setting.Height * setting.sellSize,
                Background = new System.Windows.Media.SolidColorBrush(setting.BlockColor),
                BorderBrush = new System.Windows.Media.SolidColorBrush(setting.RainEffect ? setting.RainColor : System.Windows.Media.Colors.Gray),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Tag = setting,
                Child = new TextBlock
                {
                    Text = setting.Key,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = System.Windows.Media.Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                // 오버레이 모드에서는 블럭 이동 기능은 없으므로 드래그 이벤트는 등록하지 않습니다.
                IsHitTestVisible = true
            };

            return block;
        }

        // 빈 영역 클릭 시 창 이동
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}

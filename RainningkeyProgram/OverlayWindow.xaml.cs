using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using static RainningkeyProgram.MainWindow;

namespace RainningkeyProgram
{
    public partial class OverlayWindow : Window
    {
        private RawInputProcessor _rawInputProcessor = RawInputProcessor.GetInstance();
        private double _min_y = double.MaxValue;
        public OverlayWindow()
        {
            InitializeComponent();
            // 창이 완전히 로드된 후 Loaded 이벤트에서 블럭들을 추가합니다.
            Loaded += OverlayWindow_Loaded;

        }
        private void OnRawKeyDown(string keyName)
        {
            Dispatcher.Invoke(() =>
            {
                var matchedItem = Constants.keyItems.FirstOrDefault(item => item.Key == keyName);
                if (matchedItem != null)
                {
                    // 레인 효과가 활성화되어 있을 때만 효과를 생성
                    if (!matchedItem.RainEffect)
                        return;

                    var block = OverlayCanvas.Children.OfType<Border>()
                        .FirstOrDefault(b => (b.Tag as KeyItem)?.Key == matchedItem.Key);

                    if (block != null)
                    {
                        BarEffector.CreateAndStartBarEffect(OverlayCanvas, block, matchedItem,_min_y);
                    }
                }
            });
        }

        // 키 업 이벤트: 해당 키의 바 효과를 종료하는 대신, 2초 동안 _barUpwardSpeed에 따라 위로 이동 후 페이드아웃합니다.
        private void OnRawKeyUp(string keyName)
        {
            Dispatcher.Invoke(() =>
            {
                BarEffector.AnimateBarAfterKeyRelease(OverlayCanvas, keyName);
            });
        }
        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            foreach (var setting in Constants.keyItems)
            {
                Border block = CreateBlock(setting);
                if (setting.y < _min_y)
                    _min_y = setting.y;
                Canvas.SetLeft(block, setting.x);
                Canvas.SetTop(block, setting.y);
                OverlayCanvas.Children.Add(block);
            }
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwndSource.AddHook(_rawInputProcessor.WndProc);
            _rawInputProcessor.RegisterRawInputDevices(hwndSource.Handle);
            // 키 다운과 키 업 이벤트 구독
            _rawInputProcessor.KeyDown += OnRawKeyDown;
            _rawInputProcessor.KeyUp += OnRawKeyUp;
        }

        private void MenuItem_CloseOverlay_Click(object sender, RoutedEventArgs e)
        {
            // 메인 창을 생성하고 보여줍니다.
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            OverlayCanvas.Children.Clear();
            this.Close();
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

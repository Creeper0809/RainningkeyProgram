using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RainningkeyProgram
{
    public partial class MainWindow : Window
    {
        // KeyItem 클래스 (추가 옵션 포함)
        public class KeyItem
        {
            public double x { get; set; } = 0;
            public double y { get; set; } = 0;
            public string Key { get; set; } = string.Empty;
            public int Width { get; set; } = 1;
            public int Height { get; set; } = 1;
            public Color BlockColor { get; set; } = Colors.LightGray;
            public bool RainEffect { get; set; } = true;
            public Color RainColor { get; set; } = Colors.DeepSkyBlue;
            // 새 옵션: 레인 효과 표시 순서 (true: 앞, false: 뒤)
            public bool ShowRainEffectInFront { get; set; } = true;
        }

        private ObservableCollection<KeyItem> keyItems = new();
        private string? lastPressedKey = null;
        private RawInputProcessor rawInputProcessor = new();

        // 각 키에 대해 활성화된 바 효과 정보를 저장합니다.
        private readonly System.Collections.Generic.Dictionary<string, BarEffectInfo> activeBarEffects = new();

        private class BarEffectInfo
        {
            public Rectangle Bar { get; set; } = null!;
            public DateTime StartTime { get; set; }
            public DispatcherTimer Timer { get; set; } = null!;
        }

        // 드래그 관련 변수
        private Point dragOffset;
        private bool isDragging = false;
        private Border? draggedBlock = null;
        private const int SnapSize = 20;

        // 바 효과 속도: 픽셀/초 (키를 뗐을 때 2초 동안 이 속도로 위로 이동)
        private double _barUpwardSpeed = 100; // 기본값 100 픽셀/초

        public MainWindow()
        {
            InitializeComponent();
            KeyListView.ItemsSource = keyItems;

            GlobalCellSizeBox.TextChanged += GlobalCellSizeBox_TextChanged;
            GlobalCellSizeBox.KeyDown += GlobalCellSizeBox_KeyDown;
            this.Loaded += MainWindow_Loaded;
        }
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 선택된 탭이 있을 때만 실행
            if (sender is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
            {
                // 레이아웃이 완전히 로드된 후 포커스를 클리어하도록 약간의 딜레이 후 호출
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Keyboard.ClearFocus();
                }), DispatcherPriority.ApplicationIdle);
            }
        }
        // HwndSource 후크 등록 및 Raw Input 등록
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwndSource.AddHook(rawInputProcessor.WndProc);
            rawInputProcessor.RegisterRawInputDevices(hwndSource.Handle);
            // 키 다운과 키 업 이벤트 구독
            rawInputProcessor.KeyDown += OnRawKeyDown;
            rawInputProcessor.KeyUp += OnRawKeyUp;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.ClearFocus();
            CurrentKeyText.Text = "<None>";
        }

        private void GlobalCellSizeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(GlobalCellSizeBox.Text, out int newCellSize)) return;
            newCellSize = Math.Clamp(newCellSize, 1, 100);
            GlobalCellSizeBox.Text = newCellSize.ToString();
            foreach (var block in LayoutCanvas.Children.OfType<Border>())
            {
                if (block.Tag is KeyItem item)
                {
                    block.Width = item.Width * newCellSize;
                    block.Height = item.Height * newCellSize;
                }
            }
        }

        private void GlobalCellSizeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
            }
        }

        // LayoutCanvas 크기 변경 시 DividerLine 위치 업데이트 (스냅 적용)
        private void LayoutCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (LayoutCanvas.ActualHeight > 0 && LayoutCanvas.ActualWidth > 0)
            {
                double midY = LayoutCanvas.ActualHeight / 2;
                // 스냅 적용 (예: SnapSize 단위로 반올림)
                midY = Math.Round(midY / SnapSize) * SnapSize;
                DividerLine.X1 = 0;
                DividerLine.Y1 = midY;
                DividerLine.X2 = LayoutCanvas.ActualWidth;
                DividerLine.Y2 = midY;
            }
        }
        private List<KeyItem> GetLayoutSettings()
        {
            // LayoutCanvas에 있는 각 블럭의 설정(KeyItem)을 수집합니다.
            var settings = new List<KeyItem>();
            foreach (UIElement child in LayoutCanvas.Children)
            {
                if (child is Border border && border.Tag is KeyItem keyItem)
                {
                    // 기존 KeyItem을 그대로 사용하거나 복사본을 생성할 수 있습니다.
                    settings.Add(new KeyItem
                    {
                        Key = keyItem.Key,
                        Width = keyItem.Width,
                        Height = keyItem.Height,
                        BlockColor = keyItem.BlockColor,
                        RainEffect = keyItem.RainEffect,
                        RainColor = keyItem.RainColor,
                        ShowRainEffectInFront = keyItem.ShowRainEffectInFront
                    });
                }
            }
            return settings;
        }

        private void ApplyOverlayMode_Click(object sender, RoutedEventArgs e)
        {
            // 현재 레이아웃 설정을 가져옵니다.
            var layoutSettings = GetLayoutSettings();
            // 새 오버레이 창을 생성하면서 설정을 전달합니다.
            var overlay = new OverlayWindow(keyItems);
            overlay.Show();

            // 메인 창을 닫습니다.
            this.Close();
        }

        // 마지막 입력 키를 추가합니다.
        private void AddKeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(lastPressedKey)) return;
            if (!int.TryParse(GlobalCellSizeBox.Text, out int cellSize)) cellSize = 40;

            if (keyItems.All(item => item.Key != lastPressedKey))
            {
                var item = new KeyItem { Key = lastPressedKey };
                keyItems.Add(item);
                CurrentKeyText.Text = $"<추가됨: {lastPressedKey}>";

                var block = CreateBlock(item, cellSize);
                LayoutCanvas.Children.Add(block);

                double lowerMargin = 10;
                Canvas.SetLeft(block, 10);
                Canvas.SetTop(block, LayoutCanvas.ActualHeight - block.Height - lowerMargin);
            }
            else
            {
                MessageBox.Show($"{lastPressedKey} 키는 이미 추가되어 있습니다.");
            }

            lastPressedKey = null;
            AddKeyButton.IsEnabled = false;
        }


        // 블럭 생성: UI에 나타날 블럭(키 아이템) 생성
        private Border CreateBlock(KeyItem item, int cellSize)
        {
            var block = new Border
            {
                Width = item.Width * cellSize,
                Height = item.Height * cellSize,
                Background = new SolidColorBrush(item.BlockColor),
                BorderBrush = new SolidColorBrush(item.RainEffect ? item.RainColor : Colors.Gray),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Tag = item,
                Child = new TextBlock
                {
                    Text = item.Key,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            block.MouseLeftButtonDown += Block_MouseLeftButtonDown;
            block.MouseMove += Block_MouseMove;
            block.MouseLeftButtonUp += Block_MouseLeftButtonUp;

            var contextMenu = new ContextMenu();
            var menuItem = new MenuItem { Header = "설정 열기", Tag = item };
            menuItem.Click += OpenSettings_Click;
            contextMenu.Items.Add(menuItem);
            block.ContextMenu = contextMenu;

            return block;
        }

        // 키 다운 이벤트: 해당 키에 대한 바 효과를 시작합니다.
        private void OnRawKeyDown(string keyName)
        {
            Dispatcher.Invoke(() =>
            {
                lastPressedKey = keyName;
                CurrentKeyText.Text = $"<현재 입력: {lastPressedKey}>";
                AddKeyButton.IsEnabled = true;

                var matchedItem = keyItems.FirstOrDefault(item => item.Key == keyName);
                if (matchedItem != null)
                {
                    // 레인 효과가 활성화되어 있을 때만 효과를 생성
                    if (!matchedItem.RainEffect)
                        return;

                    var block = LayoutCanvas.Children.OfType<Border>()
                        .FirstOrDefault(b => (b.Tag as KeyItem)?.Key == matchedItem.Key);

                    if (block != null)
                    {
                        // 이미 바 효과가 실행 중이면 무시
                        if (!activeBarEffects.ContainsKey(keyName))
                        {
                            CreateAndStartBarEffect(block, matchedItem);
                        }
                    }
                }
            });
        }

        // 키 업 이벤트: 해당 키의 바 효과를 종료하는 대신, 2초 동안 _barUpwardSpeed에 따라 위로 이동 후 페이드아웃합니다.
        private void OnRawKeyUp(string keyName)
        {
            Dispatcher.Invoke(() =>
            {
                if (activeBarEffects.TryGetValue(keyName, out var effect))
                {
                    effect.Timer.Stop();
                    AnimateBarAfterKeyRelease(effect.Bar);
                    activeBarEffects.Remove(keyName);
                }
            });
        }

        // 바 효과 생성: Bar는 블럭의 x 좌표와 동일하게, y는 DividerLine(스냅 적용된 값)에서 시작하도록 생성
        private void CreateAndStartBarEffect(Border block, KeyItem item)
        {
            if (!int.TryParse(GlobalCellSizeBox.Text, out int cellSize))
                cellSize = 40;
            double outlineThickness = 2; // 필요에 따라 조정
            double initialBarHeight = 10;

            // X 좌표: 블럭의 왼쪽 좌표
            double left = Canvas.GetLeft(block);
            // y 좌표는 DividerLine의 위치 (스냅 적용)
            double dividerY = LayoutCanvas.ActualHeight / 2;
            dividerY = Math.Round(dividerY / SnapSize) * SnapSize;
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

            LayoutCanvas.Children.Add(bar);

            // DispatcherTimer 간격 16ms (약 60fps)로 부드럽게 업데이트
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            DateTime startTime = DateTime.Now;

            timer.Tick += (s, e) =>
            {
                double elapsed = (DateTime.Now - startTime).TotalSeconds;
                // 초당 _barUpwardSpeed 픽셀씩 증가
                double growth = elapsed * _barUpwardSpeed;
                double newHeight = initialBarHeight + growth;
                bar.Height = newHeight;
                // Bar는 DividerLine에서 위로 이동: 새로운 top = dividerY - growth
                Canvas.SetTop(bar, top - growth);
            };

            timer.Start();

            activeBarEffects[item.Key] = new BarEffectInfo
            {
                Bar = bar,
                StartTime = startTime,
                Timer = timer
            };
        }

        // 키가 떼어진 후, 2초 동안 _barUpwardSpeed에 따라 위로 이동 후 0.5초 페이드아웃 (DoubleAnimation 사용)
        private void AnimateBarAfterKeyRelease(Rectangle bar)
        {
            double currentTop = Canvas.GetTop(bar);
            double animationDuration = 2.0; // 2초 동안 이동
            double displacement = _barUpwardSpeed * animationDuration; // 2초 동안 이동할 거리
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
                    LayoutCanvas.Children.Remove(bar);
                };
                bar.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
            };

            bar.BeginAnimation(Canvas.TopProperty, upwardAnimation);
        }

        // 드래그 시작: 블럭은 하단 영역(DividerLine 아래)에서만 드래그 가능
        private void Block_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border block)
            {
                isDragging = true;
                draggedBlock = block;
                dragOffset = e.GetPosition(block);
                block.CaptureMouse();
            }
        }

        // 드래그 이동: 새로운 위치가 LayoutCanvas 하단 영역(DividerLine 아래)보다 위로 가지 않도록 제한
        private void Block_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && draggedBlock != null)
            {
                Point pos = e.GetPosition(LayoutCanvas);
                double newX = pos.X - dragOffset.X;
                double newY = pos.Y - dragOffset.Y;

                // 하단 영역 최소 Y값: DividerLine + 여백
                double dividerY = LayoutCanvas.ActualHeight / 2;
                double lowerBoundary = dividerY + 10; // 10픽셀 여백
                newY = Math.Clamp(newY, lowerBoundary, double.MaxValue);

                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    newX = Math.Round(newX / SnapSize) * SnapSize;
                    newY = Math.Round(newY / SnapSize) * SnapSize;
                }

                Canvas.SetLeft(draggedBlock, newX);
                Canvas.SetTop(draggedBlock, newY);

                if (draggedBlock.Tag is KeyItem keyItem)
                {
                    keyItem.x = newX;
                    keyItem.y = newY;
                }
            }
        }

        // 드래그 종료
        private void Block_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (draggedBlock != null)
            {
                draggedBlock.ReleaseMouseCapture();
                draggedBlock = null;
                isDragging = false;
            }
        }

        // 설정 창 호출 (BlockSettingsWindow에서 추가 옵션 포함)
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is KeyItem item)
            {
                var dlg = new BlockSettingsWindow(item.Width, item.Height, item.BlockColor, item.RainEffect, item.RainColor, item.ShowRainEffectInFront);
                dlg.Owner = this;
                if (dlg.ShowDialog() == true)
                {
                    item.Width = dlg.WidthValue;
                    item.Height = dlg.HeightValue;
                    item.BlockColor = dlg.BlockColor;
                    item.RainEffect = dlg.UseRainEffect;
                    item.RainColor = dlg.RainColor;
                    item.ShowRainEffectInFront = dlg.ShowRainEffectInFront;

                    if (!int.TryParse(GlobalCellSizeBox.Text, out int cellSize)) cellSize = 40;

                    var block = LayoutCanvas.Children.OfType<Border>()
                        .FirstOrDefault(b => (b.Tag as KeyItem)?.Key == item.Key);
                    if (block != null)
                    {
                        block.Width = item.Width * cellSize;
                        block.Height = item.Height * cellSize;
                        block.Background = new SolidColorBrush(item.BlockColor);
                        block.BorderBrush = new SolidColorBrush(item.RainEffect ? item.RainColor : Colors.Gray);
                    }
                }
            }
        }

        // 삭제 버튼 클릭: 리스트와 캔버스 모두에서 해당 블럭 제거
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is KeyItem item)
            {
                keyItems.Remove(item);
                var block = LayoutCanvas.Children.OfType<Border>()
                    .FirstOrDefault(b => (b.Tag as KeyItem)?.Key == item.Key);
                if (block != null) LayoutCanvas.Children.Remove(block);
            }
        }
    }
}

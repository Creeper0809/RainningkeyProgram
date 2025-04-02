using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RainningkeyProgram
{
    public partial class BlockSettingsWindow : Window
    {
        public int WidthValue { get; private set; }
        public int HeightValue { get; private set; }
        public Color BlockColor { get; private set; }
        public bool UseRainEffect { get; private set; }
        public Color RainColor { get; private set; }
        // 새 옵션: Rain Z-Index (true: Front, false: Back)
        public bool ShowRainEffectInFront { get; private set; }

        public BlockSettingsWindow(int width, int height, Color currentBlockColor, bool rainEffect, Color currentRainColor, bool currentShowRainEffectInFront = true)
        {
            InitializeComponent();
            WidthBox.Text = width.ToString();
            HeightBox.Text = height.ToString();
            RainEffectCheck.IsChecked = rainEffect;
            BlockColorCombo.SelectedIndex = GetColorIndex(currentBlockColor);
            RainColorCombo.SelectedIndex = GetColorIndex(currentRainColor);
            RainZIndexCombo.SelectedIndex = currentShowRainEffectInFront ? 0 : 1;
        }

        private int GetColorIndex(Color color)
        {
            if (color == Colors.Red) return 0;
            if (color == Colors.Orange) return 1;
            if (color == Colors.Yellow) return 2;
            if (color == Colors.Green) return 3;
            if (color == Colors.Blue) return 4;
            if (color == Colors.Indigo) return 5;
            if (color == Colors.Violet) return 6;
            return -1;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(WidthBox.Text, out int w) || !int.TryParse(HeightBox.Text, out int h))
            {
                MessageBox.Show("숫자를 정확히 입력하세요.");
                return;
            }
            string? blockColorName = (BlockColorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string? rainColorName = (RainColorCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            try
            {
                BlockColor = (Color)ColorConverter.ConvertFromString(blockColorName ?? "Gray");
                RainColor = (Color)ColorConverter.ConvertFromString(rainColorName ?? "Blue");
            }
            catch
            {
                MessageBox.Show("색상 변환 실패");
                return;
            }
            WidthValue = w;
            HeightValue = h;
            UseRainEffect = RainEffectCheck.IsChecked == true;
            ShowRainEffectInFront = (RainZIndexCombo.SelectedIndex == 0);
            DialogResult = true;
            Close();
        }
    }
}

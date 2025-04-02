using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RainningkeyProgram
{
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
        public int sellSize { get; set; } = 40;
    }
}

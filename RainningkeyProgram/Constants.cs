using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainningkeyProgram
{
    class Constants
    {
        public static double SnapSize = 20;
        public static double _barUpwardSpeed = 250;

        public static ObservableCollection<KeyItem> keyItems = new();
    }
}

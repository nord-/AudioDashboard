using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace AudioDashboard
{
    public abstract class FileBase
    {
        private string _name;
        private SolidColorBrush _backgroundColor = new SolidColorBrush(Colors.Black);

        public string Path { get; set; }
        public string Name
        {
            get => _name;
            set
            {
                var splittedName = value.Split('.');
                _name = splittedName[0];
                if (splittedName.Length == 2 && splittedName[1].IndexOf('#') >= 0)
                {
                    var c = System.Drawing.Color.FromArgb(int.Parse(splittedName[1].Replace("#", "FF"), NumberStyles.HexNumber));
                    _backgroundColor = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
                }
            }
        }
        public SolidColorBrush BackgroundColor { get => _backgroundColor; }
    }

    public class AudioFolder : FileBase
    {
        public List<AudioFile> Files { get; set; }
        public string FileCount { get => $"{Files.Count} filer"; }
    }

    public class AudioFile : FileBase
    {
        public string Extension { get; set; }
    }
}

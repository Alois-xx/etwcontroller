using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;


namespace ETWController.UI
{
    class MessageToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var txt = value.ToString().ToLower();
            if (txt.Contains("saved screenshot"))
            {
                return Brushes.LightGoldenrodYellow;
            }
            if (txt.Contains(": mouse button"))
            {
                return Brushes.AliceBlue;
            }
            if (txt.Contains(": keydown"))
            {
                return Brushes.LightBlue;
            }
            return Brushes.Snow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

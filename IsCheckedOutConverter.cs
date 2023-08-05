using System;
using System.Globalization;
using System.Windows.Data;

namespace TubeTowelAppWpf {
    public class IsCheckedOutConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (bool)value ? "Check In" : "Check Out";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException("Two-way conversion is not supported by this converter.");
        }
    }
}

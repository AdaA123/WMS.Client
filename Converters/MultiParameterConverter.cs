using System;
using System.Globalization;
using System.Windows.Data;

namespace WMS.Client.Converters
{
    public class MultiParameterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 直接返回数组，把前端的多个控件打包给 ViewModel
            return values.Clone();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
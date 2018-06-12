using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Data;

using CoreSipNet;

namespace RadioVoipSimV2.Views
{
    public class SessionConnectedViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CORESIP_CallState)
            {
                switch ((CORESIP_CallState)value)
                {
                    case CORESIP_CallState.CORESIP_CALL_STATE_CONFIRMED:
                        return true;
                    default:
                        return false;
                }
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }

    public class VisibilityConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return (bool)value == true ? Visibility.Visible : Visibility.Hidden;
            }
            throw new NotImplementedException();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                return (Visibility)value == Visibility.Hidden ? false : true;
            }

            throw new NotImplementedException();
        }
    }

    public class HideConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return (bool)value == false ? Visibility.Visible : Visibility.Hidden;
            }
            throw new NotImplementedException();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                return (Visibility)value == Visibility.Hidden ? true : false;
            }

            throw new NotImplementedException();
        }
    }
}

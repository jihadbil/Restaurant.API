using System.Windows;
using System.Windows.Media;

namespace Restaurant.Desktop.Core
{
    public static class IconBehavior
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached(
                "Icon",
                typeof(Geometry),
                typeof(IconBehavior),
                new PropertyMetadata(null));

        public static Geometry GetIcon(DependencyObject obj)
        {
            return (Geometry)obj.GetValue(IconProperty);
        }

        public static void SetIcon(DependencyObject obj, Geometry value)
        {
            obj.SetValue(IconProperty, value);
        }
    }
}

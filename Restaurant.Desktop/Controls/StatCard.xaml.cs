using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Restaurant.Desktop.Controls
{
    public partial class StatCard : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(StatCard), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(StatCard), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(StatCard), new PropertyMetadata(null));

        public static readonly DependencyProperty AccentBrushProperty =
            DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(StatCard), new PropertyMetadata(Brushes.Gray));

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register(nameof(BackgroundColor), typeof(Brush), typeof(StatCard), new PropertyMetadata(Brushes.LightGray));

        public static readonly DependencyProperty TrendValueProperty =
            DependencyProperty.Register(nameof(TrendValue), typeof(string), typeof(StatCard), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TrendIsPositiveProperty =
            DependencyProperty.Register(nameof(TrendIsPositive), typeof(bool), typeof(StatCard), new PropertyMetadata(true));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public Geometry Icon
        {
            get => (Geometry)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public Brush AccentBrush
        {
            get => (Brush)GetValue(AccentBrushProperty);
            set => SetValue(AccentBrushProperty, value);
        }

        public Brush BackgroundColor
        {
            get => (Brush)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        public string TrendValue
        {
            get => (string)GetValue(TrendValueProperty);
            set => SetValue(TrendValueProperty, value);
        }

        public bool TrendIsPositive
        {
            get => (bool)GetValue(TrendIsPositiveProperty);
            set => SetValue(TrendIsPositiveProperty, value);
        }

        public StatCard()
        {
            InitializeComponent();
        }
    }
}

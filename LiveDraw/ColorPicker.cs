using System.Windows;
using System.Windows.Media.Animation;

namespace AntFu7.LiveDraw
{
    internal class ColorPicker : ActivableButton
    {
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register("Size", typeof(ColorPickerButtonSize), typeof(ColorPicker), new PropertyMetadata(default(ColorPickerButtonSize), OnColorPickerSizeChanged));

        public ColorPickerButtonSize Size
        {
            get { return (ColorPickerButtonSize)GetValue(SizeProperty); }
            set { SetValue(SizeProperty, value); }
        }

        private static void OnColorPickerSizeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var v = (ColorPickerButtonSize)eventArgs.NewValue;
            if (dependencyObject is not ColorPicker obj)
            {
                return;
            }

            var w = 0.0;
            w = v switch
            {
                ColorPickerButtonSize.Small => (double)Application.Current.Resources["ColorPickerSmall"],
                ColorPickerButtonSize.Middle => (double)Application.Current.Resources["ColorPickerMiddle"],
                _ => (double)Application.Current.Resources["ColorPickerLarge"],
            };
            obj.BeginAnimation(WidthProperty, new DoubleAnimation(w, (Duration)Application.Current.Resources["Duration3"]));
        }
    }
}
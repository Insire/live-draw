using System.Windows;
using System.Windows.Media.Animation;

namespace AntFu7.LiveDraw
{
    internal sealed class ColorPickerButton : ActivatableButton
    {
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(nameof(Size), typeof(ColorPickerButtonSize), typeof(ColorPickerButton), new PropertyMetadata(default(ColorPickerButtonSize), OnColorPickerSizeChanged));

        public ColorPickerButtonSize Size
        {
            get => (ColorPickerButtonSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        private static void OnColorPickerSizeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var buttonSize = (ColorPickerButtonSize)eventArgs.NewValue;
            if (dependencyObject is not ColorPickerButton obj)
            {
                return;
            }

            var size = buttonSize switch
            {
                ColorPickerButtonSize.Small => (double)Application.Current.Resources["ColorPickerSmall"],
                ColorPickerButtonSize.Middle => (double)Application.Current.Resources["ColorPickerMiddle"],
                _ => (double)Application.Current.Resources["ColorPickerLarge"],
            };
            obj.BeginAnimation(WidthProperty, new DoubleAnimation(size, (Duration)Application.Current.Resources["Duration3"]));
        }
    }
}
using System.Windows;
using System.Windows.Controls;

namespace AntFu7.LiveDraw
{
    internal class ActivatableButton : Button
    {
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(ActivatableButton), new PropertyMetadata(default(bool)));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }
    }
}
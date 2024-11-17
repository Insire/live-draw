using System.Windows;
using System.Windows.Controls;

namespace AntFu7.LiveDraw
{
    internal class ActivatableButton : Button
    {
        public static readonly DependencyProperty IsActivedProperty = DependencyProperty.Register(nameof(IsActived), typeof(bool), typeof(ActivatableButton), new PropertyMetadata(default(bool)));

        public bool IsActived
        {
            get => (bool)GetValue(IsActivedProperty);
            set => SetValue(IsActivedProperty, value);
        }
    }
}
using AeonHacs.Wpf.Converters;
using AeonHacs.Wpf.Views;
using System.ComponentModel;
using System.Windows;

namespace CegsMines.Views;

/// <summary>
/// Interaction logic for FTG.xaml
/// </summary>
public partial class FTG : View
{
    public static readonly DependencyProperty InletPortProperty = DependencyProperty.Register(
        nameof(InletPort),
        typeof(INotifyPropertyChanged),
        typeof(FTG),
        new PropertyMetadata(null)
    );

    [TypeConverter(typeof(ViewModelConverter))]
    public INotifyPropertyChanged InletPort
    {
        get => (INotifyPropertyChanged)GetValue(InletPortProperty);
        set => SetValue(InletPortProperty, value);
    }

    public FTG()
    {
        InitializeComponent();
    }
}

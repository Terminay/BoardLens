using Avalonia.Controls;
using Avalonia.Input;
using BoardLens.App.ViewModels;

namespace BoardLens.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.Key is Key.OemPlus or Key.Add)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.IncreaseFontSize();
                    e.Handled = true;
                }
            }
            else if (e.Key is Key.OemMinus or Key.Subtract or Key.OemPeriod)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.DecreaseFontSize();
                    e.Handled = true;
                }
            }
            else if (e.Key is Key.D0 or Key.NumPad0)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.ResetFontSize();
                    e.Handled = true;
                }
            }
        }
    }
}

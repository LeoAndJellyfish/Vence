using Microsoft.UI.Xaml.Controls;
using Vence.App.ViewModels;

namespace Vence.App.Views;

public sealed partial class ShellView : UserControl
{
    public ShellView()
    {
        ViewModel = new ShellViewModel();
        InitializeComponent();
    }

    public ShellViewModel ViewModel { get; }
}

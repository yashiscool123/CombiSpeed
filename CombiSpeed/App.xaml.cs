using System.Windows;
using CombiSpeed.ViewModels;

namespace CombiSpeed;

public partial class App : Application
{
    public static MainViewModel? ViewModel { get; set; }

    protected override void OnExit(ExitEventArgs e)
    {
        ViewModel?.OnApplicationExit();
        base.OnExit(e);
    }
}

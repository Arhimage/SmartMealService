using System.Windows;
using System.Windows.Controls;

namespace WpfSmsApp
{
    public partial class App : Application
    {
        public void MinimizeClick(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((Button)sender);
            if (window != null) window.WindowState = WindowState.Minimized;
        }

        public void CloseClick(object sender, RoutedEventArgs e)
        {
            Window.GetWindow((Button)sender)?.Close();
        }
    }

}

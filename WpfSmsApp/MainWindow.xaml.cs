using System.Windows;
using System.Windows.Media;
using WpfSmsApp.Services;
using WpfSmsApp.ViewModels;

namespace WpfSmsApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeDataGridClip();

            LoadDataGrid();
        }

        private void InitializeDataGridClip()
        {
            ClipGrid.Loaded += (s, e) => UpdateClip();
            ClipGrid.SizeChanged += (s, e) => UpdateClip();
            ClipGrid.IsVisibleChanged += (s, e) => UpdateClip();
        }

        private void LoadDataGrid()
        {
            DataContext = new DataGridVM(DataItemService.Instance, LoggerService.Instance);
        }

        private void UpdateClip()
        {
            if (ClipGrid.ActualWidth > 0 && ClipGrid.ActualHeight > 0)
            {
                ClipGrid.Clip = new RectangleGeometry(
                    new Rect(0, 0, ClipGrid.ActualWidth, ClipGrid.ActualHeight), 20, 20);
            }
        }
    }
}
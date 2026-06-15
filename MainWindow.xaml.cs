using System.Windows;
using SystemManager.ViewModels;

namespace SystemManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
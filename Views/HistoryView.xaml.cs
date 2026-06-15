using System.Windows.Controls;
using SystemManager.ViewModels;

namespace SystemManager.Views
{
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();
            DataContext = new HistoryViewModel();
        }
    }
}
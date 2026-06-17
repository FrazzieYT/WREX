using System.Windows.Controls;
using SystemManager.ViewModels;

namespace SystemManager.Views
{
    public partial class ServiceManagerView : UserControl
    {
        public ServiceManagerView()
        {
            InitializeComponent();
            DataContext = new ServiceManagerViewModel();
        }
    }
}

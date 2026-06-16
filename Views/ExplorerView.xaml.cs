using System.Windows.Controls;
using System.Windows.Input;
using SystemManager.ViewModels;

namespace SystemManager.Views
{
    public partial class ExplorerView : UserControl
    {
        public ExplorerView()
        {
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ExplorerViewModel vm)
            {
                vm.DoubleClickCommand.Execute(null);
            }
        }
    }
}
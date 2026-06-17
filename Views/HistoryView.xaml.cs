using System.Windows;
using System.Windows.Controls;
using SystemManager.Models;
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

        private void CopyHistoryEntry_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is HistoryViewModel vm && vm.SelectedEntry is HistoryEntry entry)
            {
                var text = $"[{entry.FormattedTime}] [{entry.Category}] {entry.Action}: {entry.Details}";
                Clipboard.SetText(text);
            }
        }
    }
}

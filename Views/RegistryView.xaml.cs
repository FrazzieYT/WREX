using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SystemManager.Services;
using SystemManager.Models;
using SystemManager.ViewModels; 

namespace SystemManager.Views
{
    public partial class RegistryView : UserControl
    {
        public RegistryView()
        {
            InitializeComponent();
            DataContext = new RegistryViewModel();
        }

        private void LaunchRegedit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "regedit.exe",
                    UseShellExecute = true
                });
                HistoryService.Log("Запуск утилиты", "Редактор реестра (regedit.exe)", "Utility");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось запустить: {ex.Message}", "Ошибка");
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is RegistryTreeNode node && DataContext is RegistryViewModel vm)
                vm.SelectedNode = node;
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem tvi && tvi.DataContext is RegistryTreeNode node 
                && DataContext is RegistryViewModel vm)
                vm.LoadChildren(node);
        }

        private void ValuesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is RegistryViewModel vm && vm.SelectedValue != null)
                vm.EditValueCommand.Execute(vm.SelectedValue);
        }

        private void FavoritesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView lv && lv.SelectedItem is FavoriteRegistryEntry fav 
                && DataContext is RegistryViewModel vm)
                vm.NavigateToFavoriteCommand.Execute(fav);
        }

        private void SearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView lv && lv.SelectedItem is RegistrySearchResult result 
                && DataContext is RegistryViewModel vm)
                vm.NavigateToSearchResultCommand.Execute(result);
        }
    }
}
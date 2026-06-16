using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SystemManager.ViewModels
{
    public class InputDialog : Window
    {
        private readonly TextBox _textBox;
        public string InputText => _textBox.Text;

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            Title = title;
            Width = 400;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.Children.Add(new Label { Content = prompt, Margin = new Thickness(0, 0, 0, 5) });
            Grid.SetRow(grid.Children[0], 0);

            _textBox = new TextBox { Text = defaultValue, Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(_textBox, 1);
            grid.Children.Add(_textBox);

            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right 
            };
            Grid.SetRow(buttonPanel, 2);

            var okButton = new Button 
            { 
                Content = "OK", 
                Width = 80, 
                Margin = new Thickness(0, 0, 5, 0), 
                IsDefault = true 
            };
            okButton.Click += (_, _) => { DialogResult = true; };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button 
            { 
                Content = "Отмена", 
                Width = 80, 
                IsCancel = true 
            };
            cancelButton.Click += (_, _) => { DialogResult = false; };
            buttonPanel.Children.Add(cancelButton);

            grid.Children.Add(buttonPanel);
            Content = grid;
        }
    }

    public class EditValueDialog : Window
    {
        private readonly TextBox _valueTextBox;
        private readonly ComboBox _typeComboBox;

        public string NewValue => _valueTextBox.Text;
        public RegistryValueKind NewKind => (RegistryValueKind)_typeComboBox.SelectedItem;

        public EditValueDialog(string name, string value, RegistryValueKind kind)
        {
            Title = "Редактирование значения";
            Width = 450;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(10) };
            for (int i = 0; i < 4; i++) 
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var nameLabel = new Label { Content = $"Имя: {name}", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(nameLabel, 0);
            grid.Children.Add(nameLabel);

            var valueLabel = new Label { Content = "Значение:", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(valueLabel, 1);
            grid.Children.Add(valueLabel);

            _valueTextBox = new TextBox { Text = value, Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(_valueTextBox, 1);
            grid.Children.Add(_valueTextBox);

            var typeLabel = new Label { Content = "Тип:", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(typeLabel, 2);
            grid.Children.Add(typeLabel);

            _typeComboBox = new ComboBox 
            { 
                Margin = new Thickness(0, 0, 0, 10), 
                ItemsSource = Enum.GetValues(typeof(RegistryValueKind)).Cast<RegistryValueKind>(), 
                SelectedItem = kind 
            };
            Grid.SetRow(_typeComboBox, 2);
            grid.Children.Add(_typeComboBox);

            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right 
            };
            Grid.SetRow(buttonPanel, 3);

            var okButton = new Button 
            { 
                Content = "OK", 
                Width = 80, 
                Margin = new Thickness(0, 0, 5, 0), 
                IsDefault = true 
            };
            okButton.Click += (_, _) => { DialogResult = true; };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button 
            { 
                Content = "Отмена", 
                Width = 80, 
                IsCancel = true 
            };
            cancelButton.Click += (_, _) => { DialogResult = false; };
            buttonPanel.Children.Add(cancelButton);

            grid.Children.Add(buttonPanel);
            Content = grid;
        }
    }
}
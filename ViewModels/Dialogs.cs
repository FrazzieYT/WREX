using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace SystemManager.ViewModels
{
    public class InputDialog : Window
    {
        private readonly TextBox _textBox;
        public string InputText => _textBox.Text;

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            Title = title; Width = 420; Height = 180; Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));

            var panel = new StackPanel { Margin = new Thickness(15) };
            panel.Children.Add(new TextBlock { Text = prompt, Foreground = Brushes.White, FontSize = 13, Margin = new Thickness(0, 0, 0, 8) });
            _textBox = new TextBox { Text = defaultValue, Padding = new Thickness(6), FontSize = 13, Margin = new Thickness(0, 0, 0, 15), Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)) };
            panel.Children.Add(_textBox);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 80, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)), Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            ok.Click += (_, _) => DialogResult = true;
            btnPanel.Children.Add(ok);
            var cancel = new Button { Content = "Отмена", Width = 80, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), IsCancel = true };
            cancel.Click += (_, _) => DialogResult = false;
            btnPanel.Children.Add(cancel);
            panel.Children.Add(btnPanel);
            Content = panel;
        }
    }

    public class EditValueDialog : Window
    {
        private readonly TextBox _valueTextBox;
        private readonly ComboBox _typeComboBox;

        public string NewValue => _valueTextBox.Text;
        public RegistryValueKind NewKind => (RegistryValueKind)(_typeComboBox.SelectedItem ?? RegistryValueKind.String);

        public EditValueDialog(string name, string value, RegistryValueKind kind)
        {
            Title = "Редактирование значения"; Width = 480; Height = 280; Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));

            var panel = new StackPanel { Margin = new Thickness(15) };
            panel.Children.Add(new TextBlock { Text = $"Имя: {name}", Foreground = Brushes.White, FontSize = 13, Margin = new Thickness(0, 0, 0, 10) });
            panel.Children.Add(new TextBlock { Text = "Значение:", Foreground = Brushes.White, FontSize = 13, Margin = new Thickness(0, 0, 0, 3) });
            _valueTextBox = new TextBox { Text = value, Padding = new Thickness(6), FontSize = 13, Margin = new Thickness(0, 0, 0, 10), Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)) };
            panel.Children.Add(_valueTextBox);
            panel.Children.Add(new TextBlock { Text = "Тип:", Foreground = Brushes.White, FontSize = 13, Margin = new Thickness(0, 0, 0, 3) });
            _typeComboBox = new ComboBox { Padding = new Thickness(6), FontSize = 13, Margin = new Thickness(0, 0, 0, 15), Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)), ItemsSource = new[] { RegistryValueKind.String, RegistryValueKind.DWord, RegistryValueKind.QWord, RegistryValueKind.ExpandString, RegistryValueKind.MultiString, RegistryValueKind.Binary }, SelectedItem = kind };
            panel.Children.Add(_typeComboBox);
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 80, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)), Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            ok.Click += (_, _) => DialogResult = true;
            btnPanel.Children.Add(ok);
            var cancel = new Button { Content = "Отмена", Width = 80, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), IsCancel = true };
            cancel.Click += (_, _) => DialogResult = false;
            btnPanel.Children.Add(cancel);
            panel.Children.Add(btnPanel);
            Content = panel;
        }
    }

    public class EditStartupDialog : Window
    {
        private readonly TextBox _nameBox;
        private readonly TextBox _commandBox;

        public string NewName => _nameBox.Text;
        public string NewCommand => _commandBox.Text;

        public EditStartupDialog(string name, string command, string source)
        {
            Title = "Редактирование автозагрузки"; Width = 540; Height = 260; Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));

            var panel = new StackPanel { Margin = new Thickness(15) };
            panel.Children.Add(new TextBlock { Text = $"Источник: {source}", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 0, 0, 10) });
            panel.Children.Add(new TextBlock { Text = "Имя:", Foreground = Brushes.White, FontSize = 13, Margin = new Thickness(0, 0, 0, 3) });
            _nameBox = new TextBox { Text = name, Padding = new Thickness(6), FontSize = 13, Margin = new Thickness(0, 0, 0, 10), Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)) };
            panel.Children.Add(_nameBox);
            panel.Children.Add(new TextBlock { Text = "Команда / путь:", Foreground = Brushes.White, FontSize = 13, Margin = new Thickness(0, 0, 0, 3) });
            _commandBox = new TextBox { Text = command, Padding = new Thickness(6), FontSize = 13, Margin = new Thickness(0, 0, 0, 15), Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)) };
            panel.Children.Add(_commandBox);
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 80, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)), Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            ok.Click += (_, _) => DialogResult = true;
            btnPanel.Children.Add(ok);
            var cancel = new Button { Content = "Отмена", Width = 80, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), IsCancel = true };
            cancel.Click += (_, _) => DialogResult = false;
            btnPanel.Children.Add(cancel);
            panel.Children.Add(btnPanel);
            Content = panel;
        }
    }

    public class EditServiceDialog : Window
    {
        private readonly TextBox _nameBox;
        private readonly TextBox _pathBox;
        private readonly ComboBox _startModeBox;

        public string NewName => _nameBox.Text;
        public string NewPath => _pathBox.Text;
        public string NewStartMode => _startModeBox.SelectedItem?.ToString() ?? "Manual";

        public EditServiceDialog(string name, string path, string startMode)
        {
            Title = "Редактирование службы"; Width = 540; Height = 300; Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));

            var panel = new StackPanel { Margin = new Thickness(15) };
            panel.Children.Add(new TextBlock { Text = "Имя службы:", Foreground = Brushes.White, FontSize = 13, Margin = new Thickness(0, 0, 0, 3) });
            _nameBox = new TextBox { Text = name, Padding = new Thickness(6), FontSize = 13, Margin = new Thickness(0, 0, 0, 10), Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)) };
            panel.Children.Add(_nameBox);
            panel.Children.Add(new TextBlock { Text = "Путь к исполняемому файлу:", Foreground = Brushes.White, FontSize = 13, Margin = new Thickness(0, 0, 0, 3) });
            _pathBox = new TextBox { Text = path, Padding = new Thickness(6), FontSize = 13, Margin = new Thickness(0, 0, 0, 10), Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)) };
            panel.Children.Add(_pathBox);
            panel.Children.Add(new TextBlock { Text = "Тип запуска:", Foreground = Brushes.White, FontSize = 13, Margin = new Thickness(0, 0, 0, 3) });
            _startModeBox = new ComboBox { Padding = new Thickness(6), FontSize = 13, Margin = new Thickness(0, 0, 0, 15), Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x3C, 0x3C, 0x3C)), ItemsSource = new[] { "Auto", "Manual", "Disabled" }, SelectedItem = startMode };
            panel.Children.Add(_startModeBox);
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 80, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)), Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            ok.Click += (_, _) => DialogResult = true;
            btnPanel.Children.Add(ok);
            var cancel = new Button { Content = "Отмена", Width = 80, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), IsCancel = true };
            cancel.Click += (_, _) => DialogResult = false;
            btnPanel.Children.Add(cancel);
            panel.Children.Add(btnPanel);
            Content = panel;
        }
    }
}

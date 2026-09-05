using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KaguraModManager.Models;
using KaguraModManager.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

namespace KaguraModManager;

public partial class MainWindow : Window
{
    private TaskbarIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        Icon = new BitmapImage(new Uri("pack://application:,,,/icon.png"));
        SetupTrayIcon();
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                ShowInTaskbar = false;
            }
        };
    }

    private void SetupTrayIcon()
    {
        try
        {
            var trayIcon = new TaskbarIcon
            {
                ToolTipText = "Kagura Mod Manager",
                Visibility = Visibility.Visible,
            };

            var icoStream = Application.GetResourceStream(new Uri("pack://application:,,,/icon.ico"));
            if (icoStream != null)
            {
                trayIcon.Icon = new System.Drawing.Icon(icoStream.Stream);
            }

            var menu = new ContextMenu();
            var showItem = new MenuItem { Header = "Show Mod Manager" };
            showItem.Click += TrayShow_Click;
            menu.Items.Add(showItem);
            menu.Items.Add(new Separator());
            var quitItem = new MenuItem { Header = "Quit" };
            quitItem.Click += TrayQuit_Click;
            menu.Items.Add(quitItem);

            trayIcon.ContextMenu = menu;
            trayIcon.TrayMouseDoubleClick += (_, _) =>
            {
                Show();
                ShowInTaskbar = true;
                WindowState = WindowState.Normal;
                Activate();
            };

            _trayIcon = trayIcon;
        }
        catch { }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.IsLaunching)
        {
            var result = MessageBox.Show(
                "Mods are currently being applied to the game. Exiting now may leave the game in a corrupted state.\n\nAre you sure you want to exit?",
                "Applying Mods",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
    }

    private void ModsTab_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.ActiveTab = "Mods";
    }

    private void SettingsTab_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.ActiveTab = "Settings";
    }

    private void ModDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.SelectedMod != null)
            vm.OpenModFolderCommand.Execute(vm.SelectedMod);
    }

    private void ModDataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var cm = new ContextMenu();
        var vm = (MainViewModel)DataContext;
        var mod = vm.SelectedMod;

        var openItem = new MenuItem { Header = "Open Mod Folder" };
        openItem.Click += (_, _) => { if (mod != null) vm.OpenModFolderCommand.Execute(mod); };
        cm.Items.Add(openItem);

        cm.Items.Add(new Separator());

        var deleteItem = new MenuItem { Header = "Delete Mod", Foreground = new SolidColorBrush(Colors.IndianRed) };
        deleteItem.Click += (_, _) => { if (mod != null) vm.DeleteModCommand.Execute(mod); };
        cm.Items.Add(deleteItem);

        ModDataGrid.ContextMenu = cm;
    }

    private void FavStar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is Mod mod && DataContext is MainViewModel vm)
            vm.ToggleFavoriteCommand.Execute(mod);
    }

    private void ModToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is Mod mod && DataContext is MainViewModel vm)
            vm.ToggleModCommand.Execute(mod);
    }

    private void AddModZip_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.AddModOption = "zip";
        if (NewModNameBox != null) NewModNameBox.Visibility = Visibility.Collapsed;
    }

    private void AddModFolder_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.AddModOption = "folder";
        if (NewModNameBox != null) NewModNameBox.Visibility = Visibility.Collapsed;
    }

    private void AddModNew_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.AddModOption = "new";
        if (NewModNameBox != null)
        {
            NewModNameBox.Visibility = Visibility.Visible;
            NewModNameBox.Focus();
        }
    }

    private void AddModCancel_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowAddModModal = false;
            vm.AddModOption = "zip";
            vm.NewModName = "";
        }
        NewModNameBox.Text = "";
        if (NewModNameBox != null) NewModNameBox.Visibility = Visibility.Collapsed;
    }

    private void ModalBackdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowAddModModal = false;
            vm.AddModOption = "zip";
            vm.NewModName = "";
        }
    }

    private void TrayIcon_TrayLeftMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        Show();
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
    }

    private void TrayShow_Click(object sender, RoutedEventArgs e)
    {
        Show();
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
    }

    private void TrayQuit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}

// Converters
public class BoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is int i ? i > 0 : value is bool bv && bv;
        if (parameter?.ToString() == "Invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public class TabVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tab = value?.ToString() ?? "";
        var target = parameter?.ToString() ?? "";
        return tab == target ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FavConverter : IValueConverter
{
    private static readonly Brush Gold = new SolidColorBrush(Color.FromRgb(234, 179, 8));
    private static readonly Brush Gray = new SolidColorBrush(Color.FromRgb(75, 85, 99));
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Gold : Gray;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ModEnabledConverter : IValueConverter
{
    private static readonly Brush Green = new SolidColorBrush(Color.FromRgb(34, 197, 94));
    private static readonly Brush Gray = new SolidColorBrush(Color.FromRgb(75, 85, 99));
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Green : Gray;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ModConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Mod mod)
            return new WpfRelayCommand(_ =>
            {
                if (System.Windows.Application.Current.MainWindow?.DataContext is MainViewModel vm)
                    vm.ToggleModCommand.Execute(mod);
            });
        return value!;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class WpfRelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    public WpfRelayCommand(Action<object?> execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute(parameter);
}

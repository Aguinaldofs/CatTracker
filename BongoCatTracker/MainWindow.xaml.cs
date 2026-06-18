using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BongoCatTracker;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly InputStats _stats;
    private readonly NativeInputHook _inputHook;
    private readonly NotifyIcon _notifyIcon;
    private readonly DispatcherTimer _rateTimer;
    private readonly DispatcherTimer _saveTimer;
    private readonly DispatcherTimer _spriteTimer;
    private readonly Queue<DateTimeOffset> _clickTimes = new();
    private readonly Queue<DateTimeOffset> _keyTimes = new();
    private bool _paused;
    private bool _reallyClose;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ClicksText => _stats.Clicks.ToString("N0", CultureInfo.CurrentCulture);
    public string KeysText => _stats.Keys.ToString("N0", CultureInfo.CurrentCulture);
    public string CpsText { get; private set; } = "0.0";
    public string KpsText { get; private set; } = "0.0";
    public string LastAction { get; private set; } = "Nada ainda";
    public string StatusText => _paused ? "Pausado" : "Contando mouse e teclado globalmente";
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _stats = StatsStore.Load();
        StatsStore.Save(_stats);
        _inputHook = new NativeInputHook();
        _inputHook.KeyPressed += OnGlobalKeyPressed;
        _inputHook.MousePressed += OnGlobalMousePressed;

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Text = "Bongo Cat Tracker",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();

        _rateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _rateTimer.Tick += (_, _) => UpdateRates();
        _rateTimer.Start();

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _saveTimer.Tick += (_, _) => SaveNow();

        _spriteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _spriteTimer.Tick += (_, _) =>
        {
            _spriteTimer.Stop();
            SetSprite("cat_idle.png");
        };

        RefreshCounters();
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Mostrar", null, (_, _) => ShowFromTray());
        menu.Items.Add("Pausar/Retomar", null, (_, _) => TogglePause());
        menu.Items.Add("Zerar", null, (_, _) => ResetStats());
        menu.Items.Add("Sair", null, (_, _) => ExitApp());
        return menu;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Log("Window loaded");
        SetSprite("cat_idle.png");
        Topmost = true;
        Show();
        Activate();

        try
        {
            _inputHook.Start();
            Log("Hooks started");
        }
        catch (Exception ex)
        {
            LastAction = ex.Message;
            OnPropertyChanged(nameof(LastAction));
            Log(ex.ToString());
        }
    }

    private void PositionNearTaskbar()
    {
        Rect workArea = SystemParameters.WorkArea;
        Left = Math.Max(workArea.Left + 12, workArea.Right - Width - 18);
        Top = Math.Max(workArea.Top + 12, workArea.Bottom - Height - 18);
    }

    private void OnGlobalMousePressed(string button)
    {
        Dispatcher.Invoke(() =>
        {
            if (_paused)
            {
                return;
            }

            _stats.Clicks += 1;
            _clickTimes.Enqueue(DateTimeOffset.UtcNow);
            LastAction = $"Clique {button}";
            SetSprite(button == "Direito" ? "cat_right.png" : "cat_left.png");
            RefreshCounters();
            ScheduleSave();
        });
    }

    private void OnGlobalKeyPressed()
    {
        Dispatcher.Invoke(() =>
        {
            if (_paused)
            {
                return;
            }

            _stats.Keys += 1;
            _keyTimes.Enqueue(DateTimeOffset.UtcNow);
            LastAction = "Tecla";
            SetSprite(_stats.Keys % 3 == 0 ? "cat_both.png" : _stats.Keys % 2 == 0 ? "cat_right.png" : "cat_left.png");
            RefreshCounters();
            ScheduleSave();
        });
    }

    private void SetSprite(string fileName)
    {
        string spritePath = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        CatSprite.Source = new BitmapImage(new Uri(spritePath, UriKind.Absolute));
        if (fileName != "cat_idle.png")
        {
            _spriteTimer.Stop();
            _spriteTimer.Start();
        }
    }

    private void UpdateRates()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Prune(_clickTimes, now);
        Prune(_keyTimes, now);

        CpsText = (_clickTimes.Count / 5.0).ToString("0.0", CultureInfo.InvariantCulture);
        KpsText = (_keyTimes.Count / 5.0).ToString("0.0", CultureInfo.InvariantCulture);
        OnPropertyChanged(nameof(CpsText));
        OnPropertyChanged(nameof(KpsText));
    }

    private static void Prune(Queue<DateTimeOffset> queue, DateTimeOffset now)
    {
        while (queue.Count > 0 && now - queue.Peek() > TimeSpan.FromSeconds(5))
        {
            queue.Dequeue();
        }
    }

    private void RefreshCounters()
    {
        OnPropertyChanged(nameof(ClicksText));
        OnPropertyChanged(nameof(KeysText));
        OnPropertyChanged(nameof(LastAction));
        OnPropertyChanged(nameof(StatusText));
    }

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveNow()
    {
        _saveTimer.Stop();
        StatsStore.Save(_stats);
    }

    private void TogglePause()
    {
        _paused = !_paused;
        _notifyIcon.Text = _paused ? "Bongo Cat Tracker - pausado" : "Bongo Cat Tracker";
        RefreshCounters();
    }

    private void ResetStats()
    {
        _stats.Clicks = 0;
        _stats.Keys = 0;
        _clickTimes.Clear();
        _keyTimes.Clear();
        LastAction = "Contadores zerados";
        RefreshCounters();
        SaveNow();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApp()
    {
        _reallyClose = true;
        Close();
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e) => TogglePause();

    private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetStats();

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (IsCloseButtonHit(e.GetPosition(this)))
        {
            ExitApp();
            return;
        }

        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private bool IsCloseButtonHit(System.Windows.Point position)
    {
        double side = Math.Min(ActualWidth, ActualHeight);
        if (side <= 0)
        {
            return false;
        }

        double offsetX = (ActualWidth - side) / 2;
        double offsetY = (ActualHeight - side) / 2;
        double x = (position.X - offsetX) / side;
        double y = (position.Y - offsetY) / side;

        return x >= 0.82 && x <= 0.94 && y >= 0.61 && y <= 0.75;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_reallyClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        SaveNow();
        _inputHook.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(StatsStore.AppDataDirectory);
            File.AppendAllText(
                Path.Combine(StatsStore.AppDataDirectory, "debug.log"),
                $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging should never affect the widget.
        }
    }
}

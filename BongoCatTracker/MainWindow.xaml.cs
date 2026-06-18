using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
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
    private readonly DispatcherTimer _eyeTimer;
    private readonly Queue<DateTimeOffset> _clickTimes = new();
    private readonly Queue<DateTimeOffset> _keyTimes = new();
    private readonly Random _random = new();
    private readonly Dictionary<string, BitmapSource> _spriteCache = new();
    private bool _paused;
    private bool _reallyClose;
    private string _currentSprite = "cat_idle.png";
    private EyePair _currentEyes = EyePair.Green;

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

        _eyeTimer = new DispatcherTimer();
        _eyeTimer.Tick += (_, _) => RandomizeEyes();
        ScheduleNextEyeChange();

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
        try
        {
            Log("Window loaded");
            SetSprite("cat_idle.png");
            Topmost = true;
            Show();
            Activate();
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
        _currentSprite = fileName;
        CatSprite.Source = BuildEyeVariant(GetSprite(fileName), _currentEyes);
        if (fileName != "cat_idle.png")
        {
            _spriteTimer.Stop();
            _spriteTimer.Start();
        }
    }

    private BitmapSource GetSprite(string fileName)
    {
        if (_spriteCache.TryGetValue(fileName, out BitmapSource? cached))
        {
            return cached;
        }

        string resourceName = $"BongoCatTracker.Assets.{fileName}";
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Sprite resource not found: {resourceName}");

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        _spriteCache[fileName] = bitmap;
        return bitmap;
    }

    private void RandomizeEyes()
    {
        EyePair[] variants =
        [
            EyePair.Green,
            new(EyeColor.Yellow, EyeColor.Green),
            new(EyeColor.Green, EyeColor.Yellow),
            new(EyeColor.Yellow, EyeColor.Yellow),
            new(EyeColor.YellowGreen, EyeColor.Yellow),
            new(EyeColor.Yellow, EyeColor.YellowGreen),
        ];

        _currentEyes = variants[_random.Next(variants.Length)];
        CatSprite.Source = BuildEyeVariant(GetSprite(_currentSprite), _currentEyes);
        ScheduleNextEyeChange();
    }

    private void ScheduleNextEyeChange()
    {
        _eyeTimer.Interval = TimeSpan.FromMilliseconds(_random.Next(900, 3400));
        _eyeTimer.Start();
    }

    private static BitmapSource BuildEyeVariant(BitmapSource source, EyePair eyes)
    {
        if (eyes == EyePair.Green)
        {
            return source;
        }

        BitmapSource bitmap = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;
        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        bitmap.CopyPixels(pixels, stride, 0);

        for (int y = 0; y < height; y++)
        {
            double ny = y / (double)height;
            if (ny < 0.30 || ny > 0.58)
            {
                continue;
            }

            for (int x = 0; x < width; x++)
            {
                double nx = x / (double)width;
                bool leftEye = nx is > 0.30 and < 0.48;
                bool rightEye = nx is > 0.50 and < 0.71;
                if (!leftEye && !rightEye)
                {
                    continue;
                }

                int offset = y * stride + x * 4;
                byte b = pixels[offset];
                byte g = pixels[offset + 1];
                byte r = pixels[offset + 2];
                byte a = pixels[offset + 3];
                if (a < 16 || !IsGreenEyePixel(r, g, b))
                {
                    continue;
                }

                EyeColor target = leftEye ? eyes.Left : eyes.Right;
                if (target == EyeColor.Green)
                {
                    continue;
                }

                (byte nr, byte ng, byte nb) = RecolorEyePixel(r, g, b, target);
                pixels[offset] = nb;
                pixels[offset + 1] = ng;
                pixels[offset + 2] = nr;
            }
        }

        var output = BitmapSource.Create(width, height, bitmap.DpiX, bitmap.DpiY, PixelFormats.Bgra32, null, pixels, stride);
        output.Freeze();
        return output;
    }

    private static bool IsGreenEyePixel(byte r, byte g, byte b)
    {
        return g > 95 && g > r + 18 && g > b + 16 && r is >= 55 and <= 190 && b <= 130;
    }

    private static (byte R, byte G, byte B) RecolorEyePixel(byte r, byte g, byte b, EyeColor target)
    {
        double shade = Math.Clamp(g / 170.0, 0.45, 1.45);
        (double tr, double tg, double tb) = target switch
        {
            EyeColor.Yellow => (238, 211, 62),
            EyeColor.YellowGreen => (196, 220, 75),
            _ => (r, g, b)
        };

        byte nr = (byte)Math.Clamp((tr * shade + r * 0.20) / 1.20, 0, 255);
        byte ng = (byte)Math.Clamp((tg * shade + g * 0.20) / 1.20, 0, 255);
        byte nb = (byte)Math.Clamp((tb * shade + b * 0.20) / 1.20, 0, 255);
        return (nr, ng, nb);
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
        _eyeTimer.Stop();
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

public enum EyeColor
{
    Green,
    Yellow,
    YellowGreen
}

public readonly record struct EyePair(EyeColor Left, EyeColor Right)
{
    public static EyePair Green { get; } = new(EyeColor.Green, EyeColor.Green);
}

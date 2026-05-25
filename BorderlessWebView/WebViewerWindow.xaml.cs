using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BorderlessWebView;

public partial class WebViewerWindow : Window
{
    private static readonly string _parentDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..");
    private static readonly string _userDataDirectory = Path.Combine(_parentDirectory, "UserData");
    private static readonly string _extensionsDirectory = Path.Combine(_parentDirectory, "Extensions");

    private static readonly string _titlePrefix = "BorderlessWebView |";

    private readonly string _url = "about:blank";

    public static readonly RoutedCommand ShowExtensionsCommand = new("ShowExtensions", typeof(WebViewerWindow));
    public static readonly RoutedCommand ToggleAlwaysOnTopCommand = new("ToggleAlwaysOnTop", typeof(WebViewerWindow));
    public static readonly RoutedCommand CloseAppCommand = new("CloseApp", typeof(WebViewerWindow));

    public ObservableCollection<ExtensionInfo> Extensions { get; private set; } = [];

    public WebViewerWindow()
    {
        InitializeComponent();
        CommandBindings.Add(new CommandBinding(ShowExtensionsCommand, ShowExtensions));
        CommandBindings.Add(new CommandBinding(ToggleAlwaysOnTopCommand, (s, e) => Topmost = !Topmost));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close()));
    }

    public WebViewerWindow(string url)
        : this()
    {
        _url = url;

        ((App)Application.Current).WindowPlace.Register(this);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        var options = new CoreWebView2EnvironmentOptions()
        {
            AreBrowserExtensionsEnabled = true
        };

        webView.CreationProperties = new CoreWebView2CreationProperties()
        {
            UserDataFolder = _userDataDirectory
        };

        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: webView.CreationProperties.UserDataFolder, options: options);

        await webView.EnsureCoreWebView2Async(environment);
        await InitializeExtensionsAsync();

        webView.CoreWebView2.ContainsFullScreenElementChanged += FullScreenRequested;
        webView.CoreWebView2.FaviconChanged += FaviconChanged;
        webView.CoreWebView2.DocumentTitleChanged += DocumentTitleChanged;

        webView.CoreWebView2.Navigate(_url);
    }

    private async Task InitializeExtensionsAsync()
    {
        if (!Directory.Exists(_extensionsDirectory))
        {
            Debug.WriteLine("Extensions directory not found.");
            return;
        }

        var availableCrxFiles = Directory.GetFiles(_extensionsDirectory, "*.crx");
        foreach (var crxFile in availableCrxFiles)
        {
            try
            {
                ExtensionUtils.UnpackCrxFile(crxFile, _extensionsDirectory);
                File.Delete(crxFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to unpack extension '{Path.GetFileName(crxFile)}': {ex.Message}", "Extension Unpack Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Failed to unpack extension '{crxFile}': {ex.Message}");
            }
        }

        var availableExtensions = Directory.GetDirectories(_extensionsDirectory);
        var loadedExtensions = await webView.CoreWebView2.Profile.GetBrowserExtensionsAsync();

        foreach (var path in availableExtensions)
        {
            var metadataPath = Path.Combine(path, ".loaded-info");
            var metadataText = File.Exists(metadataPath) ? File.ReadAllText(metadataPath) : null;

            ExtensionInfo? extensionInfo;

            try
            {
                extensionInfo = metadataText is not null
                    ? JsonSerializer.Deserialize(metadataText, SourceGenerationContext.Default.ExtensionInfo)
                    : null;
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse metadata for extension at '{path}': {ex.Message}");
                extensionInfo = null;
            }

            if (extensionInfo is null || !loadedExtensions.Any(ext => ext.Id == extensionInfo.Id))
            {
                try
                {
                    var extension = await webView.CoreWebView2.Profile.AddBrowserExtensionAsync(path);
                
                    extensionInfo = await ExtensionUtils.GetExtensionInfo(extension.Id, path);
                    File.WriteAllText(metadataPath, JsonSerializer.Serialize(extensionInfo, SourceGenerationContext.Default.ExtensionInfo));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load extension from '{path}': {ex.Message}", "Extension Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"Failed to load extension from '{path}': {ex.Message}");
                    continue;
                }
            }

            Extensions.Add(extensionInfo);
        }
    }

    private void FaviconChanged(object? sender, object e)
    {
        //Icon = GetFavicon(webView.CoreWebView2.FaviconUri);
    }

    private void DocumentTitleChanged(object? sender, object e)
    {
        var title = webView.CoreWebView2.DocumentTitle;
        Title = string.IsNullOrEmpty(title) ? nameof(BorderlessWebView) : $"{_titlePrefix} {title}";
    }

    private void FullScreenRequested(object? sender, object e)
    {
        if (webView.CoreWebView2.ContainsFullScreenElement)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                WindowState = WindowState.Maximized;
            }

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
        }
        else
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
        }
    }

    private static BitmapImage? GetFavicon(string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return null;
        }

        try
        {
            return new BitmapImage(new Uri(uri));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load favicon: {ex.Message}");
            return null;
        }
    }

    private void ShowExtensions(object sender, ExecutedRoutedEventArgs e)
    {
        extensionList.Visibility = extensionList.Visibility is Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        if (extensionList.Visibility is Visibility.Collapsed && extensionList.SelectedItem != null)
        {
            extensionList.SelectedItem = null;
            webView.CoreWebView2.Navigate(_url);
        }
    }

    private void ExtensionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (extensionList.SelectedItem is not ExtensionInfo selectedExtension)
        {
            return;
        }

        var target = selectedExtension.OptionsPath is not null
            ? $"extension://{selectedExtension.Id}/{selectedExtension.OptionsPath}"
            : "about:blank";

        webView.CoreWebView2.Navigate(target);
    }
}

using Microsoft.Win32;

const string AppDirectory = "app";
const string HostName = "cz.leoshusar.borderlesswebview";
const string RegistryPath = $@"Software\Google\Chrome\NativeMessagingHosts\{HostName}";

#if INSTALL_MODE
Install();
#elif UNINSTALL_MODE
Uninstall();
#else
if (args.Length == 1)
{
    var command = args[0];

    if (command == "--install")
    {
        Install();
        return;
    }
    else if (command == "--uninstall")
    {
        Uninstall();
        return;
    }
}
else
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  --install   Install the native host");
    Console.WriteLine("  --uninstall Uninstall the native host");
    Console.WriteLine();
}
#endif

Console.WriteLine("Press any key to exit...");
Console.ReadKey();

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS8321 // Local function is declared but never used
static void Install()
{
    var currentDir = AppDomain.CurrentDomain.BaseDirectory;
    var manifestPath = Path.Combine(currentDir, AppDirectory, $"{HostName}.json");

    if (!File.Exists(manifestPath))
    {
        Console.WriteLine($"Manifest file not found: {manifestPath}");
        return;
    }

    using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
    key.SetValue("", manifestPath);

    Console.WriteLine("Native host installed successfully.");
}

static void Uninstall()
{
    using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);

    if (key is not null)
    {
        Registry.CurrentUser.DeleteSubKey(RegistryPath);
        Console.WriteLine("Native host uninstalled successfully.");
    }
    else
    {
        Console.WriteLine("Native host is not installed.");
    }
}
#pragma warning restore CS8321 // Local function is declared but never used
#pragma warning restore IDE0079 // Remove unnecessary suppression

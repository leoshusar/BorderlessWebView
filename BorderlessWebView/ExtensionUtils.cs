using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace BorderlessWebView;

public static class ExtensionUtils
{
    public static void UnpackCrxFile(string crxPath, string extensionsPath)
    {
        const int MinimumCrxHeaderLength = 12;

        var extensionName = Path.GetFileNameWithoutExtension(crxPath);
        var destinationPath = Path.Combine(extensionsPath, extensionName);

        if (Directory.Exists(destinationPath))
        {
            MessageBox.Show($"Extension \"{extensionName}\" already exists. Please remove it before installing a new one.", "Extension Exists", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        else
        {
            Directory.CreateDirectory(destinationPath);
        }

        using var stream = File.Open(crxPath, FileMode.Open);
        if (stream.Length < MinimumCrxHeaderLength)
        {
            throw new FileFormatException($"[{extensionName}] Could not find Crx file header at the beginning of the file.");
        }

        var buffer = new byte[4];

        stream.ReadExactly(buffer);
        if (Encoding.ASCII.GetString(buffer) != "Cr24")
        {
            throw new FileFormatException($"[{extensionName}] Invalid Crx file header");
        }

        stream.ReadExactly(buffer);
        var version = BitConverter.ToUInt32(buffer, 0);
        if (version != 3)
        {
            throw new FileFormatException($"[{extensionName}] Invalid Crx version ({version}). Only Crx version 3 is supported.");
        }

        stream.ReadExactly(buffer);
        var headerLength = BitConverter.ToUInt32(buffer, 0);
        if (stream.Length < stream.Position + headerLength)
        {
            throw new FileFormatException($"[{extensionName}] Invalid Crx header length ({headerLength}).");
        }

        stream.Seek(headerLength, SeekOrigin.Current);

        var tempFilename = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            using (var fileStream = File.Create(tempFilename))
            {
                stream.CopyTo(fileStream);
            }

            using var zipArchive = ZipFile.OpenRead(tempFilename);
            zipArchive.ExtractToDirectory(destinationPath, true);
        }
        catch (Exception)
        {
            Directory.Delete(destinationPath, true);
            throw;
        }
        finally
        {
            File.Delete(tempFilename);
        }
    }

    public static async Task<ExtensionInfo> GetExtensionInfo(string extensionId, string path)
    {
        var manifest = await File.ReadAllTextAsync(Path.Combine(path, "manifest.json"));
        var manifestJson = JsonDocument.Parse(manifest);

        string? FindExtensionName(string property)
        {
            if (manifestJson.RootElement.TryGetProperty(property, out var nameElement))
            {
                var result = nameElement.GetString()!;
                if (!result.StartsWith("__MSG") && !string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }

            return null;
        }

        var name = FindExtensionName("name")
            ?? FindExtensionName("short_name")
            ?? Path.GetFileName(path);

        string? optionsPath = null;

        if (manifestJson.RootElement.TryGetProperty("options_page", out var optionsPage))
        {
            optionsPath = optionsPage.GetString();
        }

        if (optionsPath is null && manifestJson.RootElement.TryGetProperty("options_ui", out var optionsUi))
        {
            var uiPage = optionsUi.GetProperty("page");
            optionsPath = uiPage.GetString();
        }

        return new ExtensionInfo(extensionId, name, optionsPath);
    }
}

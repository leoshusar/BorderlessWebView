using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using var stdin = Console.OpenStandardInput();
using var reader = new BinaryReader(stdin);

var length = reader.ReadInt32();
var bytes = reader.ReadBytes(length);
var json = Encoding.UTF8.GetString(bytes);

var message = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.UrlMessage);

if (message is null)
{
    SendResponse("Invalid message");
    return;
}

Process.Start(new ProcessStartInfo
{
    FileName = "BorderlessWebView.exe",
    Arguments = message.Url,
    UseShellExecute = true
});

SendResponse("ok");

static void SendResponse(string status)
{
    var response = new ResponseMessage(status);
    var responseJson = JsonSerializer.Serialize(response, SourceGenerationContext.Default.ResponseMessage);
    var bytes = Encoding.UTF8.GetBytes(responseJson);

    using var stdout = Console.OpenStandardOutput();
    using var writer = new BinaryWriter(stdout);

    writer.Write(bytes.Length);
    writer.Write(bytes);
    writer.Flush();
}

record UrlMessage(string Url);
record ResponseMessage(string Status);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(UrlMessage))]
[JsonSerializable(typeof(ResponseMessage))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

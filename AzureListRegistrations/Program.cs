// See https://aka.ms/new-console-template for more information
using System.Net.Security;
using Microsoft.Azure.NotificationHubs;
using System.Text.Json;
using System.Text.Json.Nodes;
using Helpers;

var JSON_SERIALIZER_OPTS = new JsonSerializerOptions { WriteIndented = true};

var httpClientHandler = new HttpClientHandler();
httpClientHandler.ServerCertificateCustomValidationCallback += (_, cert, _, sslPolicyErrors) =>
{
    return sslPolicyErrors == SslPolicyErrors.None || new string[] { "CN=Cisco Umbrella Secondary SubCA atl1-SG, O=Cisco", "O=Cisco, CN=Cisco Umbrella Secondary SubCA atl1-SG" }.Contains(cert?.Issuer);
};
var httpClient = new HttpClient(httpClientHandler);
var notificationHubSettings = new NotificationHubSettings
{
    HttpClient = httpClient
};

var helper = new Helper();
if (!helper.VerifyArguments(args))
{
    throw new Exception("Arguments are not valid!");
}

var client = new NotificationHubClient(args[0] as string, args[1] as string, notificationHubSettings);

// TODO: Get tag from latest registration
var registrations = await client.GetAllRegistrationsAsync(0);
var continuationToken = registrations.ContinuationToken;
var allRegistrations = new List<RegistrationDescription>(registrations);
while (!string.IsNullOrWhiteSpace(continuationToken))
{
    var nextRegistrations = await client.GetAllRegistrationsAsync(continuationToken, 0);
    allRegistrations.AddRange(nextRegistrations);
    continuationToken = nextRegistrations.ContinuationToken;
}

var executablePath = AppDomain.CurrentDomain.BaseDirectory;
var currentTimeStamp = DateTime.Now.ToString("yyyy_MM_dd");
var filePathDir = Path.Join(executablePath, "../../../../data");
var filePath = Path.Join(filePathDir, $"/{currentTimeStamp}_registrations.json");

Directory.CreateDirectory(filePathDir);

using (var file = File.Create(filePath))
{
    file.Write(
        System.Text.Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(
                allRegistrations.OrderBy(x => x.ExpirationTime),
                JSON_SERIALIZER_OPTS
            )
        )
    );
}

Console.WriteLine($"Wrote {allRegistrations.Count} registrations to {filePath}");

var testStr = "This is a test notification`from colonelpopcorn/AzureNotificationHubListRegistrations, you may ignore it";
var androidTempl = "{ \"message\": { \"notification\": { \"body\" : \"" + testStr + "\"} } }";
var appleTempl = "{\"aps\":{\"alert\":\"" + testStr + "\"}}";

var tags = allRegistrations
    .Where(registration => registration.Tags != null)
    .SelectMany(descr => descr.Tags)
    .Where(tagStr => tagStr.StartsWith("$InstallationId"))
    .ToList();

Console.WriteLine(JsonSerializer.Serialize(tags, JSON_SERIALIZER_OPTS));
var res = "";
while (true) {
    Console.WriteLine($"Would you like to send test notifications to {tags.Count} devices? y/n");
    res = Console.ReadLine();
    if (res!.Equals("y", StringComparison.CurrentCultureIgnoreCase)) {
        var tagResponses = tags.Select(async tag => {
            var androidRes = await client.SendFcmV1NativeNotificationAsync(androidTempl, tag);
            var appleRes = await client.SendAppleNativeNotificationAsync(appleTempl, tag);
            return new { androidRes, appleRes };
        });
        var resultFilePath = Path.Join(filePathDir, $"/{currentTimeStamp}_test_sends.json");
        using (var file = File.Create(resultFilePath))
        {
            file.Write(
                System.Text.Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(
                        tagResponses,
                        JSON_SERIALIZER_OPTS
                    )
                )
            );
        }
        Console.WriteLine($"Wrote send results to {resultFilePath}");
        break;
    } else if (res!.Equals("n", StringComparison.CurrentCultureIgnoreCase)) {
        Console.WriteLine("Have a great day!");
        break;
    }
}

namespace Helpers
{
    public class Helper
    {
        public bool VerifyArguments(string[] arguments)
        {
            return arguments.Length >= 3;
        }
    }
}
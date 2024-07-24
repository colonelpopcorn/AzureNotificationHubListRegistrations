// See https://aka.ms/new-console-template for more information
using System.Net.Security;
using Microsoft.Azure.NotificationHubs;
using System.Text.Json;
using System.Text.Json.Nodes;
using Helpers;

var httpClientHandler = new HttpClientHandler();
httpClientHandler.ServerCertificateCustomValidationCallback += (_, cert, _, sslPolicyErrors) =>
{
    return sslPolicyErrors == SslPolicyErrors.None || new string[] { "CN=Cisco Umbrella Secondary SubCA atl1-SG, O=Cisco","O=Cisco, CN=Cisco Umbrella Secondary SubCA atl1-SG" }.Contains(cert?.Issuer);
};
var httpClient = new HttpClient(httpClientHandler);
var notificationHubSettings = new NotificationHubSettings
{
    HttpClient = httpClient
};

var helper = new Helper();
if (!(helper.VerifyArguments(args))) {
    throw new Exception("Arguments are not valid!");
}

var client = new NotificationHubClient(args[0] as string, args[1] as string, notificationHubSettings);

// TODO: Get tag from latest registration
var registration = (await client
    .GetAllRegistrationsAsync(1000))
    // .Where(x => $"{x.ExpirationTime:yyyy-MM-dxdTHH:mm:ss.FFFFFFFK}" != "9999-12-31T23:59:59.9999999Z")
    .OrderBy(x => x.ExpirationTime)
    .First();

Console.WriteLine(JsonSerializer.Serialize(registration, new JsonSerializerOptions {WriteIndented = true}).ToString());

// var testStr = "This is a test notification from colonelpopcorn/AzureNotificationHubListRegistrations, you may ignore it";
// var androidTempl = "{ \"message\": { \"notification\": { \"body\" : \"" + testStr + "\"} } }";
// var appleTempl = "{\"aps\":{\"alert\":\"" + testStr + "\"}}";

// if (args.Length >= 3)
// {
//     if (args[2] == "android")
//     {
//         await client.SendFcmV1NativeNotificationAsync(androidTempl);
//     }
//     else if (args[2] == "ios")
//     {
//         await client.SendAppleNativeNotificationAsync(appleTempl);
//     }
//     else
//     {
//         Console.WriteLine("No implementation found for " + args[2] + "!");
//     }
// }
// else
// {
//     Console.WriteLine("Please provide a platform argument!");
// }
namespace Helpers {
    public class Helper {
        public bool VerifyArguments(string[] arguments) {
            return arguments.Length >= 3;
        }
    }
}
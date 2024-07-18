// See https://aka.ms/new-console-template for more information
using Microsoft.Azure.NotificationHubs;
using Newtonsoft.Json;

var registrations = await NotificationHubClient.CreateClientFromConnectionString(args[0] as string, args[1] as string)
    .GetAllRegistrationsAsync(1000);

Console.WriteLine(JsonConvert.SerializeObject(registrations, Formatting.Indented).ToString());
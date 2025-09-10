using HITS.TelegramBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<TelegramBotService>();
        services.AddSingleton<ApiClientService>();
        services.AddSingleton<SimpleMappingService>();
        services.AddHttpClient();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    });

await builder.RunConsoleAsync();
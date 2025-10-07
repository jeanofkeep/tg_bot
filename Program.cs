using DotNetEnv;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using tg_bot.Data;
//using tg_bot.Models;
using tg_bot.Handlers;


namespace tg_bot
{

    class Program
    {
        static async Task Main()
        {

            DotNetEnv.Env.Load("../../../.env");

            var token = Environment.GetEnvironmentVariable("BOT_TOKEN");

            Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");

            var options = new DbContextOptionsBuilder<BotDbContext>()
                .UseNpgsql("Host=localhost;Port=5432;Database=telegram_bot;Username=postgres;Password=12345678")
                .Options;

            using var db = new BotDbContext(options);

            db.Database.EnsureCreated();


            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("BOT_TOKEN not found in .env file");
                return;

            }
            var bot = new TelegramBotClient(token);

            var me = await bot.GetMeAsync();

            var handler = new BotHandlers(db);

            bot.StartReceiving(
                handler.HandleUpdateAsync,
                handler.HandleErrorAsync);

            Console.WriteLine($"Bot @{me.Username} started!");

            Console.ReadLine();

        }
    }
}


//using var cts = new CancellationTokenSource();
//var bot = new TelegramBotClient("8455916969:AAHP799q0y0QcYykNBP8R_dkCT22xACs6WI", cancellationToken: cts.Token);
//var me = await bot.GetMe();

/*
class program
{
    private static TelegramBotClient _bot;
    private static CancellationTokenSource _cts;

    static async Task Main(string[] args)
    {
        var bot = new TelegramBotClient("8455916969:AAHP799q0y0QcYykNBP8R_dkCT22xACs6WI");

        _cts = new CancellationTokenSource();

        var me = await bot.GetMe();
        Console.WriteLine($"Bot @{me.Username} started!");


    }

    public static ReplyKeyboardMarkup GetButtonKeyboard()
    {
        var kbrd = new ReplyKeyboardMarkup(new KeyboardButton[][]
        {

            new[]
            {
                new KeyboardButton("Button1")
            },
            new[]
            {
                new KeyboardButton("Button2")
            }

        });
        return kbrd;

    }


}
*/
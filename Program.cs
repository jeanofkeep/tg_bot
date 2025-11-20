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
                .UseNpgsql(Environment.GetEnvironmentVariable("BOT_DB"))
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




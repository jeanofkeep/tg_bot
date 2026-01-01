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
using StackExchange.Redis;
using tg_bot.Services;
using StackExchange.Redis;


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

            var redisConnection = ConnectionMultiplexer.Connect("localhost:6379");
            var redisService = new RedisService(redisConnection);


            using var db = new BotDbContext(options);


            

            


            /*
            if (await db.Database.CanConnectAsync())
            {
                Console.WriteLine("Database is available.");
            }
            else
            {
                Console.WriteLine("Cannot connect to database. Please check connection string and server.");
                return;
            }
            */

            db.Database.EnsureCreated();


            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("BOT_TOKEN not found in .env file");
                return;

            }
            var bot = new TelegramBotClient(token);

            var me = await bot.GetMeAsync();

            var handler = new BotHandlers(db, redisService);

            bot.StartReceiving(
                handler.HandleUpdateAsync,
                handler.HandleErrorAsync);

            Console.WriteLine($"Bot @{me.Username} started!");

            Console.ReadLine();

        }
    }
}




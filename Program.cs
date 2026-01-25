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


namespace tg_bot
{

    class Program
    {
        static async Task Main()
        {

            DotNetEnv.Env.Load("../../../.env");

            var token = Environment.GetEnvironmentVariable("BOT_TOKEN");

            Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");

            //postgres

            var options = new DbContextOptionsBuilder<BotDbContext>()
                .UseNpgsql("Host=postgres;Port=5432;Database=telegram_bot;Username=postgres;Password=12345678")
                .Options;

            //redis

            var redisOptions = ConfigurationOptions.Parse("redis:6379");
            redisOptions.AbortOnConnectFail = false;
            redisOptions.ConnectRetry = 10;

            var redisConnection = ConnectionMultiplexer.Connect("redis:6379");;
            var redisService = new RedisService(redisConnection);

            //var redisConnection = ConnectionMultiplexer.Connect(options);

             



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
            using var cts = new CancellationTokenSource();

            bot.StartReceiving(
                handler.HandleUpdateAsync,
                handler.HandleErrorAsync,
                cancellationToken: cts.Token);

            Console.WriteLine($"Bot @{me.Username} started!");

            
            await Task.Delay(Timeout.Infinite, cts.Token);

        }
    }
}




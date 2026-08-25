using System;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Options;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel.Design;
using System.Threading.Channels;
using StackExchange.Redis;
using tg_bot.State;
using tg_bot.Services;
using tg_bot.Data;
using tg_bot.Models;
using static tg_bot.Keyboards.MainKeyboards;

namespace tg_bot.Handlers
{
    public class BotHandlers
    {
        private readonly BotDbContext _db;
        private readonly RedisService _redis;
        private readonly TaskHandler _taskHandler;
        private readonly ProjectHandler _projectHandler;
        private readonly PurchaseHandler _purchaseHandler;

        public BotHandlers(BotDbContext db, RedisService redis)
        {
            _db = db;
            _redis = redis;
            _taskHandler = new TaskHandler(db, redis);
            _projectHandler = new ProjectHandler(db, redis);
            _purchaseHandler = new PurchaseHandler(db, redis);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update == null)
            {
                Console.WriteLine("Update is null");
                return;
            }

            if (update.Type != Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                Console.WriteLine("Update type is not Message");
                return;
            }

            if (update.Message == null)
            {
                Console.WriteLine("Message is null");
                return;
            }

            if (update.Message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
            {
                Console.WriteLine("Message type is not Text");
                return;
            }

            if (string.IsNullOrEmpty(update.Message.Text))
            {
                Console.WriteLine("Message text is null or empty");
                return;
            }

            var ChatId = update.Message.Chat.Id;
            var text = update.Message.Text;

            Console.WriteLine($"Processing message from {ChatId}: {text}");

            var state = await _redis.GetUserStateAsync(ChatId);
            
            if (state == null)
            {
                state = new UserState
                {
                    UserId = ChatId,
                    CurrentState = UserStateType.None,
                    CurrentSection = "Main",
                    TempText = null,
                    TempProject = null,
                    TempPurchase = null
                };

                await _redis.SetUserStateAsync(state);
            }

            if (text == "↩️Cancel" || text == "🔙Back")
            {
                state.CurrentState = UserStateType.None;
                state.TempText = null;
                state.TempProject = null;
                state.TempPurchase = null;

                await _redis.SetUserStateAsync(state);
                
                await bot.SendTextMessageAsync(
                    ChatId,
                    "Main menu:",
                    replyMarkup: MainMenu,
                    cancellationToken: ct
                );
                return;
            }

            // WAITING FOR TASK TEXT
            // WAITING FOR TASK DATE
            // WAITING FOR TASK TIME
            if (state.CurrentState == UserStateType.AwaitingTaskText ||
                state.CurrentState == UserStateType.AwaitingTaskDate ||
                state.CurrentState == UserStateType.AwaitingTaskTime ||
                state.CurrentState == UserStateType.AwaitingTaskDelete ||
                text == "📋Tasks" || text == "📝Create task" ||
                text == "🎯My tasks" || text == "❌Delete task")
            {
                await _taskHandler.HandleAsync(bot, ChatId, text, state, ct);
                return;
            }

            // WAITING FOR PROJECT NAME
            if (state.CurrentState == UserStateType.AwaitingProjectName ||
                state.CurrentState == UserStateType.AwaitingProjectDelete ||
                text == "📁Projects" || text == "➕Create projects" ||
                text == "📒My Projects" || text == "❌Delete project")
            {
                await _projectHandler.HandleAsync(bot, ChatId, text, state, ct);
                return;
            }
            // WAITING FOR PURCHASE NAME
            if (state.CurrentState == UserStateType.AwaitingPurchaseName ||
                state.CurrentState == UserStateType.AwaitingPurchaseDelete ||
                text == "🛍Shopping list" || text == "➕Add purchases" ||
                text == "📝My purchases" || text == "❌Delete purchases")
            {
                await _purchaseHandler.HandleAsync(bot, ChatId, text, state, ct);
                return;
            }

            //task delete
            
            //project delete
            
            //purchase delete
            
            // MAIN SWITCH
            switch (text)
            {
                case "/start":
                    await bot.SendTextMessageAsync(ChatId, "Main menu:", replyMarkup: MainMenu, cancellationToken: ct);
                    break;

                case "🔔Notifications":
                    await bot.SendTextMessageAsync(ChatId, "🚧This section is under development", replyMarkup: SectionNotifications, cancellationToken: ct);
                    return;

                default:
                    await bot.SendTextMessageAsync(ChatId, "Unknown command. Use the menu.", replyMarkup: MainMenu, cancellationToken: ct);
                    break;
            }
        }
        public Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
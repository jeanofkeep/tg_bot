using System;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
//using Telegram.Bot.Exceptions.Polling;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using tg_bot.Data;
using tg_bot.Models;

namespace tg_bot.Handlers
{
    public class BotHandlers
    {
        private readonly BotDbContext _db;

        public BotHandlers(BotDbContext db)
        {
            _db = db;
        }

        private static ReplyKeyboardMarkup MainMenu = new(new[]
        {
        new KeyboardButton[]{ "📋Tasks", "Projects","Norifications"},
		//new KeyboardButton[]{"Exit" }
	})
        {
            ResizeKeyboard = true
        };

        private static ReplyKeyboardMarkup SectionTasks = new(new[]
        {
        new KeyboardButton[]{ "🎯My tasks", "📝Create task"},
        new KeyboardButton[]{ "🔙Back" }
    })
        {
            ResizeKeyboard = true
        };

        private static ReplyKeyboardMarkup SectionProjects = new(new[]
        {
        new KeyboardButton[]{"Podrazdel1","Podrazdel2"},
        new KeyboardButton[]{ "🔙Back" }
    })
        {
            ResizeKeyboard = true
        };
        public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {

            var ChatId = update.Message.Chat.Id;
            var text = update.Message.Text;

            switch (text)
            {
                case "/start":
                    await bot.SendTextMessageAsync(ChatId, "Main menu:", replyMarkup: MainMenu, cancellationToken: ct);
                    break;

                case "📋Tasks":
                    await bot.SendTextMessageAsync(ChatId, "📋Tasks", replyMarkup: SectionTasks, cancellationToken: ct);
                    break;

                case "Projects":
                    await bot.SendTextMessageAsync(ChatId, "Projects:", replyMarkup: SectionProjects, cancellationToken: ct);
                    break;

                case "🔙Back":
                    await bot.SendTextMessageAsync(ChatId, "Main menu:", replyMarkup: MainMenu, cancellationToken: ct);
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

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
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Options;
using System.Text;
//using Telegram.Bots.Types;

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
        //new KeyboardButton[]{ "🔙Back" }
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

            if (update.Type != Telegram.Bot.Types.Enums.UpdateType.Message || update.Message == null)
                return;

            if (update.Message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return;


            var ChatId = update.Message.Chat.Id;
            var text = update.Message.Text;

            //using var db = new BotDbContext();


            var state = _db.UserStates.FirstOrDefault(s => s.UserId == ChatId);
            if (state == null)
            {
                state = new UserState
                {
                    UserId = ChatId,
                    IsAwaitingText = false,
                    IsAwaitingTime = false,
                    TempText = null
                };

                _db.UserStates.Add(state);
                await _db.SaveChangesAsync(ct);
            }

            if (state.IsAwaitingText)
            {
                state.TempText = text;
                state.IsAwaitingText = false;
                state.IsAwaitingTime = true;
                await _db.SaveChangesAsync(ct);

                await bot.SendTextMessageAsync(ChatId, "Enter the time:", cancellationToken: ct);
                return;
            }

            if (state.IsAwaitingTime)
            {
                if (DateTime.TryParse(text, out DateTime parsedTime))
                {
                    var reminderDate = DateTime.Today.Add(parsedTime.TimeOfDay).ToUniversalTime();
                    _db.UserMessages.Add(new UserMessage
                    {
                        UserId = ChatId,
                        Text = state.TempText,
                        Time = reminderDate,
                        ReminderDateTime = DateTime.UtcNow,
                    });

                    state.IsAwaitingTime = false;
                    var savedTask = state.TempText;
                    state.TempText = null;

                    await _db.SaveChangesAsync(ct); 
                    
                    await bot.SendTextMessageAsync(ChatId, $"Task'{state.TempText}' on '{reminderDate:t}' saved", cancellationToken: ct);
                    //var taskText = state.TempText;
                    //var taskTime = text;
                }
                return;
            }
                switch (text)
                {
                    case "/start":
                        await bot.SendTextMessageAsync(ChatId, "Main menu:", replyMarkup: MainMenu, cancellationToken: ct);
                        break;

                    case "📋Tasks":
                        await bot.SendTextMessageAsync(ChatId, "📋Tasks", replyMarkup: SectionTasks, cancellationToken: ct);
                    break;

                    case "📝Create task":
                        await bot.SendTextMessageAsync(ChatId, "📝Create task:", cancellationToken: ct);
                        state.IsAwaitingText = true;
                        state.IsAwaitingTime = false;
                        state.TempText = null;
                        await _db.SaveChangesAsync(ct);
                    break;

                    case "🎯My tasks":

                    var UserTask = _db.UserMessages
                        .Where(x => x.UserId == ChatId)
                        .OrderBy(m => m.Time)
                        .ToList();
                    if (UserTask.Count == 0)
                    {
                        await bot.SendTextMessageAsync(ChatId, "You dont have tasks", cancellationToken: ct);
                    }
                    else
                    {
                        var sb = new StringBuilder("Your tasks: \n\n");
                        foreach (var task in UserTask)
                        {
                            sb.AppendLine($"*{task.Time:t}* - {task.Text}");
                        }
                        await bot.SendTextMessageAsync(ChatId, sb.ToString(), parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: ct);
                    }
                    break;



                case "Projects":
                        await bot.SendTextMessageAsync(ChatId, "Projects:", replyMarkup: SectionProjects, cancellationToken: ct);
                        break;

                    case "🔙Back":
                        await bot.SendTextMessageAsync(ChatId, "Main menu:", replyMarkup: MainMenu, cancellationToken: ct);
                        break;


                    

                default:

                        //await bot.SendTextMessageAsync(ChatId, $"✅ Задача '{taskText}' на {taskTime} сохранена.", cancellationToken: ct);

                        await bot.SendTextMessageAsync(ChatId, "Error", cancellationToken: ct);

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

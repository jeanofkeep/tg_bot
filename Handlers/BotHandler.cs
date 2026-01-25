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
using tg_bot.Data;
using tg_bot.Models;
using static tg_bot.Keyboards.MainKeyboards;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Options;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel.Design;
using System.Threading.Channels;
using tg_bot.State;
using StackExchange.Redis;
using tg_bot.Services;

namespace tg_bot.Handlers
{
    public class BotHandlers
    {
        private readonly BotDbContext _db;

        private readonly RedisService _redis;

        public BotHandlers(BotDbContext db, RedisService redis)
        {
            _db = db;
            _redis = redis;
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
            if (state.CurrentState == UserStateType.AwaitingTaskText)
            {
                state.TempText = text;
                state.CurrentState = UserStateType.AwaitingTaskTime;
                await _redis.SetUserStateAsync(state);

                await bot.SendTextMessageAsync(ChatId, "Enter the time:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                return;
            }

            // WAITING FOR TASK TIME
            if (state.CurrentState == UserStateType.AwaitingTaskTime)
            {
                if (DateTime.TryParse(text, out DateTime parsedTime))
                {
                    var reminderDate = DateTime.Today.Add(parsedTime.TimeOfDay).ToUniversalTime();

                    _db.UserMessages.Add(new UserMessage
                    {
                        UserId = ChatId,
                        Text = state.TempText ?? "No description",
                        Time = reminderDate,
                        ReminderDateTime = DateTime.UtcNow
                    });

                    var savedText = state.TempText ?? "No description";
                    await _db.SaveChangesAsync(ct);

                    state.CurrentState = UserStateType.None;
                    state.TempText = null;
                    await _redis.SetUserStateAsync(state);

                    await bot.SendTextMessageAsync(
                        ChatId,
                        $"Task '{savedText}' saved for {reminderDate:t}",
                        replyMarkup: SectionTasks,
                        cancellationToken: ct
                    );
                }
                else
                {
                    await bot.SendTextMessageAsync(ChatId, "Invalid time format. Try again:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                }

                return;
            }

            // WAITING FOR PROJECT NAME
            if (state.CurrentState == UserStateType.AwaitingProjectName)
            {
                _db.UserProjects.Add(new UserProject
                {
                    UserId = ChatId,
                    Text = text
                });
                await _db.SaveChangesAsync(ct);

                state.CurrentState = UserStateType.None;
                state.TempProject = null;
                await _redis.SetUserStateAsync(state);

                await bot.SendTextMessageAsync(ChatId, $"Project '{text}' saved!", replyMarkup: SectionProjects, cancellationToken: ct);
                return;
            }

            // WAITING FOR PURCHASE NAME
            if (state.CurrentState == UserStateType.AwaitingPurchaseName)
            {
                _db.UserPurchases.Add(new UserPurchase
                {
                    UserId = ChatId,
                    Text = text
                });
                await _db.SaveChangesAsync(ct);

                state.CurrentState = UserStateType.None;
                state.TempPurchase = null;
                await _redis.SetUserStateAsync(state);

                await bot.SendTextMessageAsync(ChatId, $"Purchase '{text}' saved!", replyMarkup: SectionShopping, cancellationToken: ct);
                return;
            }

            //task delete
            if (state.CurrentState == UserStateType.AwaitingTaskDelete) 
            {   
                if (int.TryParse(text, out int taskNumber)) 
                { 
                    var tasks = _db.UserMessages 
                    .Where(x => x.UserId == ChatId) 
                    .OrderBy(x => x.Time) 
                    .ToList(); 

                    if (taskNumber >= 1 && taskNumber <= tasks.Count) 
                    { 
                        var taskToDelete = tasks[taskNumber - 1]; 
                        _db.UserMessages.Remove(taskToDelete); 
                        await _db.SaveChangesAsync(ct); 

                        state.CurrentState = UserStateType.None; 

                        await _redis.SetUserStateAsync(state); 
                        await bot.SendTextMessageAsync(ChatId, "Task deleted successfully!", replyMarkup: SectionTasks, cancellationToken: ct); 
                    } 
                    else 
                    {
                        state.CurrentState = UserStateType.None; 
                        await _redis.SetUserStateAsync(state); 
                        await bot.SendTextMessageAsync(ChatId, "Invalid task number. Returning to main menu.", replyMarkup: SectionTasks, cancellationToken: ct); 
                    } 
                    } 
                    else 
                    {   state.CurrentState = UserStateType.None; 
                        await _redis.SetUserStateAsync(state); 
                        await bot.SendTextMessageAsync(ChatId, "Please enter a valid number. Returning to main menu.", replyMarkup: SectionTasks, cancellationToken: ct); 
                    } 
                return; 
            }

            //project delete
            if (state.CurrentState == UserStateType.AwaitingProjectDelete) 
            { 
                if (int.TryParse(text, out int projectNumber)) 
                { 
                    var projects = _db.UserProjects 
                    .Where(x => x.UserId == ChatId) 
                    .OrderBy(x => x.ProjectId) 
                    .ToList();

                    if (projectNumber >= 1 && projectNumber <= projects.Count) 
                    { 
                        var projectToDelete = projects[projectNumber - 1]; 
                        _db.UserProjects.Remove(projectToDelete); 
                        await _db.SaveChangesAsync(ct); 
                        state.CurrentState = UserStateType.None; 
                        await _redis.SetUserStateAsync(state); 
                        await bot.SendTextMessageAsync(ChatId, "Project deleted successfully!", replyMarkup: SectionProjects, cancellationToken: ct); 
                    } 
                    else 
                    { 
                        state.CurrentState = UserStateType.None; 
                        await _redis.SetUserStateAsync(state); 
                        await bot.SendTextMessageAsync(ChatId, "Invalid project number. Returning to main menu.", replyMarkup: SectionProjects, cancellationToken: ct); 
                    } 

                    }
                    else 

                    { 
                        state.CurrentState = UserStateType.None; 
                        await _redis.SetUserStateAsync(state); 
                        await bot.SendTextMessageAsync(ChatId, "Please enter a valid number. Returning to main menu.", replyMarkup: SectionProjects, cancellationToken: ct); 
                    } 
                    return; 
                }

            //purchase delete
            if (state.CurrentState == UserStateType.AwaitingPurchaseDelete)
            {
                if (int.TryParse(text, out int purchaseNumber))
                {
                    var purchase = _db.UserPurchases
                        .Where(x => x.UserId == ChatId)
                        .OrderBy(x => x.PurchaseId)
                        .ToList();

                    if (purchaseNumber >= 1 && purchaseNumber <= purchase.Count)
                    {
                        var purchaseToDelete = purchase[purchaseNumber - 1];
                        _db.UserPurchases.Remove(purchaseToDelete);
                        await _db.SaveChangesAsync(ct);

                        state.CurrentState = UserStateType.None;
                        await _redis.SetUserStateAsync(state);

                        await bot.SendTextMessageAsync(ChatId, "Purchase deleted successfully!", replyMarkup: SectionShopping, cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(ChatId, "Invalid number.", cancellationToken: ct);
                    }
                }
                else
                {
                    await bot.SendTextMessageAsync(ChatId, "Please enter a valid number.", cancellationToken: ct);
                }
                return;
            }

            // MAIN SWITCH
            switch (text)
            {
                case "/start":
                    await bot.SendTextMessageAsync(ChatId, "Main menu:", replyMarkup: MainMenu, cancellationToken: ct);
                    break;

                case "📋Tasks":
                    state.CurrentSection = "Tasks";
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(ChatId, "📋Tasks", replyMarkup: SectionTasks, cancellationToken: ct);
                    return;

                case "📝Create task":
                    state.CurrentState = UserStateType.AwaitingTaskText;
                    state.TempText = null;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(ChatId, "📝Create task:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;

                case "🎯My tasks":
                    var userTasks = _db.UserMessages
                        .Where(x => x.UserId == ChatId)
                        //.OrderByDescending(m => m.Time)
                        .OrderBy(m => m.Time)
                        .ToList();

                    for (int i = 0; i < userTasks.Count; i++)
                    {
                        userTasks[i].TaskId = i + 1;
                    }
                    await _db.SaveChangesAsync(ct);

                    if (userTasks.Count == 0)
                    {
                        await bot.SendTextMessageAsync(ChatId, "You don't have tasks", cancellationToken: ct);
                    }
                    else
                    {
                        var sb = new StringBuilder("Your tasks:\n\n");
                        foreach (var task in userTasks)
                            sb.AppendLine($"{task.TaskId}.*[{task.Time:t}]*{task.DueDate} - {task.Text}");

                        await bot.SendTextMessageAsync(ChatId, sb.ToString(),
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: ct);
                    }
                    break;

                case "📁Projects":
                    state.CurrentSection = "Projects";
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(ChatId, "📁Projects", replyMarkup: SectionProjects, cancellationToken: ct);
                    return;

                case "➕Create projects":
                    state.CurrentState = UserStateType.AwaitingProjectName;
                    state.TempProject = null;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(ChatId, "Enter project name:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;

                case "📒My Projects":
                    var userProjects = _db.UserProjects
                        .Where(p => p.UserId == ChatId)
                        .OrderBy(p => p.Id)
                        .ToList();
                    
                    for (int i = 0; i < userProjects.Count; i++)
                    {
                        userProjects[i].ProjectId = i + 1;
                    }
                    await _db.SaveChangesAsync(ct);

                    if (!userProjects.Any())
                    {
                        await bot.SendTextMessageAsync(ChatId, "You don't have projects.", cancellationToken: ct);
                    }
                    else
                    {
                        var sb = new StringBuilder("Your projects:\n\n");
                        foreach (var p in userProjects)
                            sb.AppendLine($"{p.ProjectId}.{p.Text}");

                        await bot.SendTextMessageAsync(ChatId, sb.ToString(), cancellationToken: ct);
                    }
                    break;

                case "🛍Shopping list":
                    state.CurrentSection = "SectionShopping";
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(ChatId, "Purchases", replyMarkup: SectionShopping, cancellationToken: ct);
                    return;

                case "➕Add purchases":
                    state.CurrentState = UserStateType.AwaitingPurchaseName;
                    state.TempPurchase = null;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(ChatId, "Enter purchases name:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;

                case "📝My purchases":
                    var userPurchases = _db.UserPurchases
                        .Where(p => p.UserId == ChatId)
                        .OrderBy(p => p.Id)
                        .ToList();
                    
                    for (int i = 0; i < userPurchases.Count; i++)
                    {
                        userPurchases[i].PurchaseId = i + 1;
                    }
                    await _db.SaveChangesAsync(ct);

                    if (!userPurchases.Any())
                    {
                        await bot.SendTextMessageAsync(ChatId, "You don't have purchases.", cancellationToken: ct);
                    }
                    else
                    {
                        var sb = new StringBuilder("Your purchases:\n\n");
                        foreach (var p in userPurchases)
                            sb.AppendLine($"{p.PurchaseId}.{p.Text}");

                        await bot.SendTextMessageAsync(ChatId, sb.ToString(), cancellationToken: ct);
                    }
                    break;

                case "❌Delete task":
                    state.CurrentState = UserStateType.AwaitingTaskDelete;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(ChatId, "Enter task number to delete:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;

                case "❌Delete project":
                    state.CurrentState = UserStateType.AwaitingProjectDelete;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(ChatId, "Enter project number to delete:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;

                case "❌Delete purchases":
                    state.CurrentState = UserStateType.AwaitingPurchaseDelete;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(ChatId, "Enter purchase number to delete:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;

                case "SectionShopping":
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
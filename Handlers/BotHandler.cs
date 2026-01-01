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
//using Telegram.Bots.Types;

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
            if (update.Type != Telegram.Bot.Types.Enums.UpdateType.Message || update.Message == null)
                return;

            if (update.Message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return;

            var ChatId = update.Message.Chat.Id;
            var TaskId = update.Message.Chat.Id;
            var text = update.Message.Text;

            //var state = _db.UserStates.FirstOrDefault(s => s.UserId == ChatId);
            var state = await _redis.GetUserStateAsync(ChatId);


            if (text == "↩️Cancel" || text == "🔙Back")
            {
                state.CurrentState = UserStateType.None;
                state.TempText = null;
                state.TempProject = null;
                state.TempPurchase = null;

                await _db.SaveChangesAsync(ct);
                await bot.SendTextMessageAsync(
                ChatId,
                "Main menu:",
                replyMarkup: MainMenu,
                cancellationToken: ct
                );
                return;
            }

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

                //_db.UserStates.Add(state);
                await _redis.SetUserStateAsync(state);
                //await _db.SaveChangesAsync(ct);
            }





            // WAITING FOR TASK TEXT
            // 
            if (state.CurrentState == UserStateType.AwaitingTaskText)
            {
                state.TempText = text;

                state.CurrentState = UserStateType.AwaitingTaskTime;
                await _db.SaveChangesAsync(ct);
                await bot.SendTextMessageAsync(ChatId, "Enter the time:", cancellationToken: ct);



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
                        Text = state.TempText,
                        Time = reminderDate,
                        ReminderDateTime = DateTime.UtcNow
                    });

                    var savedText = state.TempText;

                    //state.IsAwaitingTime = false;
                    state.TempText = null;

                    await _db.SaveChangesAsync(ct);

                    await bot.SendTextMessageAsync(
                        ChatId,
                        $"Task '{savedText}' saved for {reminderDate:t}",
                        cancellationToken: ct
                    );
                    state.CurrentState = UserStateType.None;
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


                await _db.SaveChangesAsync(ct);

                await bot.SendTextMessageAsync(ChatId, $"Project '{text}' saved!", cancellationToken: ct);
                return;
            }

            //WAITING FOR PURCHASE NAME

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


                await _db.SaveChangesAsync(ct);

                await bot.SendTextMessageAsync(ChatId, $"Purchase '{text}' saved!", cancellationToken: ct);
                return;
            }

            //DELETING
            if (state.CurrentState == UserStateType.AwaitingTaskDelete)
            {
                if (int.TryParse(text, out int taskNumber))
                {
                    var tasks = _db.UserMessages
                        .Where(x => x.UserId == ChatId)
                        .OrderByDescending(x => x.Time)
                        .ToList();

                    if (taskNumber >= 1 && taskNumber <= tasks.Count)
                    {
                        var taskToDelete = tasks[taskNumber - 1];

                        _db.UserMessages.Remove(taskToDelete);
                        await _db.SaveChangesAsync(ct);

                        // отключаем состояние
                        //state.IsAwaitingTaskDelete = false;
                        state.CurrentState = UserStateType.None;
                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId,
                            "Task deleted successfully!",
                            cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(ChatId,
                            "Invalid task number.",
                            cancellationToken: ct);
                    }


                }
                else
                {
                    await bot.SendTextMessageAsync(ChatId,
                        "Please enter a valid number.",
                        cancellationToken: ct);
                }
                return;
            }
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

                        // отключаем состояние
                        state.CurrentState = UserStateType.None;

                        //state.IsAwaitingProjectDelete = false;
                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId,
                            "Project deleted successfully!",
                            cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(ChatId,
                            "Invalid project number.",
                            cancellationToken: ct);
                    }
                }
                else
                {
                    await bot.SendTextMessageAsync(ChatId,
                        "Please enter a valid number.",
                        cancellationToken: ct);
                }

                return;

                //DELETING PURCHASES

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
                            //ошибка при удалении!!!!!!
                            var purchaseToDelete = purchase[purchaseNumber - 1];

                            _db.UserPurchases.Remove(purchaseToDelete);
                            await _db.SaveChangesAsync(ct);

                            // отключаем состояние
                            state.CurrentState = UserStateType.None;

                            //state.IsAwaitingProjectDelete = false;
                            await _db.SaveChangesAsync(ct);

                            await bot.SendTextMessageAsync(ChatId,
                                "Purchase deleted successfully!",
                                cancellationToken: ct);
                        }
                        else
                        {
                            await bot.SendTextMessageAsync(ChatId,
                                "Invalid number.",
                                cancellationToken: ct);
                        }
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(ChatId,
                            "Please enter a valid number.",
                            cancellationToken: ct);
                    }

                    return;

                }
            }


                // MAIN SWITCH
                switch (text)
                {
                    case "/start":
                        await bot.SendTextMessageAsync(ChatId, "Main menu:", replyMarkup: MainMenu, cancellationToken: ct);
                        break;

                    case "📋Tasks":
                        state.CurrentSection = "Tasks";
                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId, "📋Tasks", replyMarkup: SectionTasks, cancellationToken: ct);
                        return;
                    ///////////23.11.25
                    case "📝Create task":
                        state.CurrentState = UserStateType.AwaitingTaskText;

                        state.TempText = null;

                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId, "📝Create task:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                        ;
                        return;

                    case "🎯My tasks":
                        var userTasks = _db.UserMessages
                            .Where(x => x.UserId == ChatId)
                            .OrderByDescending(m => m.Time)
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
                                sb.AppendLine($"{task.TaskId}.*[{task.Time:t}]*.{task.DueDate} - {task.Text}");

                            await bot.SendTextMessageAsync(ChatId, sb.ToString(),
                                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: ct);
                        }

                        break;

                    case "📁Projects":
                        state.CurrentSection = "Projects";
                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId, "📁Projects", replyMarkup: SectionProjects, cancellationToken: ct);

                        return;


                    case "➕Create projects":
                        state.CurrentState = UserStateType.AwaitingProjectName;
                        state.TempProject = null;

                        await _db.SaveChangesAsync(ct);

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


                    //case "🔙Back":
                    case "↩️Cancel":
                        state.CurrentState = UserStateType.None;

                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(
                            ChatId,
                            "Main menu:",
                            replyMarkup: MainMenu,
                            cancellationToken: ct
                            );
                        return;

                    case "❌Delete task":
                        //state.IsAwaitingTaskDelete = true;

                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId, "Enter the number task for deleting:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                        break;

                    case "❌Delete project":
                        //state.IsAwaitingProjectDelete = true;

                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId, "Enter the number task for deleting:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                        break;

                    case "🔔Notifications":
                        await bot.SendTextMessageAsync(ChatId, "🚧Section is under development 🚧", replyMarkup: SectionNotifications, cancellationToken: ct);

                        break;

                    //case "On":
                    //await bot.SendTextMessageAsync(ChatId, "🚧This section is under development 🚧", replyMarkup: MainMenu, cancellationToken: ct);
                    //break;
                    case "🛍Shopping list":
                        state.CurrentSection = "SectionShopping";
                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId, "🛍Shopping list", replyMarkup: SectionShopping, cancellationToken: ct);

                        return;

                    case "➕Add purchases":
                        state.CurrentState = UserStateType.AwaitingPurchaseName;
                        state.TempPurchase = null;

                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId, "Add new purchase:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);


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
                            var sb = new StringBuilder("Your list:\n\n");
                            foreach (var p in userPurchases)
                                sb.AppendLine($"{p.PurchaseId}.{p.Text}");

                            await bot.SendTextMessageAsync(ChatId, sb.ToString(), cancellationToken: ct);
                        }
                        break;

                    case "❌Delete purchases":

                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId, "Enter the number of purchase for deleting:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                        break;

                    //"📝My purchases", "➕Add purchases", "❌Delete purchases"
                    default:
                        await bot.SendTextMessageAsync(ChatId, "Unknown command", cancellationToken: ct);
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


/*
 * 
 * /*
            if (state.IsAwaitingDate)
            {
                if (DateTime.TryParse(text, out DateTime dueDate))
                {
                    // сохраняем дату в последнюю созданную задачу
                    var task = _db.UserMessages
                        .Where(x => x.UserId == ChatId)
                        .OrderByDescending(x => x.Time)
                        .FirstOrDefault();

                    if (task != null)
                    {
                        task.DueDate = dueDate;
                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId,
                            $"Task due date set: {dueDate:yyyy-MM-dd}",
                            cancellationToken: ct);
                    }

                    state.IsAwaitingDate = false;
                    await _db.SaveChangesAsync(ct);
                }
                else
                {
                    await bot.SendTextMessageAsync(ChatId,
                        "Invalid date format. Please enter YYYY-MM-DD.",
                        cancellationToken: ct);
                }
                return; // останавливаем дальнейшую обработку сообщений
            }
            
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
        new KeyboardButton[]{ "My Projects", "Create Projects"},
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

            if (state.IsAwaitingProject)
            {
                // Сохраняем проект
                _db.UserProjects.Add(new UserProject
                {
                    UserId = ChatId,
                    Text = text,

                });

                state.IsAwaitingProject = false;
                state.TempProject = null;
                await _db.SaveChangesAsync(ct);

                await bot.SendTextMessageAsync(ChatId, $"Project '{text}' saved!", cancellationToken: ct);
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
                            sb.AppendLine($"[*{task.Time:t}*] - {task.Text}");
                        }
                        await bot.SendTextMessageAsync(ChatId, sb.ToString(), parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: ct);
                    }
                    break;

                case "Projects":
                    await bot.SendTextMessageAsync(ChatId, "Projects", replyMarkup: SectionProjects, cancellationToken: ct);
                    break;

                case "Create projects":
                    await bot.SendTextMessageAsync(ChatId, "Enter project name:", cancellationToken: ct);
                    state.IsAwaitingProject = true;
                    state.TempProject = null;
                    await _db.SaveChangesAsync(ct);
                    break;

                case "My Projects":
                    var userProjects = _db.UserProjects
                        .Where(p => p.UserId == ChatId)
                        .OrderBy(p => p.Id)
                        .ToList();

                    if (userProjects.Count == 0)
                    {
                        await bot.SendTextMessageAsync(ChatId, "You don't have projects.", cancellationToken: ct);
                    }
                    else
                    {
                        var sb = new StringBuilder("Your projects:\n\n");
                        foreach (var project in userProjects)
                        {
                            sb.AppendLine($"- {project.Text}");
                        }
                        await bot.SendTextMessageAsync(ChatId, sb.ToString(), cancellationToken: ct);
                    }
                    break;

                case "🔙Back":
                     await bot.SendTextMessageAsync(ChatId, "Main menu:", replyMarkup: MainMenu, cancellationToken: ct);
                     break;
 

                    default:

                        //await bot.SendTextMessageAsync(ChatId, $"✅ Задача '{taskText}' на {taskTime} сохранена.", cancellationToken: ct);

                        //await bot.SendTextMessageAsync(ChatId, "Error", cancellationToken: ct);

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
*/
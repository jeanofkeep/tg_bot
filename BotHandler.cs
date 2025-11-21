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
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Options;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel.Design;
using System.Threading.Channels;

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
            new KeyboardButton[]{ "📋Tasks", "📁Projects", "🔔Notifications" }
        })
        {
            ResizeKeyboard = true
        };

        private static ReplyKeyboardMarkup SectionTasks = new(new[]
        {
            new KeyboardButton[]{ "🎯My tasks", "📝Create task", "❌Delete task"},
            new KeyboardButton[]{ "🔙Back" }
        })
        {
            ResizeKeyboard = true
        };

        private static ReplyKeyboardMarkup SectionProjects = new(new[]
        {
            new KeyboardButton[]{ "📒My Projects", "➕Create projects", "❌Delete project"},
            new KeyboardButton[]{ "🔙Back" }
        })
        {
            ResizeKeyboard = true
        };

        private static ReplyKeyboardMarkup SectionNotifications = new (new[]
        {
            new KeyboardButton[]{ "On", "Off"},
            new KeyboardButton[]{ "🔙Back" }
        })
        {
            ResizeKeyboard = true
        };

        private static ReplyKeyboardMarkup cancelKeyboardMarkup = new(new[]
        {
            new KeyboardButton[]{ "↩️Cancel"},
            //new KeyboardButton[]{ "🔙Back" }
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
            var TaskId = update.Message.Chat.Id;
            var text = update.Message.Text;

            var state = _db.UserStates.FirstOrDefault(s => s.UserId == ChatId);

            if (text == "↩️Cancel")
            {
                state.IsAwaitingTime = false;
                state.IsAwaitingText = false;
                state.IsAwaitingProject = false;
                state.IsAwaitingTaskDelete = false;
                state.IsAwaitingProjectDelete = false;

                await _db.SaveChangesAsync(ct);


                switch (state.CurrentSection)
                {
                    case "Projects":
                        await bot.SendTextMessageAsync(ChatId, "📁Projects", replyMarkup: SectionProjects);
                     return;

                    case "Tasks":
                        await bot.SendTextMessageAsync(ChatId, "📋Tasks", replyMarkup: SectionTasks);
                    return;


                    default:
                        await bot.SendTextMessageAsync(ChatId, "Main menu", replyMarkup: MainMenu);
                        return;
                }
                

            }

            if (state == null)
            {
                state = new UserState
                {
                    UserId = ChatId,
                    IsAwaitingText = false,
                    IsAwaitingTime = false,
                    IsAwaitingProject = false,
                    TempText = null,
                    TempProject = null
                };

                _db.UserStates.Add(state);

                await _db.SaveChangesAsync(ct);
            }

            // WAITING FOR TASK TEXT
            if (state.IsAwaitingText)
            {
                state.TempText = text;
                state.IsAwaitingText = false;
                state.IsAwaitingTime = true;

                await _db.SaveChangesAsync(ct);

                await bot.SendTextMessageAsync(ChatId, "Enter the time:", cancellationToken: ct);
                return;
            }

            // WAITING FOR TASK TIME
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
                        ReminderDateTime = DateTime.UtcNow
                    });

                    var savedText = state.TempText;

                    state.IsAwaitingTime = false;
                    state.TempText = null;

                    await _db.SaveChangesAsync(ct);

                    await bot.SendTextMessageAsync(
                        ChatId,
                        $"Task '{savedText}' saved for {reminderDate:t}",
                        cancellationToken: ct
                    );
                }

                return;
            }

            // WAITING FOR PROJECT NAME
            if (state.IsAwaitingProject)
            {
                _db.UserProjects.Add(new UserProject
                {
                    UserId = ChatId,
                    Text = text
                });
                await _db.SaveChangesAsync(ct);

                state.IsAwaitingProject = false;
                state.TempProject = null;

                await _db.SaveChangesAsync(ct);

                await bot.SendTextMessageAsync(ChatId, $"Project '{text}' saved!", cancellationToken: ct);
                return;
            }

            if (state.IsAwaitingTaskDelete)
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

                        //state off
                        state.IsAwaitingTaskDelete = false;
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
                if (state.IsAwaitingProjectDelete)
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

                            //state off
                            state.IsAwaitingProjectDelete = false;
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

                    case "📝Create task":
                        await bot.SendTextMessageAsync(ChatId, "📝Create task:", replyMarkup: cancelKeyboardMarkup,  cancellationToken: ct);
                        state.IsAwaitingText = true;
                        state.IsAwaitingTime = false;
                        state.TempText = null;
                        await _db.SaveChangesAsync(ct);
                        break;

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
                                sb.AppendLine($"{task.TaskId}.*[{task.Time:t}]* - {task.Text}");

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
                        state.IsAwaitingProject = true;
                        await _db.SaveChangesAsync(ct);

                        await bot.SendTextMessageAsync(ChatId, "Enter project name:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                        
                        state.TempProject = null;
                        await _db.SaveChangesAsync(ct);
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


                case "🔙Back":
                    await bot.SendTextMessageAsync(ChatId, "Main menu:", replyMarkup: MainMenu, cancellationToken: ct);
                    break;

                case "❌Delete task":
                    state.IsAwaitingTaskDelete = true;
                    
                    await _db.SaveChangesAsync(ct);
                    await bot.SendTextMessageAsync(ChatId, "Enter the number task for deleting:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    break;

                case "❌Delete project":
                    state.IsAwaitingProjectDelete = true;
                    await _db.SaveChangesAsync(ct);
                    await bot.SendTextMessageAsync(ChatId, "Enter the number task for deleting:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    break;

                case "🔔Notifications":
                    await bot.SendTextMessageAsync(ChatId, "🚧This section is under development 🚧", replyMarkup: SectionNotifications, cancellationToken: ct);

                    break;

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
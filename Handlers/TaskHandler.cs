using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Globalization;
using System.Text;
using tg_bot.State;
using tg_bot.Services;
using tg_bot.Data;
using tg_bot.Models;
using static tg_bot.Keyboards.MainKeyboards;

namespace tg_bot.Handlers
{
    public class TaskHandler
    {
        private readonly BotDbContext _db;
        private readonly RedisService _redis;

        public TaskHandler(BotDbContext db, RedisService redis)
        {
            _db = db;
            _redis = redis;
        }

        public async Task HandleAsync(ITelegramBotClient bot, long chatId, string text, UserState state, CancellationToken ct)
        {
            if (state.CurrentState == UserStateType.AwaitingTaskText)
            {
                state.TempText = text;
                state.CurrentState = UserStateType.AwaitingTaskDate;
                await _redis.SetUserStateAsync(state);
                await bot.SendTextMessageAsync(chatId, "Enter the date (dd.MM.yyyy):", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                return;
            }

            if (state.CurrentState == UserStateType.AwaitingTaskDate)
            {
                if (!DateTime.TryParseExact(text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    await bot.SendTextMessageAsync(chatId, "Invalid date format. Use dd.MM.yyyy", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;
                }
                state.TempDate = parsedDate;
                state.CurrentState = UserStateType.AwaitingTaskTime;
                await _redis.SetUserStateAsync(state);
                await bot.SendTextMessageAsync(chatId, "Enter the time (HH:mm):", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                return;
            }

            if (state.CurrentState == UserStateType.AwaitingTaskTime)
            {
                if (!TimeSpan.TryParse(text, out var parsedTime))
                {
                    await bot.SendTextMessageAsync(chatId, "Invalid time format. Use HH:mm", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;
                }
                if (state.TempDate == null)
                {
                    await bot.SendTextMessageAsync(chatId, "Date was not set. Please try again.");
                    return;
                }
                var taskDateTime = state.TempDate.Value.Date + parsedTime;
                _db.UserMessages.Add(new UserMessage
                {
                    UserId = chatId,
                    Text = state.TempText ?? "No description",
                    TaskDateTime = taskDateTime.ToUniversalTime(),
                    ReminderDateTime = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(ct);
                var savedText = state.TempText;
                state.CurrentState = UserStateType.None;
                state.TempText = null;
                state.TempDate = null;
                await _redis.SetUserStateAsync(state);
                await bot.SendTextMessageAsync(chatId, $"Task '{savedText}' saved for {taskDateTime:dd.MM HH:mm}", replyMarkup: SectionTasks, cancellationToken: ct);
                return;
            }

            if (state.CurrentState == UserStateType.AwaitingTaskDelete)
            {
                if (int.TryParse(text, out int taskNumber))
                {
                    var tasks = _db.UserMessages
                        .Where(x => x.UserId == chatId)
                        .OrderBy(x => x.TaskDateTime)
                        .ToList();
                    if (taskNumber >= 1 && taskNumber <= tasks.Count)
                    {
                        _db.UserMessages.Remove(tasks[taskNumber - 1]);
                        await _db.SaveChangesAsync(ct);
                        state.CurrentState = UserStateType.None;
                        await _redis.SetUserStateAsync(state);
                        await bot.SendTextMessageAsync(chatId, "Task deleted successfully!", replyMarkup: SectionTasks, cancellationToken: ct);
                    }
                    else
                    {
                        state.CurrentState = UserStateType.None;
                        await _redis.SetUserStateAsync(state);
                        await bot.SendTextMessageAsync(chatId, "Invalid task number.", replyMarkup: SectionTasks, cancellationToken: ct);
                    }
                }
                else
                {
                    state.CurrentState = UserStateType.None;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(chatId, "Please enter a valid number.", replyMarkup: SectionTasks, cancellationToken: ct);
                }
                return;
            }

            switch (text)
            {
                case "📋Tasks":
                    state.CurrentSection = "Tasks";
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(chatId, "📋Tasks", replyMarkup: SectionTasks, cancellationToken: ct);
                    return;

                case "📝Create task":
                    state.CurrentState = UserStateType.AwaitingTaskText;
                    state.TempText = null;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(chatId, "📝Create task:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;

                case "🎯My tasks":
                    var userTasks = _db.UserMessages
                        .Where(x => x.UserId == chatId)
                        .OrderBy(x => x.TaskDateTime)
                        .ToList();
                    for (int i = 0; i < userTasks.Count; i++)
                        userTasks[i].TaskId = i + 1;
                    await _db.SaveChangesAsync(ct);
                    if (userTasks.Count == 0)
                    {
                        await bot.SendTextMessageAsync(chatId, "You don't have tasks", cancellationToken: ct);
                    }
                    else
                    {
                        var sb = new StringBuilder("🗂 Your tasks\n\n");
                        var groupedByDate = userTasks
                            .GroupBy(t => t.TaskDateTime.ToLocalTime().Date)
                            .OrderBy(g => g.Key);
                        int globalIndex = 1;
                        foreach (var group in groupedByDate)
                        {
                            sb.AppendLine($"——— {group.Key.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)} ———");
                            foreach (var task in group.OrderBy(t => t.TaskDateTime))
                            {
                                sb.AppendLine($"{globalIndex}. [{task.TaskDateTime.ToLocalTime():HH:mm}] — {task.Text}");
                                globalIndex++;
                            }
                            sb.AppendLine();
                        }
                        await bot.SendTextMessageAsync(chatId, sb.ToString().TrimEnd(), cancellationToken: ct);
                    }
                    break;

                case "❌Delete task":
                    state.CurrentState = UserStateType.AwaitingTaskDelete;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(chatId, "Enter task number to delete:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;
            }
        }
    }
}
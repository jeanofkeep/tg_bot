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
    public class ProjectHandler
    {
        private readonly BotDbContext _db;
        private readonly RedisService _redis;

        public ProjectHandler(BotDbContext db, RedisService redis)
        {
            _db = db;
            _redis = redis;
        }

        public async Task HandleAsync(ITelegramBotClient bot, long chatId, string text, UserState state, CancellationToken ct)
        {
            if (state.CurrentState == UserStateType.AwaitingProjectName)
            {
                _db.UserProjects.Add(new UserProject
                {
                    UserId = chatId,
                    Text = text
                });
                await _db.SaveChangesAsync(ct);

                state.CurrentState = UserStateType.None;
                state.TempProject = null;
                await _redis.SetUserStateAsync(state);
                await bot.SendTextMessageAsync(chatId, $"Project '{text}' saved!", replyMarkup: SectionProjects, cancellationToken: ct);
                return;
            }
                //delete
            if (state.CurrentState == UserStateType.AwaitingProjectDelete) 
            { 
                if (int.TryParse(text, out int projectNumber)) 
                { 
                    var projects = _db.UserProjects 
                    .Where(x => x.UserId == chatId) 
                    .OrderBy(x => x.ProjectId) 
                    .ToList();

                    if (projectNumber >= 1 && projectNumber <= projects.Count) 
                    { 
                        var projectToDelete = projects[projectNumber - 1]; 
                        _db.UserProjects.Remove(projectToDelete); 
                        await _db.SaveChangesAsync(ct); 
                        state.CurrentState = UserStateType.None; 
                        await _redis.SetUserStateAsync(state); 
                        await bot.SendTextMessageAsync(chatId, "Project deleted successfully!", replyMarkup: SectionProjects, cancellationToken: ct); 
                    } 
                    else 
                    { 
                        state.CurrentState = UserStateType.None; 
                        await _redis.SetUserStateAsync(state); 
                        await bot.SendTextMessageAsync(chatId, "Invalid project number. Returning to main menu.", replyMarkup: SectionProjects, cancellationToken: ct); 
                    } 
                    }
                    else 
                    { 
                        state.CurrentState = UserStateType.None; 
                        await _redis.SetUserStateAsync(state); 
                        await bot.SendTextMessageAsync(chatId, "Please enter a valid number. Returning to main menu.", replyMarkup: SectionProjects, cancellationToken: ct); 
                    } 
                    return; 
            }    

            switch (text)
            {
                case "📁Projects":
                    state.CurrentSection = "Projects";
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(chatId, "📁Projects", replyMarkup: SectionProjects, cancellationToken: ct);
                    return;

                case "➕Create projects":
                    state.CurrentState = UserStateType.AwaitingProjectName;
                    state.TempProject = null;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(chatId, "Enter project name:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;

                case "📒My Projects":
                    var userProjects = _db.UserProjects
                        .Where(p => p.UserId == chatId)
                        .OrderBy(p => p.Id)
                        .ToList();
                    
                    for (int i = 0; i < userProjects.Count; i++)
                    {
                        userProjects[i].ProjectId = i + 1;
                    }
                    await _db.SaveChangesAsync(ct);

                    if (!userProjects.Any())
                    {
                        await bot.SendTextMessageAsync(chatId, "You don't have projects.", cancellationToken: ct);
                    }
                    else
                    {
                        var sb = new StringBuilder("Your projects:\n\n");
                        foreach (var p in userProjects)
                            sb.AppendLine($"{p.ProjectId}.{p.Text}");

                        await bot.SendTextMessageAsync(chatId, sb.ToString(), cancellationToken: ct);
                    }
                    break;

                case "❌Delete project":
                    state.CurrentState = UserStateType.AwaitingProjectDelete;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(chatId, "Enter project number to delete:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                return;
            }
        }
    }
}
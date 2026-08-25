using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using tg_bot.Data;

namespace tg_bot.Services
{
    public class NotificationService
    {
        private readonly DbContextOptions<BotDbContext> _dbOptions;
        private readonly ITelegramBotClient _bot;

        public NotificationService(DbContextOptions<BotDbContext> dbOptions, ITelegramBotClient bot)
        {
            _dbOptions = dbOptions;
            _bot = bot;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendNotifications(ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Notification error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
        }

        private async Task CheckAndSendNotifications(CancellationToken ct)
        {
            using var db = new BotDbContext(_dbOptions);

            var now = DateTime.UtcNow;

            var tasksDue = db.UserMessages
                .Where(t => t.TaskDateTime <= now && t.IsNotified == false)
                .ToList();

            foreach (var task in tasksDue)
            {
                var moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(task.TaskDateTime, moscowZone);

                await _bot.SendTextMessageAsync(
                    task.UserId,           
                    $"🔔 Reminder: {task.Text}\n🕐 {localTime:dd.MM HH:mm}",
                    cancellationToken: ct
                );

                task.IsNotified = true;
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
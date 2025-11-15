

/*
namespace tg_bot.Services
{
    public class ReminderService
    {
        private readonly ITelegramBotClient _bot;
        private readonly DbContextOptions<BotDbContext> _dbOptions;

        public ReminderService(ITelegramBotClient bot, DbContextOptions<BotDbContext> dbOptions)
        {
            _bot = bot;
            _dbOptions = dbOptions;
        }

        public async Task StartAsync()
        {
            while (true)
            {
                using (var db = new BotDbContext(_dbOptions))
                {
                    var now = DateTime.UtcNow;

                    var reminders = db.UserReminders
                        .Where(r => !r.IsSent && r.ReminderTime <= now)
                        .ToList();

                    foreach (var reminder in reminders)
                    {
                        await _bot.SendTextMessageAsync(reminder.UserId, $"🔔 Напоминание:\n\n{reminder.Text}");

                        reminder.IsSent = true;
                    }

                    db.SaveChanges();
                }

                await Task.Delay(5000); // проверка каждые 5 сек
            }
        }
    }

}
*/
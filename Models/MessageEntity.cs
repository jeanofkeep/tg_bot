using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tg_bot.Models
{
    
    public class UserMessage
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public string? Username { get; set; }
        public string Text { get; set; } = "";
        public DateTime Date { get; set; }
        public DateTime Time { get; set; }
        public DateTime? ReminderDateTime { get; set; }

    }

    public class UserState
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public bool IsAwaitingText { get; set; }
        public bool IsAwaitingTime { get; set; }
        public string? TempText { get; set; }

    }
}

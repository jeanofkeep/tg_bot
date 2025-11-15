using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tg_bot.Models
{
    public class UserReminder
    {
        public int Id { get; set; }

        public long UserId { get; set; }

        public string Text { get; set; }

        public DateTime ReminderTime { get; set; }

        public bool IsSent { get; set; } = false;
    }
}

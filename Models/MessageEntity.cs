using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tg_bot.Models
{
    
    public class MessageEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Text { get; set; } = "";
        public DateTime Date { get; set; }
        public DateTime? ReminderDateTime { get; set; }

    }
}

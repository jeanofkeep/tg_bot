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
        public int TaskId { get; set; }
        public long UserId { get; set; }
        public string? Username { get; set; }
        public string Text { get; set; } = "";
        public DateTime Date { get; set; }
        public DateTime Time { get; set; }
        public DateTime? ReminderDateTime { get; set; }
       
    }

    public class UserState
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public bool IsAwaitingText { get; set; }
        public bool IsAwaitingTime { get; set; }
        public string? TempText { get; set; }
        public string? TempProject { get; set; }
        public bool IsAwaitingProject { get; set; } = false;
        public bool IsAwaitingTaskDelete { get; set; }
        public bool IsAwaitingProjectDelete { get; set; }
        public string CurrentSection { get; set; } = "MainMenu";

    }

    public class UserProject
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public long UserId { get; set; }
        public string? Username { get; set; }
        public string Text { get; set; } = "";
        public DateTime Date { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace tg_bot.Keyboards
{
    public class MainKeyboards
    {
        public static ReplyKeyboardMarkup MainMenu = new(new[]
        {
            new KeyboardButton[]{ "📋Tasks", "📁Projects", "🛍Shopping list" },
	    new KeyboardButton[]{ "🔔Notifications" }
        })
        {
            ResizeKeyboard = true
        };

        public static ReplyKeyboardMarkup SectionTasks = new(new[]
        {
            new KeyboardButton[]{ "🎯My tasks", "📝Create task", "❌Delete task"},
            new KeyboardButton[]{ "🔙Back" }
        })
        {
            ResizeKeyboard = true
        };

        public static ReplyKeyboardMarkup SectionProjects = new(new[]
        {
            new KeyboardButton[]{ "📒My Projects", "➕Create projects", "❌Delete project"},
            new KeyboardButton[]{ "🔙Back" }
        })
        {
            ResizeKeyboard = true
        };

        public static ReplyKeyboardMarkup SectionShopping = new (new[]
        {
            new KeyboardButton[]{ "📝My purchases", "➕Add purchases", "❌Delete purchases"},
            new KeyboardButton[]{ "🔙Back" }
        })
        {
            ResizeKeyboard = true
        };

        public static ReplyKeyboardMarkup SectionNotifications = new(new[]
        {
            new KeyboardButton[]{ "On", "Off"},
            new KeyboardButton[]{ "🔙Back" }
        })
        {
            ResizeKeyboard = true
        };

        public static ReplyKeyboardMarkup cancelKeyboardMarkup = new(new[]
        {
            new KeyboardButton[]{ "↩️Cancel"},
            //new KeyboardButton[]{ "Skip" }
        })
        {
            ResizeKeyboard = true
        };
    }
}

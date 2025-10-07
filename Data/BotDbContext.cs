using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Telegram.Bot.Types;
using Microsoft.EntityFrameworkCore;
using tg_bot.Models;

namespace tg_bot.Data
{
    public class BotDbContext:DbContext
    {
        public DbSet<UserState> UserStates { get; set; }
        public DbSet<UserMessage> UserMessages {  get; set; }

        public BotDbContext(DbContextOptions<BotDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserMessage>()
                .HasKey(e => e.Id);


            modelBuilder.Entity<UserState>()
                .HasKey(e => e.Id);

        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace tg_bot.Data
{

        public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
        {
            public BotDbContext CreateDbContext(string[] args)
            {
                var optionsBuilder = new DbContextOptionsBuilder<BotDbContext>();

                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=botdb;Username=postgres;Password=12345678");

                return new BotDbContext(optionsBuilder.Options);
            }
        }
}

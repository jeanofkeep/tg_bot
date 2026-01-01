//using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tg_bot.Models;
using StackExchange.Redis;
using System.Text.Json;


using RedisDb = StackExchange.Redis.IDatabase;

namespace tg_bot.Services
{
    public class RedisService
    {
        private readonly IDatabase _db;

        public RedisService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        private static string GetUserKey(long userId)
            => $"user:state:{userId}";

        public async Task SetUserStateAsync(UserState state)
        {
            var json = JsonSerializer.Serialize(state);

            await _db.StringSetAsync(
                GetUserKey(state.UserId),
                json,
                TimeSpan.FromDays(1)
            );
        }

        public async Task<UserState?> GetUserStateAsync(long userId)
        {
            var value = await _db.StringGetAsync(GetUserKey(userId));

            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<UserState>(value!);
        }

        public async Task ClearUserStateAsync(long userId)
        {
            await _db.KeyDeleteAsync(GetUserKey(userId));
        }
    }

}

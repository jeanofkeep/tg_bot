using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Globalization;
using System.Text;
using tg_bot.State;
using tg_bot.Services;
using tg_bot.Data;
using tg_bot.Models;
using static tg_bot.Keyboards.MainKeyboards;

namespace tg_bot.Handlers
{
    public class PurchaseHandler
    {
        private readonly BotDbContext _db;
        private readonly RedisService _redis;

        public PurchaseHandler(BotDbContext db, RedisService redis)
        {
            _db = db;
            _redis = redis;
        }

        public async Task HandleAsync(ITelegramBotClient bot, long chatId, string text, UserState state, CancellationToken ct)
        {
            if (state.CurrentState == UserStateType.AwaitingPurchaseName)
            {
                _db.UserPurchases.Add(new UserPurchase
                {
                    UserId = chatId,
                    Text = text
                });
                await _db.SaveChangesAsync(ct);

                state.CurrentState = UserStateType.None;
                state.TempPurchase = null;
                await _redis.SetUserStateAsync(state);

                await bot.SendTextMessageAsync(chatId, $"Purchase '{text}' saved!", replyMarkup: SectionShopping, cancellationToken: ct);
                return;
            }

            if (state.CurrentState == UserStateType.AwaitingPurchaseDelete)
            {
                if (int.TryParse(text, out int purchaseNumber))
                {
                    var purchase = _db.UserPurchases
                        .Where(x => x.UserId == chatId)
                        .OrderBy(x => x.PurchaseId)
                        .ToList();

                    if (purchaseNumber >= 1 && purchaseNumber <= purchase.Count)
                    {
                        var purchaseToDelete = purchase[purchaseNumber - 1];
                        _db.UserPurchases.Remove(purchaseToDelete);
                        await _db.SaveChangesAsync(ct);

                        state.CurrentState = UserStateType.None;
                        await _redis.SetUserStateAsync(state);
                        await bot.SendTextMessageAsync(chatId, "Purchase deleted successfully!", replyMarkup: SectionShopping, cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(chatId, "Invalid number.", cancellationToken: ct);
                    }
                }
                else
                {
                    await bot.SendTextMessageAsync(chatId, "Please enter a valid number.", cancellationToken: ct);
                }
                return;
            }
            switch (text)
            {
                case "🛍Shopping list":
                    state.CurrentSection = "SectionShopping";
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(chatId, "Purchases", replyMarkup: SectionShopping, cancellationToken: ct);
                    return;

                case "➕Add purchases":
                    state.CurrentState = UserStateType.AwaitingPurchaseName;
                    state.TempPurchase = null;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(chatId, "Enter purchases name:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;

                case "📝My purchases":
                    var userPurchases = _db.UserPurchases
                        .Where(p => p.UserId == chatId)
                        .OrderBy(p => p.Id)
                        .ToList();
                    
                    for (int i = 0; i < userPurchases.Count; i++)
                    {
                        userPurchases[i].PurchaseId = i + 1;
                    }
                    await _db.SaveChangesAsync(ct);

                    if (!userPurchases.Any())
                    {
                        await bot.SendTextMessageAsync(chatId, "You don't have purchases.", cancellationToken: ct);
                    }
                    else
                    {
                        var sb = new StringBuilder("Your purchases:\n\n");
                        foreach (var p in userPurchases)
                            sb.AppendLine($"{p.PurchaseId}.{p.Text}");

                        await bot.SendTextMessageAsync(chatId, sb.ToString(), cancellationToken: ct);
                    }
                    break;

                case "❌Delete purchases":
                    state.CurrentState = UserStateType.AwaitingPurchaseDelete;
                    await _redis.SetUserStateAsync(state);
                    await bot.SendTextMessageAsync(chatId, "Enter purchase number to delete:", replyMarkup: cancelKeyboardMarkup, cancellationToken: ct);
                    return;
            }
        }
    }
}
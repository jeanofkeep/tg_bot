using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tg_bot.State
{
    
    public enum UserStateType
    {
        None = 0,
        AwaitingTaskText,
        AwaitingTaskTime,
        AwaitingTaskDate,
        AwaitingProjectName,
        AwaitingPurchaseName,
        AwaitingTaskDelete,
        AwaitingProjectDelete,
        AwaitingPurchaseDelete,
    }
}

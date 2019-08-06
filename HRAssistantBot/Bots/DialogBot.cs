using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using HRAssistantBot.Services;
using HRAssistantBot.Helpers;

namespace HRAssistantBot.Bots
{
    public class DialogBot<T> : ActivityHandler where T : Dialog
    {
        #region Variables
        protected readonly Dialog dialog;
        protected readonly HRBotStateService hrBotStateService;
        protected readonly ILogger logger;
        #endregion  

        public DialogBot(HRBotStateService botStateService, T dialog, ILogger<DialogBot<T>> logger)
        {
            hrBotStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            dialog = dialog ?? throw new System.ArgumentNullException(nameof(dialog));
            logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await hrBotStateService.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
    }

}

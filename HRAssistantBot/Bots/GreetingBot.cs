using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using HRAssistantBot.Models;
using HRAssistantBot.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HRAssistantBot.Bots
{
    public class GreetingBot : ActivityHandler
    {
        #region Variables
            private readonly HRBotStateService hrBotStateService;
        #endregion  

        public GreetingBot(HRBotStateService botStateService)
        {
            hrBotStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await GetName(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await GetName(turnContext, cancellationToken);
                }
            }
        }

        private async Task GetName(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            VacationDetail vacationDetails = await hrBotStateService.UserProfileAccessor.GetAsync(turnContext, () => new VacationDetail());

            if (!string.IsNullOrEmpty(vacationDetails.Name))
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(String.Format("Hi {0}. How can I help you today?", vacationDetails.Name)), cancellationToken);
            }
            else
            {
                // Prompt the user for their name.
                await turnContext.SendActivityAsync(MessageFactory.Text($"What is your name?"), cancellationToken);

                // Save any state changes that might have occured during the turn.
                await hrBotStateService.UserProfileAccessor.SetAsync(turnContext, vacationDetails);

                await hrBotStateService.UserState.SaveChangesAsync(turnContext);
            }
        }

    }
}

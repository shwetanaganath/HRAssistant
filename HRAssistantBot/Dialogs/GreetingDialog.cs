using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using HRAssistantBot.Models;
using HRAssistantBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace HRAssistantBot.Dialogs
{
    public class GreetingDialog : ComponentDialog
    {
        #region Variables
        private readonly HRBotStateService hrBotStateService;
        #endregion  
        public GreetingDialog(string dialogId, HRBotStateService botStateService) : base(dialogId)
        {
            hrBotStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(GreetingDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(GreetingDialog)}.name"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(GreetingDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            VacationDetail vacationDetails = await hrBotStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new VacationDetail());

            if (string.IsNullOrEmpty(vacationDetails.Name))
            {
                return await stepContext.PromptAsync($"{nameof(GreetingDialog)}.name",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("What is your name?")
                    }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            VacationDetail vacationDetails = await hrBotStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new VacationDetail());
            if (string.IsNullOrEmpty(vacationDetails.Name))
            {
                // Set the name
                vacationDetails.Name = (string)stepContext.Result;

                // Save any state changes that might have occured during the turn.
                await hrBotStateService.UserProfileAccessor.SetAsync(stepContext.Context, vacationDetails);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Hi {0}. How can I help you today?", vacationDetails.Name)), cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}

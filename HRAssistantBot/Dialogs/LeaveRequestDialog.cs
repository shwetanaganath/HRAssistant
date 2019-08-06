using HRAssistantBot.Models;
using HRAssistantBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HRAssistantBot.Dialogs
{
    public class PaySlipDialog : ComponentDialog
    {
        #region Variables
        private readonly HRBotStateService hrBotStateService;
        #endregion  


        public PaySlipDialog(string dialogId, HRBotStateService botStateService) : base(dialogId)
        {
            hrBotStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));


            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var leaveApplicationSteps = new WaterfallStep[]
            {
                LeaveStartStepAsync, // DateTimePrompt
                NumberOfDaysStepAsync, // NumberPrompt
                OOOMessageConfirmStepAsync, // ConfirmPrompt
                MessageDescriptionStepAsync, // TextPrompt
                MessageOptionStepAsyc, // ChoicePrompt
                LeaveSummaryStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(PaySlipDialog)}.mainFlow", leaveApplicationSteps));
            AddDialog(new DateTimePrompt($"{nameof(PaySlipDialog)}.leaveStart", LeaveStartValidatorAsync));
            AddDialog(new NumberPrompt<int>($"{nameof(PaySlipDialog)}.numberOfDays", LeaveDaysValidatorAsync));
            AddDialog(new ConfirmPrompt($"{nameof(PaySlipDialog)}.oofMessageConfirm"));
            AddDialog(new TextPrompt($"{nameof(PaySlipDialog)}.messageDescription"));
            AddDialog(new ChoicePrompt($"{nameof(PaySlipDialog)}.messageVisibilityOptions"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(PaySlipDialog)}.mainFlow";
        }

        #region Waterfall Steps
        private async Task<DialogTurnResult> LeaveStartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(PaySlipDialog)}.leaveStart",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("When do you want to start your leave?")
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> NumberOfDaysStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["leaveStart"] = Convert.ToDateTime(((List<DateTimeResolution>)stepContext.Result).FirstOrDefault().Value);

            return await stepContext.PromptAsync($"{nameof(PaySlipDialog)}.numberOfDays",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("How many days do you want to avail?"),
                    RetryPrompt = MessageFactory.Text("The value entered must be between 0.5 and 20."),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> OOOMessageConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["numberOfDays"] = (int)stepContext.Result;

            return await stepContext.PromptAsync($"{nameof(PaySlipDialog)}.oofMessageConfirm",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you want to set up OOO message on outlook?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Yes", "No"}),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> MessageDescriptionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["oofMessageConfirm"] = Convert.ToBoolean(stepContext.Result);

            return await stepContext.PromptAsync($"{nameof(PaySlipDialog)}.messageDescription",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please enter the message here.")
                }, cancellationToken);
        }


        private async Task<DialogTurnResult> MessageOptionStepAsyc(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["messageDescription"] = Convert.ToString(stepContext.Result);
            return await stepContext.PromptAsync($"{nameof(PaySlipDialog)}.messageVisibilityOptions",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you want to set up message on outlook for internal / external / both?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Internal", "External","Both" }),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> LeaveSummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["messageVisibilityOptions"] = ((FoundChoice)stepContext.Result).Value;

            // Get the current profile object from user state.
            var vacationDetails = await hrBotStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new VacationDetail(), cancellationToken);

            // Save all of the data inside the user profile
            vacationDetails.LeaveStartDate = Convert.ToDateTime(stepContext.Values["leaveStart"]);
            vacationDetails.NoOfDays = (int)stepContext.Values["numberOfDays"];
            vacationDetails.SetOutlookMessage = Convert.ToString(stepContext.Values["oofMessageConfirm"]);
            vacationDetails.OutlookMessage = Convert.ToString(stepContext.Values["messageDescription"]);
            vacationDetails.OutlookMessageVisibility = Convert.ToString(stepContext.Values["messageVisibilityOptions"]);

            // Show the summary to the user
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Leave Summary Details:"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Leave Start Date: {0}", vacationDetails.LeaveStartDate)), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("No. Of Days: {0}", vacationDetails.NoOfDays.ToString())), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Outlook Message: {0}", vacationDetails.OutlookMessage)), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Set Outlook Message: {0}", vacationDetails.SetOutlookMessage)), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Message Visibility: {0}", vacationDetails.OutlookMessageVisibility)), cancellationToken);
            

            // Save data in userstate
            await hrBotStateService.UserProfileAccessor.SetAsync(stepContext.Context, vacationDetails);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
        #endregion

        #region Validators

        private Task<bool> LeaveStartValidatorAsync(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var resolution = promptContext.Recognized.Value.First();
                DateTime leaveStartDate = Convert.ToDateTime(resolution.Value);
                DateTime todaysDate = DateTime.Today;
                if (leaveStartDate.TimeOfDay >= todaysDate.TimeOfDay)
                {
                    valid = true;
                }
            }
            return Task.FromResult(valid);
        }

        private Task<bool> LeaveDaysValidatorAsync(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;

            if (promptContext.Recognized.Succeeded)
            {
                var leaveDays = Convert.ToInt16(promptContext.Recognized.Value);
                if(leaveDays >= 1 && leaveDays <= 20)
                    valid = true;
                
            }
            return Task.FromResult(valid);
        }

        #endregion
    }
}

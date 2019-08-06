using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using HRAssistantBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRAssistantBot.Services
{
    public class HRBotStateService
    {
        #region Variables
        // State Variables
        
        public UserState UserState { get; }

        // IDs
        public static string UserProfileId { get; } = $"{nameof(HRBotStateService)}.VacationDetail";
       
        // Accessors
        public IStatePropertyAccessor<VacationDetail> UserProfileAccessor { get; set; }
        #endregion

        public HRBotStateService(ConversationState conversationState, UserState userState)
        {
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
            InitializeAccessors();
        }

        public void InitializeAccessors()
        {
            // Initialize User State
            UserProfileAccessor = UserState.CreateProperty<VacationDetail>(UserProfileId);
        }
    }
}

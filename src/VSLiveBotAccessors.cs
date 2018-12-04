using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VSLiveBot.StateInformation;

namespace VSLiveBot
{
    public class VSLiveBotAccessors
    {
        public VSLiveBotAccessors(ConversationState conversationState, UserState userState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        }

        public IStatePropertyAccessor<DialogState> ConversationDialogState { get; set; }
        public IStatePropertyAccessor<UserProfile> UserProfile { get; set; }
        public IStatePropertyAccessor<CreateNewData> CreateNewData { get; set; }

        public static string UserProfileName { get; } = "UserProfile";

        public static string ConversationDataName { get; } = "ConversationData";

        public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; set; }

        public IStatePropertyAccessor<ConversationData> ConversationDataAccessor { get; set; }

        public ConversationState ConversationState { get; }
        public UserState UserState { get; }
    }
}
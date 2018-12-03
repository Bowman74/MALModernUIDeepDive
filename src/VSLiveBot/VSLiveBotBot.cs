// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using VSLiveBot.Dialogs;
using VSLiveBot.StateInformation;

namespace VSLiveBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. Objects that are expensive to construct, or have a lifetime
    /// beyond a single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class VSLiveBotBot : IBot
    {
        private readonly VSLiveBotAccessors _accessors;
        private DialogSet _dialogs;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>                        
        public VSLiveBotBot(VSLiveBotAccessors accessors)
        {
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

            _dialogs = new DialogSet(accessors.ConversationDialogState);

            var userInforDialog = new UserInfoDialog(_accessors);

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            _dialogs.Add(new WaterfallDialog("UserInfo", userInforDialog.Steps()));
            _dialogs.Add(new TextPrompt("name"));
            _dialogs.Add(new TextPrompt("adoId"));
        }

        /// <summary>
        /// Every conversation turn calls this method.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (turnContext.Activity.Type)
            {
                case ActivityTypes.ConversationUpdate:
                    {
                        IConversationUpdateActivity activity = turnContext.Activity.AsConversationUpdateActivity();

                        break;
                    }
                case ActivityTypes.Message:
                    {
                        // Get the state properties from the turn context.
                        UserProfile userProfile =
                            await _accessors.UserProfileAccessor.GetAsync(turnContext, () => new UserProfile());
                        ConversationData conversationData =
                            await _accessors.ConversationDataAccessor.GetAsync(turnContext, () => new ConversationData());

                        if (string.IsNullOrEmpty(userProfile.Name) || string.IsNullOrEmpty(userProfile.AdoId))
                        {
                            var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                            var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                            // If the DialogTurnStatus is Empty we should start a new dialog.
                            if (results.Status == DialogTurnStatus.Empty)
                            {
                                await dialogContext.BeginDialogAsync("UserInfo", null, cancellationToken);
                            }
                        }
                        else
                        {
                            await turnContext.SendActivityAsync($"{userProfile.Name}, We got what we need");
                        }

                        await _accessors.ConversationDataAccessor.SetAsync(turnContext, conversationData);
                        await _accessors.ConversationState.SaveChangesAsync(turnContext);
                    }
                    break;
            }
        }
    }
}
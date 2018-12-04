// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using VSLiveBot.Dialogs;
using VSLiveBot.StateInformation;

namespace VSLiveBot
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class VSLiveBot : IBot
    {
        private readonly VSLiveBotAccessors _accessors;
        private DialogSet _dialogs;
        public static readonly string LuisKey = "ModernAppsLiveLanguage";
        private readonly BotServices _services;
                   
        public VSLiveBot(VSLiveBotAccessors accessors, BotServices services)
        {
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

            _dialogs = new DialogSet(accessors.ConversationDialogState);

            var userInforDialog = new UserInfoDialog(_accessors);

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            _dialogs.Add(new WaterfallDialog("UserInfo", userInforDialog.Steps()));
            _dialogs.Add(new TextPrompt("name"));
            _dialogs.Add(new TextPrompt("adoId"));

            var createNewDialog = new CreateNewDialog(_accessors);

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            _dialogs.Add(new WaterfallDialog("CreateNew", createNewDialog.Steps()));
            _dialogs.Add(new TextPrompt("description"));
            _dialogs.Add(new ConfirmPrompt("assignSelf"));

            _services = services ?? throw new System.ArgumentNullException(nameof(services));
            if (!_services.LuisServices.ContainsKey(LuisKey))
            {
                throw new System.ArgumentException($"Invalid configuration....");
            }
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
                            var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                            var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                            // If the DialogTurnStatus is Empty we should start a new dialog.
                            if (results.Status == DialogTurnStatus.Empty)
                            {
                                var recognizerResult = await _services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
                                var topIntent = recognizerResult?.GetTopScoringIntent();
                                if (topIntent != null && topIntent.HasValue && topIntent.Value.intent != "None")
                                {
                                    dynamic entity = recognizerResult.Entities.Last.First.First.First.ToString();

                                    if (topIntent.Value.intent == "Create_New")
                                    {
                                        await dialogContext.BeginDialogAsync("CreateNew", entity, cancellationToken);
                                    }
                                }
                                else
                                {
                                    var msg = $"{userProfile.Name}, I am unsure what you want to do.";
                                    await turnContext.SendActivityAsync(msg);
                                }
                            }

                        }

                        await _accessors.ConversationDataAccessor.SetAsync(turnContext, conversationData);
                        await _accessors.ConversationState.SaveChangesAsync(turnContext);
                    }
                    break;
            }
        }

        private string GetAdoItemName(string entityName)
        {
            var returnValue = "";
            switch (entityName.ToUpper())
            {
                case "FEATURE":
                    {
                        returnValue = "Feature";
                        break;
                    }
                case "EPIC":
                    {
                        returnValue = "Epic";
                        break;
                    }
                case "TEST CASE":
                    {
                        returnValue = "Test Case";
                        break;
                    }
                case "TASK":
                    {
                        returnValue = "Task";
                        break;
                    }
                case "USER STORY":
                    {
                        returnValue = "User Story";
                        break;
                    }
                case "BUG":
                    {
                        returnValue = "Bug";
                        break;
                    }
            }
            return returnValue;
        }
    }
}

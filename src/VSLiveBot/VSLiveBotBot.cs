// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
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

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>                        
        public VSLiveBotBot(VSLiveBotAccessors accessors)
        {
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
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
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Get the state properties from the turn context.
                UserProfile userProfile =
                    await _accessors.UserProfileAccessor.GetAsync(turnContext, () => new UserProfile());
                ConversationData conversationData =
                    await _accessors.ConversationDataAccessor.GetAsync(turnContext, () => new ConversationData());

                if (string.IsNullOrEmpty(userProfile.Name))
                {
                    // First time around this is set to false, so we will prompt user for name.
                    if (conversationData.PromptedUserForName)
                    {
                        // Set the name to what the user provided
                        userProfile.Name = turnContext.Activity.Text?.Trim();
                        // Reset the flag to allow the bot to go though the cycle again.
                        conversationData.PromptedUserForName = false;
                        if (string.IsNullOrEmpty(userProfile.AdoId))
                        {
                            // Prompt the user for their name.
                            await turnContext.SendActivityAsync($"Thanks {userProfile.Name}, What is your Azure Dev Ops Id?");

                            // Set the flag to true, so we don't prompt in the next turn.
                            conversationData.PromptedUserForAdoId = true;
                        }
                        else
                        {
                            // Acknowledge that we got their name.
                            await turnContext.SendActivityAsync($"Thanks {userProfile.Name}.");
                        }
                    }
                    else
                    {
                        // Prompt the user for their name.
                        await turnContext.SendActivityAsync($"What is your name?");
                        // Set the flag to true, so we don't prompt in the next turn.
                        conversationData.PromptedUserForName = true;
                    }
                    // Save user state and save changes.
                    await _accessors.UserProfileAccessor.SetAsync(turnContext, userProfile);
                    await _accessors.UserState.SaveChangesAsync(turnContext);
                }
                else if(string.IsNullOrEmpty(userProfile.AdoId) && conversationData.PromptedUserForAdoId)
                {
                    // Set the name to what the user provided
                    userProfile.AdoId = turnContext.Activity.Text?.Trim();

                    // Acknowledge that we got their name.
                    await turnContext.SendActivityAsync($"Thanks {userProfile.Name} for your Azure Dev Ops username of {userProfile.AdoId}.");

                    // Reset the flag to allow the bot to go though the cycle again.
                    conversationData.PromptedUserForAdoId = false;
                }
                else
                {
                    await turnContext.SendActivityAsync($"We got what we need");
                }

                await _accessors.ConversationDataAccessor.SetAsync(turnContext, conversationData);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
            }
        }
    }
}
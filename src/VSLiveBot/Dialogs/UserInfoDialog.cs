using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VSLiveBot.StateInformation;

namespace VSLiveBot.Dialogs
{
    public class UserInfoDialog
    {
        private readonly VSLiveBotAccessors _accessors;

        public UserInfoDialog(VSLiveBotAccessors accessors)
        {
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
        }


        public WaterfallStep[] Steps()
        {
            return new WaterfallStep[]
            {
                GetNameStepAsync,
                NameConfirmStepAsync,
                GetAdoIdStepAsync,
                AdoIdConfirmStepStepAsync
            };
        }
        private static async Task<DialogTurnResult> GetNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            // Running a prompt here means the next WaterfallStep will be run when the users response is received.
            return await stepContext.PromptAsync("name", new PromptOptions { Prompt = MessageFactory.Text("Before we can start we need to know a bit about you, what is your name?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> NameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

            // Update the profile.
            userProfile.Name = (string)stepContext.Result;

            await _accessors.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            await _accessors.UserState.SaveChangesAsync(stepContext.Context);

            // We can send messages to the user at any point in the WaterfallStep.
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thanks {stepContext.Result}."), cancellationToken);

            return await stepContext.ContinueDialogAsync(cancellationToken);
        }

        private async Task<DialogTurnResult> GetAdoIdStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _accessors.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
 
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is a Prompt Dialog.
            return await stepContext.PromptAsync("adoId", new PromptOptions { Prompt = MessageFactory.Text("What is your Azure Dev Ops Id?") }, cancellationToken);
        }


        private async Task<DialogTurnResult> AdoIdConfirmStepStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _accessors.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            // Update the profile.
            userProfile.AdoId = (string)stepContext.Result;

            await _accessors.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            await _accessors.UserState.SaveChangesAsync(stepContext.Context);

            // We can send messages to the user at any point in the WaterfallStep.
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thanks {userProfile.Name}, I have your Azure Dev Ops Id as {userProfile.AdoId}."), cancellationToken);

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"What do you want to do?"), cancellationToken);
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.EndDialogAsync(userProfile, cancellationToken: cancellationToken);
        }
    }
}

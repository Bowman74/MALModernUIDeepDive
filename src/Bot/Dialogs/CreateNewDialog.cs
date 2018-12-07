using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VSLiveBot.StateInformation;

namespace VSLiveBot.Dialogs
{
    public class CreateNewDialog
    {
        private readonly VSLiveBotAccessors _accessors;

        public CreateNewDialog(VSLiveBotAccessors accessors)
        {
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
        }

        public WaterfallStep[] Steps()
        {
            return new WaterfallStep[]
            {
                GetDescriptionAsync,
                DescriptionConfirmStepAsync,
                GetAssignedToStepAsync,
                AssignedToStepStepAsync
            };
        }
        private async Task<DialogTurnResult> GetDescriptionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var createNewData = await _accessors.CreateNewData.GetAsync(stepContext.Context, () => new CreateNewData(), cancellationToken);
            createNewData.ItemType = (string)stepContext.Options;
            await _accessors.CreateNewData.SetAsync(stepContext.Context, createNewData);
            await _accessors.UserState.SaveChangesAsync(stepContext.Context);

            return await stepContext.PromptAsync("description", new PromptOptions { Prompt = MessageFactory.Text($"What should be the description for this {stepContext.Options}?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> DescriptionConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
            var createNewData = await _accessors.CreateNewData.GetAsync(stepContext.Context, () => new CreateNewData(), cancellationToken);
            createNewData.Description = (string)stepContext.Result;
            await _accessors.CreateNewData.SetAsync(stepContext.Context, createNewData);
            await _accessors.UserState.SaveChangesAsync(stepContext.Context);

            // We can send messages to the user at any point in the WaterfallStep.
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Thanks {userProfile.Name}, the description will be '{createNewData.Description}'."), cancellationToken);

            return await stepContext.ContinueDialogAsync(cancellationToken);
        }

        private async Task<DialogTurnResult> GetAssignedToStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _accessors.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            var choices = new List<Choice>();
            choices.Add(new Choice { Value = "Yes" });
            choices.Add(new Choice { Value = "No" });
            return await stepContext.PromptAsync("assignSelf", new PromptOptions { Prompt = MessageFactory.Text("Do you want to assign this to yourself?"), Choices = choices }, cancellationToken);
        }


        private async Task<DialogTurnResult> AssignedToStepStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = await _accessors.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            var createNewData = await _accessors.CreateNewData.GetAsync(stepContext.Context, () => new CreateNewData(), cancellationToken);
            createNewData.AssignToSelf = (bool)stepContext.Result;
            await _accessors.CreateNewData.SetAsync(stepContext.Context, createNewData);
            await _accessors.UserState.SaveChangesAsync(stepContext.Context);

            // We can send messages to the user at any point in the WaterfallStep.
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Creating a new {createNewData.ItemType} with a description of {createNewData.Description} and assigned to self as {createNewData.AssignToSelf}"), cancellationToken);

            // Add code here to call Azure Dev Ops REST API to create item.

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"What do you want to do?"), cancellationToken);
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.EndDialogAsync(userProfile, cancellationToken: cancellationToken);
        }
    }
}

using System;
using System.Threading.Tasks;

namespace Sleepet
{
    [Serializable] public struct CompanionContext
    {
        public string petName;
        public string currentMode;
        public CompanionMode companionMode;
        public SleepState sleepState;
        public string skillId, today, capturedAt;
        public string thoughtMotion, thoughtTrigger;
        public bool shareData;
        public CompanionPreferences preferences;
        public CompanionRecord[] records;
        public CompanionPlan plan;
        public CompanionTurn[] conversation;
    }

    public interface ICompanionAI
    {
        Task<string> SendMessage(string userText, CompanionContext context);
    }

    public interface ICancellableCompanionAI { void CancelPending(); }

    public sealed class MockCompanionAI : ICompanionAI
    {
        public const string OfflineMessage = "you need access the Internet to generate the answers.";

        public Task<string> SendMessage(string userText, CompanionContext context)
        {
            return Task.FromResult(OfflineMessage);
        }
    }
}

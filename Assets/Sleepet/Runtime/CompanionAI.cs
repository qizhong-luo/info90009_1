using System;
using System.Threading.Tasks;

namespace Sleepet
{
    public struct CompanionContext
    {
        public string petName;
        public string currentMode;
        public CompanionMode companionMode;
        public SleepState sleepState;
    }

    public interface ICompanionAI
    {
        Task<string> SendMessage(string userText, CompanionContext context);
    }

    public sealed class MockCompanionAI : ICompanionAI
    {
        public Task<string> SendMessage(string userText, CompanionContext context)
        {
            string input = (userText ?? "").ToLowerInvariant();
            string reply;
            if (input.Contains("good night") || input.Contains("晚安"))
                reply = "Good night. I'll stay quietly beside you.";
            else if (input.Contains("bad day") || input.Contains("糟") || input.Contains("难过"))
                reply = "That sounds like a hard day. We can talk a little, or just stay quiet together.";
            else if (input.Contains("tired") || input.Contains("累"))
                reply = "You don't have to do anything for me. We can rest here together.";
            else if (input.Contains("stay") || input.Contains("陪"))
                reply = "I'm here with you. We can take a slow breath, or simply stay quiet.";
            else if (input.Contains("sleep") || input.Contains("awake") || input.Contains("睡"))
                reply = "It's okay to still be awake. We can talk, breathe, or just stay quiet.";
            else
                reply = "I'm here. Would you like to talk a little, breathe, or stay quiet together?";
            return Task.FromResult(reply);
        }
    }
}

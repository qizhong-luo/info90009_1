using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Sleepet
{
    // Explicit opt-in standalone validation; never runs during ordinary launches.
    public static class LocalAISmokeRunner
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static async void Run()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--local-ai-smoke");
            if (index < 0 || index + 1 >= args.Length) return;
            string directory = Path.GetFullPath(args[index + 1]);
            Directory.CreateDirectory(directory);
            using (var ai = new LocalCompanionAI())
            {
                try
                {
                    var context = new CompanionContext { skillId = CompanionSkills.Daily };
                    string reply = await ai.SendMessage("Say hello in one short sentence.", context);
                    if (string.IsNullOrWhiteSpace(reply) || reply == MockCompanionAI.OfflineMessage) throw new Exception("No generated answer.");
                    File.WriteAllText(Path.Combine(directory, "smoke.txt"), "PASS\nProcess: " + ai.ProcessId + "\nSeconds: " + ai.LastReplySeconds + "\nReply: " + reply);
                    Application.Quit(0);
                }
                catch (Exception e) { File.WriteAllText(Path.Combine(directory, "smoke.txt"), "FAIL\n" + e); Application.Quit(1); }
            }
        }
    }
}

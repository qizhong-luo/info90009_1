#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sleepet.Editor
{
    public static class SleepetWindowsBuild
    {
        [MenuItem("Sleepet/Build Windows With Local AI")]
        public static void Build()
        {
            string root = Path.GetFullPath("LocalAI");
            if (!File.Exists(Path.Combine(root, "runtime", "llama-server.exe")) ||
                !File.Exists(Path.Combine(root, "models", LocalCompanionAI.ModelFile)))
                throw new InvalidOperationException("Run Tools/PrepareLocalAI.ps1 before building.");
            foreach (var name in new[] { "msvcp140.dll", "vcruntime140.dll", "vcruntime140_1.dll" })
                if (!File.Exists(Path.Combine(root, "runtime", name))) throw new InvalidOperationException("Missing bundled CRT: " + name);
            string output = Path.GetFullPath("Builds/Sleepet-LocalAI-Windows");
            var args = Environment.GetCommandLineArgs();
            int outputIndex = Array.IndexOf(args, "-sleepet-build-output");
            if (outputIndex >= 0)
            {
                if (outputIndex + 1 >= args.Length) throw new ArgumentException("Missing build output directory.");
                output = Path.GetFullPath(args[outputIndex + 1]);
                string buildsRoot = Path.GetFullPath("Builds") + Path.DirectorySeparatorChar;
                if (!output.StartsWith(buildsRoot, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Build output must be inside this project's Builds directory.");
            }
            Directory.CreateDirectory(output);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = Path.Combine(output, "Sleepet.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed: " + result.summary.result);
            foreach (var folder in new[] { "runtime", "models", "licenses" }) Copy(Path.Combine(root, folder), Path.Combine(output, "LocalAI", folder));
            File.Copy(Path.Combine(root, "manifest.json"), Path.Combine(output, "LocalAI", "manifest.json"), true);
            File.WriteAllText(Path.Combine(output, "START_HERE.txt"),
                "SLEEPET WITH LOCAL AI\r\n\r\nDouble-click Sleepet.exe. Keep this entire folder together.\r\n" +
                "Local AI loads automatically. No Node, terminal, Internet or API key is needed.\r\n" +
                "Enter AR and choose Chat with the Sleepet to start a conversation.\r\n" +
                "Meet the Sleepet in AR is a separate placement feature. Skills are selected automatically. Saved information is enabled by default. The first answer may take longer while the model loads.\r\n" +
                "Model: Qwen3 1.7B Q4_K_M. Runtime: llama.cpp CPU, Windows x64.\r\n" +
                "Licenses and source attribution are in LocalAI/licenses and LocalAI/manifest.json.\r\n");
            Debug.Log("SLEEPET_WINDOWS_BUILD_SUCCESS " + output);
        }
        static void Copy(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            foreach (string dir in Directory.GetDirectories(source)) Copy(dir, Path.Combine(target, Path.GetFileName(dir)));
        }
    }
}
#endif

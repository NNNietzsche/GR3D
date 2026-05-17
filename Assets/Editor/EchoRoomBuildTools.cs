using System.IO;
using UnityEditor;
using UnityEngine;

public static class EchoRoomBuildTools
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string BuildDirectory = "Builds/EchoRoom-Windows";
    private const string ExeName = "EchoRoom.exe";

    [MenuItem("Tools/Echo Room/Build Windows Package", false, 20)]
    public static void BuildWindowsPackage()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Echo Room", "请先运行：Tools > Echo Room > Build Starter Scene", "OK");
            return;
        }

        Directory.CreateDirectory(BuildDirectory);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(BuildDirectory, ExeName),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);
        CopyProxyFiles();

        EditorUtility.RevealInFinder(BuildDirectory);
        EditorUtility.DisplayDialog("Echo Room", "Windows 包已生成：\n" + BuildDirectory, "OK");
    }

    private static void CopyProxyFiles()
    {
        CopyFileIfExists("start-gemini-proxy.cmd", Path.Combine(BuildDirectory, "start-gemini-proxy.cmd"));
        CopyFileIfExists("LLM_SETUP.txt", Path.Combine(BuildDirectory, "LLM_SETUP.txt"));

        string sourceServer = "server";
        string targetServer = Path.Combine(BuildDirectory, "server");
        if (!Directory.Exists(sourceServer)) return;

        Directory.CreateDirectory(targetServer);
        foreach (string sourceFile in Directory.GetFiles(sourceServer, "*", SearchOption.AllDirectories))
        {
            string relative = sourceFile.Substring(sourceServer.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string targetFile = Path.Combine(targetServer, relative);
            string targetFolder = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetFolder)) Directory.CreateDirectory(targetFolder);
            File.Copy(sourceFile, targetFile, true);
        }
    }

    private static void CopyFileIfExists(string source, string target)
    {
        if (!File.Exists(source)) return;
        string folder = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.Copy(source, target, true);
    }
}

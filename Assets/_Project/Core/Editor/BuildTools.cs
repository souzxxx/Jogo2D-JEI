using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AFirma.EditorTools
{
    /// <summary>
    /// Build WebGL pro itch.io. Menu: A Firma > Build > WebGL.
    /// Linha de comando: Unity -batchmode -quit -projectPath . -executeMethod AFirma.EditorTools.BuildTools.BuildWebGL [-buildPath Builds/WebGL] [-development]
    /// Depois é só zipar o conteúdo da pasta (o index.html precisa ficar na raiz do zip) e subir no itch.
    /// </summary>
    public static class BuildTools
    {
        const string DefaultPath = "Builds/WebGL";

        [MenuItem("A Firma/Build/WebGL")]
        public static void BuildWebGLMenu() => Build(DefaultPath, false);

        [MenuItem("A Firma/Build/WebGL (Development, com painel F1)")]
        public static void BuildWebGLDevMenu() => Build(DefaultPath + "-dev", true);

        public static void BuildWebGL()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-buildPath");
            var path = index >= 0 && index + 1 < args.Length ? args[index + 1] : DefaultPath;
            var development = args.Contains("-development");
            var report = Build(path, development);
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static BuildReport Build(string path, bool development)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[A Firma] Build WebGL: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime.TotalSeconds:0}s -> {path}");
            return report;
        }
    }
}

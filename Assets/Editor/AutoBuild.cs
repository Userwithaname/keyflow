using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class AutoBuild {
/*
	This can be used in the terminal as such:

		/opt/Unity/Editor/Unity -quit -batchmode -projectPath '/path/to/project' -executeMethod AutoBuild.Build [arguments]

		Valid arguments:
			--linux             - Build for Linux
			--macos             - Build for Mac OS
			--windows           - Build for Windows
			--show              - Opens build directory uppon completion
			--profiler          - Connects the Unity's profiler
			--debug             - Allow live debugging
			--fast              - Only build scripts
			--clean             - Remove old files before building
			--final             - Disables build options: development build, debugging, --profiler, --fast
			--build-path [path] - Override the default build path ('[current]/Builds')
*/

private static BuildPlayerOptions	buildOptions;
private static BuildTargetGroup		targetPlatformGroup;
private static BuildTarget			targetPlatform;
private static string buildPath;
	
	public static void Build() {
		PrepareForBuild();
		var target = EditorUserBuildSettings.activeBuildTarget;
		var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
		bool built = false;
		if (HasArgument("--linux") || HasArgument("-l")) {
			BuildLinux();
			built = true;
		}
		if (HasArgument("--macos") || HasArgument("-m")) {
			BuildMacOS();
			built = true;
		}
		if (HasArgument("--windows") || HasArgument("-w")) {
			BuildWindows();
			built = true;
		}
		if (HasArgument("--webgl") || HasArgument("-gl")) {
			EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.WebGL;
			// target = EditorUserBuildSettings.activeBuildTarget;
			BuildWebGL();
			built = true;
		}
		if (!built)	// If no platform was specified, build for Linux
			BuildLinux();
		
		if (target != EditorUserBuildSettings.activeBuildTarget) {
			Debug.Log("Reverting editor build settings");
			EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);
		}
	}

	private static void PrepareForBuild() {
		buildPath = GetArgumentValue("-projectPath") + "/Builds/";
		Debug.Log("Building to: '" + buildPath + "'");

		Debug.Log("Determining scenes for build...");
		EditorBuildSettingsScene[] scenesTemp=EditorBuildSettings.scenes;
		var scenes = new List<string>();
		for (int i = 0; i < scenesTemp.Length; i++) {
			if (scenesTemp[i].enabled) {
				scenes.Add(scenesTemp[i].path);
			}
		}

		buildOptions = new BuildPlayerOptions();
		buildOptions.scenes = scenes.ToArray();

		var projectPath = System.IO.Directory.GetParent(Application.dataPath);

		if (HasArgument("--clean") || HasArgument("--full-clean") || HasArgument("--reimport")) {
			Debug.Log("Removing directory: 'Library/PlayerDataCache'");
			try {
				System.IO.Directory.Delete(projectPath+"/Library/PlayerDataCache",true);
			} catch {}
		}
		if (HasArgument("--full-clean") || HasArgument("--reimport")) {
			Debug.Log("Removing directory: 'Library/il2cpp_cache'");
			try {
				System.IO.Directory.Delete(projectPath + "/Library/il2cpp_cache",true);
			} catch {}

			Debug.Log("Removing directory: 'Library/PlayerScriptAssemblies'");
			try {
				System.IO.Directory.Delete(projectPath + "/Library/PlayerScriptAssemblies",true);
			} catch {}

			Debug.Log("Removing directory: 'Library/PlayerAssemblies'");
			try {
				System.IO.Directory.Delete(projectPath + "/Library/PlayerAssemblies",true);
			} catch {}

			Debug.Log("Removing directory: 'Library/ShaderCache'");
			try {
				System.IO.Directory.Delete(projectPath + "/Library/ShaderCache",true);
			} catch {}

			Debug.Log("Removing directory: 'Library/SplashScreenCache'");
			try {
				System.IO.Directory.Delete(projectPath + "/Library/SplashScreenCache",true);
			} catch {}

			Debug.Log("Removing file: 'Library/ShaderCache.db'");
			System.IO.File.Delete(projectPath + "/Library/ShaderCache.db");
		}
		if (HasArgument("--reimport")) {
			Debug.Log("Removing directory: 'Library/Artifacts'");
			try {
				System.IO.Directory.Delete(projectPath + "/Library/Artifacts",true);
			} catch {}

			Debug.Log("Removing file: 'Library/ArtifactDB'");
			System.IO.File.Delete(projectPath + "/Library/ArtifactDB");

			Debug.Log("Removing file: 'Library/ArtifactDB-lock'");
			System.IO.File.Delete(projectPath + "/Library/ArtifactDB-lock");
		}

		if (HasArgument("--show") || HasArgument("-s")) {
			buildOptions.options = BuildOptions.ShowBuiltPlayer;
		}

		if (HasArgument("--fast") || HasArgument("-f") || HasArgument("--quick")) {
			buildOptions.options |= BuildOptions.BuildScriptsOnly;
		}
		
		// if (HasArgument("--final") || HasArgument("-F")) {
		// 	return;
		// }
		//
		// buildOptions.options |= BuildOptions.Development;
		//
		//
		// if (HasArgument("--debug")) {
		// 	buildOptions.options |= BuildOptions.AllowDebugging;
		// }
		// if (HasArgument("--profiler") || HasArgument("-p")) {
		// 	buildOptions.options |= BuildOptions.ConnectWithProfiler;
		// }
	}

	private static void BuildLinux() {
		Debug.Log("Switching build target... (Linux 64-bit)");
		string buildDir = $"{buildPath}{PlayerSettings.productName}-Linux-x64/{PlayerSettings.productName}/{PlayerSettings.productName}";
		if (HasArgument("--clean")) {
			Debug.Log($"Removing existing build: '{buildDir}'");
			try { System.IO.File.Delete($"{buildDir}.x86_64"); } catch {}
			try { System.IO.Directory.Delete($"{buildDir}_Data",true); } catch {}
			while (System.IO.Directory.Exists(buildDir)) {
				System.Threading.Thread.Sleep(500);
				Debug.Log("Directory not deleted yet, retrying in 0.5 seconds...");
			}
		}
		targetPlatform = BuildTarget.StandaloneLinux64;
		targetPlatformGroup = BuildTargetGroup.Standalone;
		EditorUserBuildSettings.SwitchActiveBuildTarget(targetPlatformGroup, targetPlatform);

		buildOptions.locationPathName = $"{buildDir}.x86_64";
		buildOptions.targetGroup = targetPlatformGroup;
		buildOptions.target = targetPlatform;

		Debug.Log("Creating a Linux build...");
		BuildPipeline.BuildPlayer(
			buildOptions.scenes,
			buildOptions.locationPathName,
			targetPlatform,
			buildOptions.options
		);
		Debug.Log("Done!");
	}

	private static void BuildMacOS() {
		Debug.Log("Switching build target... (Mac OS 64-bit+ARM)");
		string buildDir = $"{buildPath}{PlayerSettings.productName}-Mac-ARM+x64/{PlayerSettings.productName}.app";
		if (HasArgument("--clean")) {
			System.IO.Directory.Delete(buildDir,true);
		}
		targetPlatform = BuildTarget.StandaloneOSX;
		targetPlatformGroup = BuildTargetGroup.Standalone;
		EditorUserBuildSettings.SwitchActiveBuildTarget(targetPlatformGroup, targetPlatform);

		buildOptions.locationPathName = buildDir;
		buildOptions.targetGroup = targetPlatformGroup;
		buildOptions.target = targetPlatform;

		Debug.Log("Creating a Mac OS build...");
		BuildPipeline.BuildPlayer(
			buildOptions.scenes,
			buildOptions.locationPathName,
			targetPlatform,
			buildOptions.options
		);
		Debug.Log("Done!");
	}

	private static void BuildWindows() {
		Debug.Log("Switching build target... (Windows 64-bit)");
		string buildDir = $"{buildPath}{PlayerSettings.productName}-Windows-x64/{PlayerSettings.productName}/{PlayerSettings.productName}";
		if (HasArgument("--clean")) {
			System.IO.File.Delete($"{buildDir}.exe");
			System.IO.Directory.Delete($"{buildDir}_Data",true);
		}
		targetPlatform = BuildTarget.StandaloneWindows64;
		targetPlatformGroup = BuildTargetGroup.Standalone;
		EditorUserBuildSettings.SwitchActiveBuildTarget(targetPlatformGroup, targetPlatform);

		buildOptions.locationPathName = $"{buildDir}.exe";
		buildOptions.targetGroup = targetPlatformGroup;
		buildOptions.target = targetPlatform;

		Debug.Log("Creating a Windows build...");
		BuildPipeline.BuildPlayer(
			buildOptions.scenes,
			buildOptions.locationPathName,
			targetPlatform,
			buildOptions.options
		);
		Debug.Log("Done!");
	}
	
	private static void BuildWebGL() {
		Debug.Log("Switching build target... (WebGL)");
		targetPlatform = BuildTarget.WebGL;
		targetPlatformGroup = BuildTargetGroup.WebGL;
		EditorUserBuildSettings.SwitchActiveBuildTarget(targetPlatformGroup,targetPlatform);

		string buildDir = $"{buildPath}{PlayerSettings.productName}-WebGL/";
		buildOptions.locationPathName = buildDir;
		buildOptions.targetGroup = targetPlatformGroup;
		buildOptions.target = targetPlatform;

		Debug.Log("Creating a WebGL build...");
		BuildPipeline.BuildPlayer(
			buildOptions.scenes,
			buildOptions.locationPathName,
			BuildTarget.WebGL,
			buildOptions.options
		);
		Debug.Log("Done!");
	}

	private static bool HasArgument(string name) {
		string[] args = System.Environment.GetCommandLineArgs();
		foreach (string arg in args) {
			if (arg == name)
				return true;
		}
		return false;
	}

	private static string GetArgumentValue(string name) {
		string[] args = System.Environment.GetCommandLineArgs();
		for (int i = args.Length - 1; i > -1; i--) {
			if (args[i] == name) {
				return args[i + 1];
			}
		}
		return null;
	}
}

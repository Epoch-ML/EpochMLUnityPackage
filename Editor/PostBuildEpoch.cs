using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.PackageManager;

public class XcodeProjectModifier {
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {

        if (target == BuildTarget.iOS) {
            string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));
			
#if UNITY_2019_3_OR_NEWER
			string unityTargetGuid = proj.GetUnityMainTargetGuid();
			string unityFrameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();
#else
			string unityTargetGuid = proj.TargetGuidByName("Unity-iPhone");
			string unityFrameworkTargetGuid = proj.TargetGuidByName("Unity-iPhone");
#endif


			string epochLibGuid = proj.AddFile("libepoch_cli_lib.a", "Libraries/libepoch_cli_lib.a", PBXSourceTree.Sdk);
			proj.AddFileToBuild(unityFrameworkTargetGuid, epochLibGuid); 

			// Frameworks path by way of EpochButton class 
			var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(XcodeProjectModifier).Assembly);
			string packageRuntimePath = packageInfo.assetPath;
            string frameworksRelativePath = "./Plugins/Apple/ios"; 
            string frameworksPath = Path.Combine(packageRuntimePath, frameworksRelativePath);

			// Frameworks 
			var frameworksDir = Path.Combine(pathToBuiltProject, "Frameworks");
			if (!Directory.Exists(frameworksDir))
    			Directory.CreateDirectory(frameworksDir);

			var librariesDir = Path.Combine(pathToBuiltProject, "Libraries");
			if (!Directory.Exists(librariesDir))
    			Directory.CreateDirectory(librariesDir);

			var xcframeworks = new string[] {
    			"libavcodec.xcframework",
    			"libavdevice.xcframework",
    			"libavfilter.xcframework",
    			"libavformat.xcframework",
    			"libavutil.xcframework",
    			"libswresample.xcframework",
    			"libswscale.xcframework"
			};
			
		//string fileGuid = project.AddFile(destPath, destPath);
        //project.AddFileToEmbedFrameworks(targetGuid, fileGuid);

			//string frameworksBuildPhaseGUID = proj.GetFrameworksBuildPhaseByTarget(unityTargetGuid);

			foreach (var xcframework in xcframeworks) {
    			var srcPath = Path.Combine(frameworksPath, xcframework); // Update with the actual path to your .xcframeworks
    			//var destPath = Path.Combine(frameworksDir, xcframework);

				var destPath = Path.Combine(librariesDir, xcframework);
    			FileUtil.CopyFileOrDirectory(srcPath, destPath);

				//var libFrameworkPath = 
				//proj.AddFileToBuild(targetGuid, epochLibGuid);
				
				string xcframeworkGuid = proj.AddFile(destPath, destPath, PBXSourceTree.Sdk);
				UnityEditor.iOS.Xcode.Extensions.PBXProjectExtensions.AddFileToEmbedFrameworks(proj, unityTargetGuid, xcframeworkGuid); 
				
				// add to project
				//proj.AddFrameworkToProject(targetGuid, xcframework, false);
			}

            File.WriteAllText(projPath, proj.WriteToString());
        }
    }
}
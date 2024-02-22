using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class XcodeProjectModifier {
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {

        if (target == BuildTarget.iOS) {
            string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));

#if UNITY_2019_3_OR_NEWER
            string targetGuid = proj.GetUnityFrameworkTargetGuid();
#else
            string targetGuid = proj.TargetGuidByName("Unity-iPhone");
#endif

			string epochLibGuid = proj.AddFile("libepoch_cli_lib.a", "Libraries/libepoch_cli_lib.a", PBXSourceTree.Sdk);
			proj.AddFileToBuild(targetGuid, epochLibGuid); 
			
			proj.AddFrameworkToProject(targetGuid, "libavcodec.xcframework", false);
			proj.AddFrameworkToProject(targetGuid, "libavdevice.xcframework", false);
			proj.AddFrameworkToProject(targetGuid, "libavfilter.xcframework", false);
			proj.AddFrameworkToProject(targetGuid, "libavformat.xcframework", false);
			proj.AddFrameworkToProject(targetGuid, "libavutil.xcframework", false);
			proj.AddFrameworkToProject(targetGuid, "libswresample.xcframework", false);
			proj.AddFrameworkToProject(targetGuid, "libswscale.xcframework", false);

            File.WriteAllText(projPath, proj.WriteToString());
        }
    }
}
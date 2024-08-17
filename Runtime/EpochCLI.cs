using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Android;

public class EpochCLI {
#if UNITY_IOS && !UNITY_EDITOR
    private const string DllName = "__Internal";
#elif UNITY_ANDROID && !UNITY_EDITOR
    private const string DllName = "epoch_cli_lib"; 
#else
    private const string DllName = "epoch_cli_lib"; 
#endif
    
    #if UNITY_ANDROID && !UNITY_EDITOR
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr epoch_cli_init_android_context_internal(IntPtr androidContext);
    #endif

    private static void epoch_cli_init_android_context() {
        #if !UNITY_ANDROID || UNITY_EDITOR
            Debug.LogError("epoch_cli_init_android_context called on non android platform");
            return;
        #endif

        try {
            using (var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                if (unityPlayerClass == null) {
                    Debug.LogError("Failed to load UnityPlayer class");
                    return;
                }
                Debug.Log("loaded UnityPlayer class");
                
                AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                if (currentActivity == null) {
                    Debug.LogError("currentActivity is null");
                    return;
                }
                Debug.Log("loaded currentActivity");

                // Use the context as needed
                #if UNITY_ANDROID && !UNITY_EDITOR
                        Debug.Log("epoch_cli_init_android_context_internal");
                        epoch_cli_init_android_context_internal(currentActivity.GetRawObject());
                #endif
            }
        } 
        catch (Exception ex) {
            Debug.LogError($"Failed to initialize Android context: {ex.Message}");
        }
    }

    public static IntPtr epoch_cli_create(string buildPath, string logPath, bool verbose) {
        
        #if UNITY_ANDROID && !UNITY_EDITOR
            epoch_cli_init_android_context();
        #endif

        return epoch_cli_new(buildPath, logPath, verbose);
    }
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr epoch_cli_new(string buildPath, string logPath, bool verbose);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_destroy(IntPtr cli);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_initialize_with_port(IntPtr cli, string hostname, int port, int secure);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_initialize(IntPtr cli, string hostname, int secure);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_login(IntPtr cli, string email, string password);

    [StructLayout(LayoutKind.Sequential)]
    public struct NewSession {
        public uint Id;
        public IntPtr Uri;
    }
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern NewSession epoch_cli_login_auth_and_start_new_session(
        IntPtr cli,
        string email,
        string password,
        string project_uri
    );

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_upload_video_to_session_uri(
        IntPtr cli,
        string session_uri,
        string video_filename
    );
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern string epoch_cli_start_new_session(
        IntPtr cli,
        string project_uri
    );

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_complete_session(
        IntPtr cli,
        string session_uri
    );
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_update_session_is_uploading(
        IntPtr cli,
        uint session_id,
        bool is_uploading
    );
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_initialize_ffmpeg_encoder_consumer(
        IntPtr cli,
        string video_filename, 
        string target_encoder_codec, 
        uint framerate,
        string source_pixel_format,
        uint source_width, uint source_height,
        string target_pixel_format,
        uint target_width, uint target_height
    );
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_initialize_dashcam(
        IntPtr cli,
        string dashcam_video_filename,
        uint dashcam_seconds,
        string target_encoder_codec, 
        uint framerate,
        string source_pixel_format,
        uint source_width, uint source_height,
        string target_pixel_format,
        uint target_width, uint target_height
    );
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_save_dashcam(IntPtr cli);
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_finalize_ffmpeg_encoder_consumer(IntPtr cli);
    
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void epoch_cli_encode_frame(
        IntPtr cli,
        ulong time_us,
        uint frame_idx,
        uint source_width, uint source_height,
        IntPtr frame_buffer_ptr, uint frame_buffer_len
    );
    
}
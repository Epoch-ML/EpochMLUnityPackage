using System;
using System.Runtime.InteropServices;

public class EpochCLI {
    private const string DllName = "__Internal";
    //private const string DllName = "epoch_cli_lib";
    //private const string DllName = "@rpath/epoch_cli_lib.xcframework/epoch_cli_lib";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr epoch_cli_new(byte[] buildPath, ulong buildPathLen, bool verbose);

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
        string video_filename, // C# automatically marshals string to *const c_char
        uint framerate,
        uint source_width, uint source_height,
        uint target_width, uint target_height
    );
    
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
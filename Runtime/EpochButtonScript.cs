using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class EpochButtonScript : MonoBehaviour {

    public Sprite imageInactive;
    public Sprite imageActive;
    
    
    private Button button;
    private Image image;

    public string epochCLIBuildPath = "epoch_cli_build_path";
    public string epochCLIHostname = "dev.epochml.com";
    public string epochCLIEmail = "idan@epochml.com";
    public string epochCLIPassword = "Fasfusa19!";
    public string epochCLIProjectURI = "7c95ad89";
    public string videoFilename = "temp_video.mp4";

    public string epochSessionURI = "";
    public uint epochSessionID = 0;
    
    public uint framerate = 20;
    public uint sourceWidth = 0;
    public uint sourceHeight = 0;
    public uint targetWidth = 0;
    public uint targetHeight = 0;
    
    public Stopwatch stopwatch;

    [FormerlySerializedAs("frameIndex")] public uint frameIdx = 0;
    
    private bool isPressed = false;
    private bool isCLIReady = false;

    private IntPtr cliInstance;
    
    // Start is called before the first frame update
    void Start() {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        
        
        button.onClick.AddListener(OnButtonPressed);
        
        // Start screen capture routine
        StartCoroutine(ScreenCaptureRoutine());
    }

    private void StartNewSession() {
        
    }

    private void OnButtonPressed() {
        if (isPressed == false) {
            image.sprite = imageActive;
            isPressed = true;
            
            // initialize EpochCLI DLL
            cliInstance = EpochCLI.epoch_cli_new(
                System.Text.Encoding.UTF8.GetBytes(epochCLIBuildPath),
                (ulong)(epochCLIBuildPath.Length),
                true
            );
        
            // Initialize 
            Debug.Log("initializaing Epoch CLI");
            EpochCLI.epoch_cli_initialize(
                cliInstance, 
                epochCLIHostname, 
                1
            );
            
            // Login 
            Debug.Log("Logging in with Epoch CLI");
            EpochCLI.epoch_cli_login(
                cliInstance, 
                epochCLIEmail, 
                epochCLIPassword
            );

            sourceWidth = (uint)Screen.width;
            sourceHeight = (uint)Screen.height;
            targetWidth = sourceWidth;
            targetHeight = sourceHeight;
            
            // initialize the encoder 
            videoFilename = Path.Combine(Application.persistentDataPath, videoFilename);
            Debug.Log($"Initialize Epoch CLI encoder with {sourceWidth}x{sourceHeight} -> {targetWidth}x{targetHeight}, saving to {videoFilename}");
            EpochCLI.epoch_cli_initialize_ffmpeg_encoder_consumer(
                cliInstance,
                videoFilename,
                framerate,
                sourceWidth, sourceHeight,
                targetWidth, targetHeight
            );
            
            Debug.Log($"login, auth, and start new session");
            var newSession = EpochCLI.epoch_cli_login_auth_and_start_new_session(
                cliInstance,
                epochCLIEmail,
                epochCLIPassword,
                epochCLIProjectURI
            );
            
            epochSessionURI = Marshal.PtrToStringAnsi(newSession.Uri);
            epochSessionID = newSession.Id;

            isCLIReady = true;
            stopwatch = Stopwatch.StartNew();

        }
        else {
            image.sprite = imageInactive;
            isPressed = false;
            isCLIReady = false;
            
            // Finalize 
            Debug.Log("Finalize encoder in Epoch CLI");
            EpochCLI.epoch_cli_finalize_ffmpeg_encoder_consumer(cliInstance);
            
            Debug.Log($"Complete session {epochSessionURI} to project {epochCLIProjectURI}");
            EpochCLI.epoch_cli_complete_session(cliInstance, epochSessionURI);
            
            // Debug.Log($"Complete session {epochSessionURI} to project {epochCLIProjectURI}");
            // EpochCLI.epoch_cli_update_session_is_uploading(cliInstance, epochSessionID, true);
            
            Debug.Log($"Upload video {videoFilename} to epoch session {epochSessionURI}");
            EpochCLI.epoch_cli_upload_video_to_session_uri(
                cliInstance, 
                epochSessionURI, 
                videoFilename
            );
            
            Debug.Log($"Complete session {epochSessionURI} to project {epochCLIProjectURI}");
            EpochCLI.epoch_cli_update_session_is_uploading(cliInstance, epochSessionID, false);
            
            // Destory and teardown
            Debug.Log("Destroying Epoch CLI");
            EpochCLI.epoch_cli_destroy(cliInstance);

            cliInstance = IntPtr.Zero;
        }
    }
    
    // Update is called once per frame
    void Update() {
        //
    }
    
    IEnumerator ScreenCaptureRoutine() {
        while (true) {
            yield return new WaitForEndOfFrame(); // Wait until all frame rendering is done
            if (isPressed & isCLIReady) {
                CaptureScreen();
            }
        }
    }

    private void CaptureScreen() {
        Debug.Log($"capturing screen {frameIdx}");
        Texture2D screenCapture = ScreenCapture.CaptureScreenshotAsTexture();
        long elapsedMicroseconds = (stopwatch.ElapsedTicks * 1_000_000) / Stopwatch.Frequency;

        int width = screenCapture.width;
        int height = screenCapture.height;
        Color32[] pixels = screenCapture.GetPixels32();
        
        byte[] byteArray = new byte[pixels.Length * 4]; // 4 bytes per color
        
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                // Calculate the index for the current pixel in the source image
                int i = x + (height - 1 - y) * width; // Flipping the image by adjusting the Y index
                int j = (x + y * width) * 4; // Index in the destination byte array
    
                // Copy the pixel data into the byte array in BGRA format
                byteArray[j] = pixels[i].b;
                byteArray[j + 1] = pixels[i].g;
                byteArray[j + 2] = pixels[i].r;
                byteArray[j + 3] = pixels[i].a;
            }
        }
        
        UnityEngine.Object.Destroy(screenCapture);

        uint expectedBytes = sourceHeight * sourceWidth * 4;
        Debug.Log($"got {pixels.Length} pixels buffer len {byteArray.Length} for {sourceWidth}x{sourceHeight} expected {expectedBytes}");
        
        GCHandle pinnedArray = GCHandle.Alloc(byteArray, GCHandleType.Pinned);
        IntPtr bufferPtr = pinnedArray.AddrOfPinnedObject();
        
        Debug.Log($"encoding frame with cli {frameIdx}");
        EpochCLI.epoch_cli_encode_frame(
            cliInstance,
            (ulong)elapsedMicroseconds,
            frameIdx,
            sourceWidth,
            sourceHeight,
            bufferPtr,
            (uint)byteArray.Length
        );
        
        pinnedArray.Free();

        frameIdx += 1;

    }
}

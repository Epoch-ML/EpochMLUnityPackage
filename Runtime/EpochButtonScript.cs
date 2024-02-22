using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System.Threading.Tasks;

using Debug = UnityEngine.Debug;

public class EpochButtonScript : MonoBehaviour {

    public Sprite imageInactive;
    public Sprite imageActive;

    private Button button;
    private Image image;

    private string epochCLIBuildPath = "epoch_cli_build_path";
    
    
    [Header("Epoch Settings")]
    public string epochCLIHostname = "dev.epochml.com";
    public string epochCLIEmail = "";
    public string epochCLIPassword = "";
    public string epochCLIProjectURI = "7c95ad89";
    
    [SerializeField]
    private string epochSessionURI = "";
    [SerializeField]
    private uint epochSessionID = 0;
    
    private string logFilename = "epoch_cli_lib.log";
    
    [Header("Video Settings")]
    private string videoFilename = "temp_video.mp4";
    private string encoderCodec = "libx264";
    public string sourcePixelFormat = "bgra";
    public string targetPixelFormat = "yuv420p";
    public uint framerate = 20;
    private uint sourceWidth = 0;
    private uint sourceHeight = 0;
    public uint targetWidth = 1280;
    public uint targetHeight = 720;
    
    public Stopwatch stopwatch;

    [FormerlySerializedAs("frameIndex")] public uint frameIdx = 0;
    
    private bool isPressed = false;
    private bool isCLIReady = false;

    private IntPtr cliInstance;

    private bool rotateImage = false;
    private float rotationSpeed = -150.0f;
    
    private bool isNewSessionTaskCompleted = false;
    private Task<string> newSessionTask;
    
    private bool isUploadSessionTaskCompleted = false;
    private Task<string> uploadSessionTask;
    
    // Start is called before the first frame update
    void Start() {
        button = GetComponent<Button>();
        
        Transform epochButtonImageTransform = transform.Find("EpochButtonImage");
        image = epochButtonImageTransform.GetComponent<Image>();

        button.onClick.AddListener(OnButtonPressed);
        
        // Start screen capture routine
        StartCoroutine(ScreenCaptureRoutine());
    }

    // Update is called once per frame
    void Update() {
        if(rotateImage) {
            image.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);    
        }

        if (newSessionTask != null && newSessionTask.IsCompleted && !isNewSessionTaskCompleted) {
            isNewSessionTaskCompleted = true;
            Debug.Log("New Session task complete");

            image.sprite = imageActive;
            isCLIReady = true;
            rotateImage = false;
            image.transform.rotation = Quaternion.identity;
            stopwatch = Stopwatch.StartNew();
            
        }

        if (uploadSessionTask != null && uploadSessionTask.IsCompleted && !isUploadSessionTaskCompleted) {
            isUploadSessionTaskCompleted = true;
            Debug.Log("Upload Session task completed");
            rotateImage = false;
            image.transform.rotation = Quaternion.identity;
            image.sprite = imageInactive;
        }
    }

    private void StartNewSession() {
        Debug.Log($"Running new session async task");
        
        isNewSessionTaskCompleted = false;
        
        logFilename = Path.Combine(Application.persistentDataPath, logFilename);
        epochCLIBuildPath = Path.Combine(Application.persistentDataPath, epochCLIBuildPath);
        videoFilename = Path.Combine(Application.persistentDataPath, videoFilename);
        
        // Screen resolution and apply aspect ratio to target 
        sourceWidth = (uint)Screen.width;
        sourceHeight = (uint)Screen.height;
        
        float screenAspectRatio = ((float)(sourceHeight) / (float)(sourceWidth));
        targetHeight = (uint)((float)(targetWidth) * screenAspectRatio);
        
        newSessionTask = Task.Run(() => {
            InitializeEpochCLI(epochCLIBuildPath, logFilename);
            
            StartNewSessionEpoch(videoFilename, sourceWidth, sourceHeight, targetWidth, targetHeight);
            
            return "new session task complete";
        });
    }
    
    private void CompleteAndUploadSession() {
        Debug.Log($"Complete and upload session async task");
        
        isUploadSessionTaskCompleted = false;

        uploadSessionTask = Task.Run(() => {
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
            
            return "complete and upload session task complete";
        });
    }

    private void StartNewSessionEpoch(
        string videoPath, 
        uint sourceWidth, 
        uint sourceHeight, 
        uint targetWidth, 
        uint targetHeight
    ) {
        Debug.Log($"StartNewSessionEpoch video {videoPath} source {sourceWidth}x{sourceHeight} target {targetWidth}x{targetHeight}");

        // initialize the encoder 
        Debug.Log($"Initialize Epoch CLI encoder with {sourceWidth}x{sourceHeight} -> {targetWidth}x{targetHeight}, saving to {videoPath}");
        EpochCLI.epoch_cli_initialize_ffmpeg_encoder_consumer(
            cliInstance,
            videoPath,
            encoderCodec,
            framerate,
            sourcePixelFormat,
            sourceWidth, sourceHeight,
            targetPixelFormat,
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
    }
    
    private void InitializeEpochCLI(string buildPath, string logPath) {
        Debug.Log($"InitializeEpochCLI build path {buildPath} log path {logPath}");

        cliInstance = EpochCLI.epoch_cli_new(
            buildPath,
            logPath,
            true
        );
        
        Debug.Log("initializaing Epoch CLI");
        EpochCLI.epoch_cli_initialize(
            cliInstance, 
            epochCLIHostname, 
            1
        );
        
        Debug.Log("Logging in with Epoch CLI");
        EpochCLI.epoch_cli_login(
            cliInstance, 
            epochCLIEmail, 
            epochCLIPassword
        );
    }

    private void OnButtonPressed() {
        if (isPressed == false) {
            
            isPressed = true;
            rotateImage = true;
            
            StartNewSession();
        }
        else {
            isPressed = false;
            isCLIReady = false;
            rotateImage = true;

            CompleteAndUploadSession();
        }
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
        //Debug.Log($"capturing screen {frameIdx}");
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
        //Debug.Log($"got {pixels.Length} pixels buffer len {byteArray.Length} for {sourceWidth}x{sourceHeight} expected {expectedBytes}");
        
        GCHandle pinnedArray = GCHandle.Alloc(byteArray, GCHandleType.Pinned);
        IntPtr bufferPtr = pinnedArray.AddrOfPinnedObject();
        
        //Debug.Log($"encoding frame with cli {frameIdx}");
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

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
using UnityEditor.UI;
using Debug = UnityEngine.Debug;

public class DashcamButtonScript : MonoBehaviour {

    public Sprite imageInactive;
    public Sprite imageActive;

    private Button button;
    private Image image;

    private string epochCLIBuildPath = "epoch_cli_build_path";

    public bool enableOnRun = true;
    public uint dashcamMinutes = 1;
    
    [Header("Epoch Settings")]
    public string epochCLIHostname = "epochml.com";
    public string epochCLIEmail = "";
    public string epochCLIPassword = "";
    public string epochCLIProjectURI = "";
    
    private string epochSessionURI = "";
    private uint epochSessionID = 0;
    private string logFilename = "epoch_cli_lib.log";
    
    [Header("Video Settings")]
    public string sourcePixelFormat = "bgra";
    public string targetPixelFormat = "yuv420p";
    public uint framerate = 20;
    public uint targetWidth = 1280;
    public uint targetHeight = 720;
    
    private uint sourceWidth = 0;
    private uint sourceHeight = 0;
    private string videoFilename = "temp_video.mp4";
    private string encoderCodec = "libx264";
    
    [Header("Debug")]
    public Text debugText;
    
    public Stopwatch stopwatch;

    private uint frameIdx = 0;
    private bool isCLIReady = false;

    private IntPtr cliInstance;

    private bool rotateImage = false;
    private float rotationSpeed = -150.0f;

    private bool isSartDashcamTaskCompleted = false;
    private Task<string> startDashcamTask;
    
    // TODO: this
    // private bool isUploadSessionTaskCompleted = false;
    // private Task<string> uploadSessionTask;
    
    // Start is called before the first frame update
    void Start() {
        button = GetComponent<Button>();
        
        Transform epochButtonImageTransform = transform.Find("DashcamButtonImage");
        image = epochButtonImageTransform.GetComponent<Image>();

        button.onClick.AddListener(OnButtonPressed);
        
        // Start screen capture routine
        StartCoroutine(ScreenCaptureRoutine());
        StartCoroutine(RotateImageRoutine());

        if (enableOnRun)
        {
            Debug.Log($"Starting dashcam on run");
            StartDashcam();
        }
    }

    // Update is called once per frame
    void Update() {
        

        if (startDashcamTask != null && startDashcamTask.IsCompleted && !isSartDashcamTaskCompleted) {
            isSartDashcamTaskCompleted = true;
            Debug.Log("start dashcam task complete");

            image.sprite = imageActive;
            isCLIReady = true;
            rotateImage = false;
            image.transform.rotation = Quaternion.identity;
            stopwatch = Stopwatch.StartNew();
            
        }
    }

    private void debugTextOutput(string text)  {
        if (debugText) {
            debugText.text += "/n";
            debugText.text += $"{text}";
        }
        else {
            Debug.Log(text);    
        }
    }

    private void StartDashcam() {
        Debug.Log($"Running new session async task");
        
        isSartDashcamTaskCompleted = false;

        //string epochWorkPath = Application.externalStoragePath; 
        string epochWorkPath = Application.persistentDataPath;

        logFilename = Path.Combine(epochWorkPath, logFilename);
        epochCLIBuildPath = Path.Combine(epochWorkPath, epochCLIBuildPath);
        videoFilename = Path.Combine(epochWorkPath, videoFilename);

        debugTextOutput(logFilename);

        // Screen resolution and apply aspect ratio to target 
        sourceWidth = (uint)Screen.width;
        sourceHeight = (uint)Screen.height;
        
        float screenAspectRatio = ((float)(sourceHeight) / (float)(sourceWidth));
        targetHeight = (uint)((float)(targetWidth) * screenAspectRatio);

        startDashcamTask = Task.Run(() => {
            InitializeEpochCLI(epochCLIBuildPath, logFilename);
            StartDashcamEpoch(
                videoFilename, 
                sourceWidth, 
                sourceHeight, 
                targetWidth, 
                targetHeight
            );
            return "start dashcam task complete";
        });
    }
    
    private void StartDashcamEpoch(
        string videoPath, 
        uint sourceWidth, uint sourceHeight, 
        uint targetWidth, uint targetHeight
    ) {
        Debug.Log($"StartDashcamEpoch video {videoPath} source {sourceWidth}x{sourceHeight} target {targetWidth}x{targetHeight}");

        // initialize the encoder 
        Debug.Log($"Initialize Epoch CLI encoder with {sourceWidth}x{sourceHeight} -> {targetWidth}x{targetHeight}, saving to {videoPath}");
        EpochCLI.epoch_cli_initialize_dashcam(
            cliInstance,
            videoPath,
            dashcamMinutes * 60,
            encoderCodec,
            framerate,
            sourcePixelFormat,
            sourceWidth, sourceHeight,
            targetPixelFormat,
            targetWidth, targetHeight
        );
            
        // TODO: add session tracking, even without video etc
        // Debug.Log($"login, auth, and start new session");
        // var newSession = EpochCLI.epoch_cli_login_auth_and_start_new_session(
        //     cliInstance,
        //     epochCLIEmail,
        //     epochCLIPassword,
        //     epochCLIProjectURI
        // );
        //     
        // epochSessionURI = Marshal.PtrToStringAnsi(newSession.Uri);
        // epochSessionID = newSession.Id;
    }
    
    private void InitializeEpochCLI(string buildPath, string logPath) {
        Debug.Log($"InitializeEpochCLI build path {buildPath} log path {logPath}");
        
        // Test to see if we can load the shared lib
        try {
            using (var systemClass = new AndroidJavaClass("java.lang.System")) {
                systemClass.CallStatic("loadLibrary", "epoch_cli_lib");
            }
        }
        catch (Exception ex) {
            Debug.LogError($"Error loading library: {ex.Message}");
            debugTextOutput($"Error loading library: {ex.Message}");
        }
        
        cliInstance = EpochCLI.epoch_cli_create(
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
        // TODO: save the dashcam video here
        // TODO: upload and create a session 
        
        Debug.LogError($"dashcam button pressed");
    }

    IEnumerator RotateImageRoutine() {
        
        Transform epochButtonImageTransform = transform.Find("EpochButtonImage");
        RectTransform rectTransform = epochButtonImageTransform.GetComponent<RectTransform>();
        
        float previousTime = Time.realtimeSinceStartup;

        while (true) {
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - previousTime;
            previousTime = currentTime;

            if(rotateImage) {
                rectTransform.Rotate(0f, 0f, rotationSpeed * deltaTime);
            }

        yield return null; // Wait for the next frame
        }
    }

    IEnumerator ScreenCaptureRoutine() {
        while (true) {
            yield return new WaitForEndOfFrame(); // Wait until all frame rendering is done
            
            if (isCLIReady) {
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using Fove.Unity;
using Fove;
using System.Text;

using Stopwatch = System.Diagnostics.Stopwatch;
using System.Globalization;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScriptExecutionOrder : Attribute
{
    public int order;
    public ScriptExecutionOrder(int order) { this.order = order; }
}

#if UNITY_EDITOR
[InitializeOnLoad]
public class ScriptExecutionOrderManager
{
    static ScriptExecutionOrderManager()
    {
        foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
        {
            if (monoScript.GetClass() == null)
                continue;

            foreach (var attr in Attribute.GetCustomAttributes(monoScript.GetClass(), typeof(ScriptExecutionOrder)))
            {
                var newOrder = ((ScriptExecutionOrder)attr).order;
                if (MonoImporter.GetExecutionOrder(monoScript) != newOrder)
                    MonoImporter.SetExecutionOrder(monoScript, newOrder);
            }
        }
    }
}
#endif

// A behaviour class which records eye gaze data (with floating-point timestamps) and writes it out to a .csv file
// for continued processing.
[ScriptExecutionOrder(1000)] // execute last so that user current frame transformations on the fove interface pose are included
public class GazeRecorder : MonoBehaviour
{
    public const string OutputFolder = "ResultData";

    public enum RecordingSync
    {
        SyncWithRendering, // synch with vsynch
        SyncWithEyeTracking, // synch with eye frame
    }

    public enum CoordinateSpace
    {
        World,
        Local,
        HMD,
    }

    [Serializable]
    public class ExportSettings
    {
        [Tooltip("The time since the application started.")]
        public bool ApplicationTime = true;

        [Tooltip("The id of the eye frame used to compute the eye tracking data")]
        public bool EyeFrameId = true;

        [Tooltip("The timestamp of the eye frame used to compute the eye tracking data")]
        public bool EyeFrameTimestamp = false;

        [Tooltip("Custom mark set by the user when pressing the corresponding key. ")]
        public bool UserMark = true;

        [Tooltip("The headset orientation quaternion")]
        public bool HeadsetOrientation = true;

        [Tooltip("The headset position")]
        public bool HeadsetPosition = true;

        [Tooltip("The two eyes combined gaze ray")]
        public bool CombinedRay = true;

        [Tooltip("The gaze depth")]
        public bool GazeDepth = true;

        [Tooltip("The gaze ray for each eye separately")]
        public bool EyeRays = true;

        [Tooltip("The raw gaze ray for each eye separately")]
        public bool EyeRaysRaw = true;

        [Tooltip("The open, closed or not detected status of the eyes")]
        public bool EyesState = true;

        [Tooltip("The current radius of the pupil")]
        public bool PupilsRadius = true;

        [Tooltip("The Unity object gazed by the user")]
        public bool GazedObject = true;

        [Tooltip("The torsion of the left & right eyes in degree")]
        public bool EyeTorsion = true;

        [Tooltip("Whether the user is wearing the headset")]
        public bool UserPresence = true;

        [Tooltip("Whether the user is shifting attention (performs a saccade)")]
        public bool UserAttentionShift = true;

        [Tooltip("The interpupillary distance of the user")]
        public bool IPD = true;

        [Tooltip("The interocular distance of the user")]
        public bool IOD = true;

        [Tooltip("The radius of the eyeball")]
        public bool EyeballRadius = true;

        [Tooltip("The 2d coordinates of the gaze on the HMD screen")]
        public bool ScreenGaze = true;

        [Tooltip("The shape of the eye (eyelids) on the eye camera image")]
        public bool EyeShape = true;

        [Tooltip("The shape of the pupil ellipse on the eye camera image")]
        public bool PupilShape = true;
    }

    // Require a reference (assigned via the Unity Inspector panel) to a FoveInterface object.
    // This could be either FoveInterface
    [Tooltip("This should be a reference to a FoveInterface object of the scene.")]
    public FoveInterface fove = null;

    // Pick a key (customizable via the Inspector panel) to toggle recording.
    [Tooltip("Pressing this key will toggle data recording.")]
    public KeyCode toggleRecordingKey = KeyCode.Space;

    // Pick a key (customizable via the Inspector panel) to toggle recording.
    [Tooltip("Pressing this key will add a mark in the data recording.")]
    public KeyCode markFrameKey = KeyCode.X;

    [Tooltip("Specify the rate at which gaze sampling is performed. `SyncWithRendering` synchronizes the gaze samples with the rendering." +
        "One gaze sample is taken per frame (eg. 70FPS on Fove0). `SyncWithEyeTracking synchronizes the gaze samples with incoming eye frames. " + 
        "A new gaze sample is taken every time eye data is updated (eg. 120FPS on Fove0)")]
    public RecordingSync recordingSync;

    [Tooltip("Specify the coordinate space used for combined gaze and eye vector rays.")]
    public CoordinateSpace gazeCoordinateSpace;

    // The name of the file to write our results into
    [Tooltip("The base name of the file. Don't add any extensions, as \".csv\" will be appended to whatever you put here.")]
    public string outputFileName = "fove_recorded_results";

    // Check this to overwrite existing data files rather than incrementing a value each time.
    [Tooltip("If the specified filename already exists, the recorder will increment a counter until an unused " +
             "filename is found.")]
    public bool overwriteExistingFile = false;

    [Tooltip("Specify which data fields to export to the csv file")]
    public ExportSettings exportFields;

    // The number a data to record before writing out to disk
    [Tooltip("The number of entries to store in memory before writing asynchronously to disk")]
    public int writeAtDataCount = 1000;

    //=================//
    // Private members //
    //=================//

    // An internal flag to track whether we should be recording or not
    private bool shouldRecord;

    // A struct for recording in one place all the information that needs to be recorded for each frame
    // If you need more data recorded, you can add more fields here. Just be sure to write is out as well later on.
    struct Datum
    {
        public float AppTime;
        public bool UserMark;
        public FrameTimestamp EyeFrameTimestamp;
        public Result<Quaternion> HeadsetOrientation;
        public Result<Vector3> HeadsetPosition;
        public Result<string> GazedObjectName;
        public Result<Ray> CombinedRay;
        public Result<float> GazeDepth;
        public Stereo<Result<Ray>> EyeRays;
        public Stereo<Result<Ray>> EyeRaysRaw;
        public Stereo<Result<EyeState>> EyesState;
        public Stereo<Result<float>> PupilsRadius;
        public Stereo<Result<float>> EyeTorsions;
        public Result<bool> UserPresence;
        public Result<bool> UserAttentionShift;
        public Result<float> IPD;
        public Result<float> IOD;
        public Stereo<Result<float>> EyeballRadius;
        public Stereo<Result<Vector2>> ScreenGaze;
        public Stereo<Result<Fove.Unity.EyeShape>> EyeShape;
        public Stereo<Result<Fove.Unity.PupilShape>> PupilShape;
    }

    const char CsvSeparator = ',';

    interface IDataWriter
    {
        void Append(StringBuilder stringBuilder);
    }

    class AggregatedData : List<Datum>
    {
        public AggregatedData(int reserveCount) : base(reserveCount) { }
    }

    class ConcurrentQueue<T>
    {
        Queue<T> queue = new Queue<T>();

        public bool IsEmpty 
        {  
            get
            {
                lock (queue)
                    return queue.Count == 0;
            } 
        }

        public void Enqueue(T t)
        {
            lock (queue)
                queue.Enqueue(t);
        }

        public bool TryDequeue(out T t)
        {
            lock (queue)
            {
                if (queue.Count == 0)
                {
                    t = default(T);
                    return false;
                }

                t = queue.Dequeue();
                return true;
            }
        }
    }

    // Use another instead of the Headset here to not mess with the gaze client data (in 120Hz mode new data fetch may happen in middle of frames)
    private Headset headset;

    // A list for storing the recorded data from many frames
    private AggregatedData dataSlice;

    // This reference to a list is used by the writing thread. Essentially, one list is being populated (above)
    // while another can be writing out to disk asynchronously (this one).
    private ConcurrentQueue<IDataWriter> dataToWrite = new ConcurrentQueue<IDataWriter>();

    private EventWaitHandle threadWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

    // Track whether or not the write thread should live.
    private bool writeThreadShouldLive = true;

    // Track whether or not the write thread should live.
    private bool collectThreadShouldLive = true;

    // The thread object which we will call into the write thread function.
    private Thread writeThread;

    // The thread object which we will call into the write thread function.
    private Thread collectThread;

    // Fove interface transformation matrices
    private struct UnityThreadData
    {
        public Vector3 localScale;
        public Vector3 localPosition;
        public Quaternion localOrientation;
        public Matrix4x4 parentTransform;
        public Result<string> gazedObject;
        public bool markKeyDown;
    }
    private UnityThreadData unityThreadData;
    private readonly object unityThreadDataLock = new object();

    private Stopwatch stopwatch = new Stopwatch(); // Unity Time.time can't be use outside of main thread.
    
    // Use this for initialization.
    void Start () 
    {
        // initialize thread data before starting the threads
        unityThreadData = new UnityThreadData
        {
            localScale = Vector3.one,
            localPosition = Vector3.zero,
            localOrientation = Quaternion.identity,
            parentTransform = Matrix4x4.identity,
            gazedObject = new Result<string>("", ErrorCode.Data_NoUpdate),
            markKeyDown = false
        };

        stopwatch.Start();
        if (!Stopwatch.IsHighResolution)
            Debug.LogWarning("GazeRecorder: High precision stopwatch is not supported on this machine. Recorded frame times may not be highly accurate.");

        // Check to make sure that the FOVE interface variable is assigned. This prevents a ton of errors
        // from filling your log if you forget to assign the interface through the inspector.
        if (fove == null)
        {
            Debug.LogWarning("GazeRecorder: Forgot to assign a Fove interface to the FOVERecorder object.");
            enabled = false;
            return;
        }

        var caps = ClientCapabilities.EyeTracking;
        if (exportFields.GazeDepth)
            caps |= ClientCapabilities.GazeDepth;
        if (exportFields.PupilsRadius)
            caps |= ClientCapabilities.PupilRadius;
        if (exportFields.GazedObject)
            caps |= ClientCapabilities.GazedObjectDetection;
        if (exportFields.EyeTorsion)
            caps |= ClientCapabilities.EyeTorsion;
        if (exportFields.UserPresence)
            caps |= ClientCapabilities.UserPresence;
        if (exportFields.UserAttentionShift)
            caps |= ClientCapabilities.UserAttentionShift;
        if (exportFields.IPD)
            caps |= ClientCapabilities.UserIPD;
        if (exportFields.IOD)
            caps |= ClientCapabilities.UserIOD;
        if (exportFields.EyeballRadius)
            caps |= ClientCapabilities.EyeballRadius;
        if (exportFields.EyeShape)
            caps |= ClientCapabilities.EyeShape;
        if (exportFields.PupilShape)
            caps |= ClientCapabilities.PupilShape;

        if (recordingSync == RecordingSync.SyncWithRendering)
        {
            headset = FoveManager.Headset;
            FoveManager.RegisterCapabilities(caps);
        }
        else
        { 
            headset = new Headset(ClientCapabilities.PositionTracking | ClientCapabilities.OrientationTracking);
            headset.RegisterCapabilities(caps);
        }

        // We set the initial data slice capacity to the expected size + 1 so that we never waste time reallocating and
        // copying data under the hood. If the system ever requires more than a single extra entry, there is likely
        // a severe problem causing delays which should be addressed.
        dataSlice = new AggregatedData(writeAtDataCount + 1);

        // If overwrite is not set, then we need to make sure our selected file name is valid before proceeding.
        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);
        {
            string testFileName = Path.Combine(OutputFolder, outputFileName + ".csv");
            if (!overwriteExistingFile)
            {
                int counter = 1;
                while (File.Exists(testFileName))
                {
                    testFileName = Path.Combine(OutputFolder, outputFileName + "_" + (counter++) + ".csv"); // e.g., "results_12.csv"
                }
            }
            outputFileName = testFileName;

            Debug.Log("GazeRecorder: Writing data to " + outputFileName);
        }

        dataToWrite.Enqueue(new DataHeaderSerializer(exportFields));

        // Create the write thread to call "WriteThreadFunc", and then start it.
        writeThread = new Thread(WriteThreadFunc);
        writeThread.Start();

        StartCoroutine(JobsSpawnerCoroutine());
    }

    // Unity's standard Update function, here used only to listen for input to toggle data recording
    void Update()
    {
        // If you press the assigned key, it will toggle the "recordingStopped" variable.
        if (Input.GetKeyDown(toggleRecordingKey))
        {
            shouldRecord = !shouldRecord;
            Debug.Log("GazeRecorder: " + (shouldRecord ? "Starting" : "Stopping") + " data collecton...");
        }
    }

    // This is called when the program quits, or when you press the stop button in the editor (if running from there).
    void OnApplicationQuit()
    {
        shouldRecord = false;
        collectThreadShouldLive = false;
        if(collectThread != null)
            collectThread.Join(100);

        if (writeThread != null)
        {
            // Tell the thread to end, then release the wait handle so it can finish.
            writeThreadShouldLive = false; 
            flushData();

            // Wait for the write thread to end (up to 1 second).
            writeThread.Join(1000);
        }
    }
    IEnumerator JobsSpawnerCoroutine()
    {
        // Wait for the eye tracking to be ready before starting the recording (recording null data is not very useful)
        // Another reason to wait is because WaitForProcessedEyeFrame return errors (API_Timeout/Unknown)
        // when called before the client connection to the service could be properly established
        var nextFrameAwaiter = new WaitForEndOfFrame();
        while (true)
        {
            var etReady = headset.IsEyeTrackingReady();
            if (etReady.IsValid && etReady.value)
                break;

            if (shouldRecord) // log a message to the user to notify him that the recording hasn't actually started yet
                Debug.Log("GazeRecorder: Waiting for the eye tracking to be ready...");

            yield return nextFrameAwaiter;
        }
        if (shouldRecord)
            Debug.Log("GazeRecorder: Eye tracking ready. Start recording data...");

        // if the recording rate is the same as the fove rendering rate,
        // we use a coroutine to be sure that recorded gazes are synchronized with frames
        if (recordingSync == RecordingSync.SyncWithRendering)
        {
            // Coroutines give us a bit more control over when the call happens, and also simplify the code
            // structure. However they are only ever called once per frame -- they processing to happen in
            // pieces, but they shouldn't be confused with threads.
            StartCoroutine(RecordDataCoroutine());
        }
        else // otherwise we just start a vsynch asynchronous thread
        {
            StartCoroutine(RecordFoveTransformCoroutine());
            collectThread = new Thread(CollectThreadFunc);
            collectThread.Start();
        }
    }

    void flushData()
    {
        if (dataSlice != null)
            dataToWrite.Enqueue(new AggregatedDataSerializer(exportFields) { Data = dataSlice });

        dataSlice = new AggregatedData(writeAtDataCount + 1);

        if (!threadWaitHandle.Set())
            Debug.LogError("GazeRecorder: Error setting the event to wake up the file writer thread on application quit");
    }

    private void UpdateFoveInterfaceMatrices()
    {
        var t = fove.transform;
        var markKeyDown = Input.GetKey(markFrameKey);
        var gazedObjectResult = FoveManager.GetGazedObject();
        var gazedObjectName = new Result<string>(gazedObjectResult.value ? gazedObjectResult.value.name : "", gazedObjectResult.error);

        var data = new UnityThreadData
        {
            localScale = t.localScale,
            localPosition = t.localPosition,
            localOrientation = t.localRotation,
            parentTransform = t.parent != null ? t.parent.localToWorldMatrix : Matrix4x4.identity,
            gazedObject = gazedObjectName,
            markKeyDown = markKeyDown,
        };

        lock (unityThreadDataLock)
            unityThreadData = data;
    }

    private void RecordDatum(bool immediate)
    {
        // If recording is stopped (which is it by default), loop back around next frame.
        if (!shouldRecord)
            return;

        if (!immediate) // we run in the same thread as unity we can update the transformations in a synchronized way
            UpdateFoveInterfaceMatrices();

        UnityThreadData unityData;
        lock (unityThreadDataLock)
            unityData = unityThreadData;

        var hmdPosition = unityData.localPosition;
        var hmdOrientation = unityData.localOrientation;
        if (immediate)
        {
            var fetchResult = headset.FetchPoseData();
            var poseResult = headset.GetPose();
            if (fetchResult.IsValid && poseResult.IsValid)
            {
                var pose = poseResult.value;
                if (fove.fetchPosition)
                {
                    var isStanding = fove.poseType == FoveInterface.PlayerPose.Standing;
                    hmdPosition = (isStanding ? pose.standingPosition : pose.position).ToVector3();
                }
                if (fove.fetchOrientation)
                {
                    hmdOrientation = pose.orientation.ToQuaternion();
                }
            }
        }

        Matrix4x4 transformMat;
        {
            var localTransform = Matrix4x4.TRS(hmdPosition, hmdOrientation, unityData.localScale);
            switch (gazeCoordinateSpace)
            {
                case CoordinateSpace.World:
                    transformMat = unityData.parentTransform * localTransform;
                    break;
                case CoordinateSpace.Local:
                    transformMat = localTransform;
                    break;
                default:
                    transformMat = Matrix4x4.identity;
                    break;
            }
        }

        var eyeOffsets = FoveManager.GetEyeOffsets();
        var eyeVectorL = headset.GetGazeVector(Eye.Left).ToUnity();
        var eyeVectorR = headset.GetGazeVector(Eye.Right).ToUnity();

        Stereo<Result<Ray>> eyeRays;
        eyeRays.left = Utils.CalculateWorldGazeVector(ref transformMat, ref eyeOffsets.value.left, ref eyeVectorL);
        eyeRays.right = Utils.CalculateWorldGazeVector(ref transformMat, ref eyeOffsets.value.right, ref eyeVectorR);

        var eyeVectorRawL = headset.GetGazeVectorRaw(Eye.Left).ToUnity();
        var eyeVectorRawR = headset.GetGazeVectorRaw(Eye.Right).ToUnity();

        Stereo<Result<Ray>> eyeRaysRaw;
        eyeRaysRaw.left = Utils.CalculateWorldGazeVector(ref transformMat, ref eyeOffsets.value.left, ref eyeVectorRawL);
        eyeRaysRaw.right = Utils.CalculateWorldGazeVector(ref transformMat, ref eyeOffsets.value.right, ref eyeVectorRawR);

        var foveGazeDepth = headset.GetCombinedGazeDepth();
        var gazeDepth = new Result<float>(foveGazeDepth.value * FoveManager.WorldScale, foveGazeDepth.error);

        var combinedRay = headset.GetCombinedGazeRay().ToUnity();
        combinedRay.value.origin = transformMat.MultiplyPoint(combinedRay.value.origin);
        combinedRay.value.direction = transformMat.MultiplyVector(combinedRay.value.direction).normalized;

        var pupilRadiusLeft = headset.GetPupilRadius(Eye.Left);
        var pupilRadiusRight = headset.GetPupilRadius(Eye.Right);

        var eyeStateL = headset.GetEyeState(Eye.Left);
        var eyeStateR = headset.GetEyeState(Eye.Right);

        var eyeTorsionL = headset.GetEyeTorsion(Eye.Left);
        var eyeTorsionR = headset.GetEyeTorsion(Eye.Right);

        // If you add new fields, be sure to write them here.
        var datum = new Datum
        {
            AppTime = (float)stopwatch.Elapsed.TotalSeconds,
            EyeFrameTimestamp = headset.GetEyeTrackingDataTimestamp().value,
            UserMark = unityData.markKeyDown,
            GazedObjectName = unityData.gazedObject,
            HeadsetPosition = new Result<Vector3>(hmdPosition),
            HeadsetOrientation = new Result<Quaternion>(hmdOrientation),
            CombinedRay = combinedRay,
            GazeDepth = gazeDepth,
            EyeRays = eyeRays,
            EyeRaysRaw = eyeRaysRaw,
            EyesState = new Stereo<Result<EyeState>>(eyeStateL, eyeStateR),
            PupilsRadius = new Stereo<Result<float>>(pupilRadiusLeft, pupilRadiusRight),
            EyeTorsions = new Stereo<Result<float>>(eyeTorsionL, eyeTorsionR),
            UserPresence = headset.IsUserPresent(),
            UserAttentionShift = headset.IsUserShiftingAttention(),
            IPD = headset.GetUserIPD(),
            IOD = headset.GetUserIOD(),
            EyeballRadius = new Stereo<Result<float>>(headset.GetEyeballRadius(Eye.Left), headset.GetEyeballRadius(Eye.Right)),
            ScreenGaze = new Stereo<Result<Vector2>>(headset.GetGazeScreenPosition(Eye.Left).ToUnity(), headset.GetGazeScreenPosition(Eye.Right).ToUnity()),
            EyeShape = new Stereo<Result<Fove.Unity.EyeShape>>(headset.GetEyeShape(Eye.Left).ToUnity(), headset.GetEyeShape(Eye.Right).ToUnity()),
            PupilShape = new Stereo<Result<Fove.Unity.PupilShape>>(headset.GetPupilShape(Eye.Left).ToUnity(), headset.GetPupilShape(Eye.Right).ToUnity()),
        };
        dataSlice.Add(datum);

        if (dataSlice.Count >= writeAtDataCount) 
            flushData();
    }

    // The coroutine function which records data to the dataSlice List<> member
    IEnumerator RecordDataCoroutine()
    {
        var nextFrameAwaiter = new WaitForEndOfFrame();

        // Infinite loops are okay within coroutines because the "yield" statement pauses the function each time to
        // return control to the main program. Great for breaking tasks up into smaller chunks over time, or for doing
        // small amounts of work each frame but potentially outside of the normal Update cycle/call order.
        while (true)
        {
            // This statement pauses this function until Unity has finished rendering a frame. Inside the while loop,
            // this means that this function will resume from here every frame.
            yield return nextFrameAwaiter;

            RecordDatum(false);
        }
    }
    
    // this coroutine is used to fetch the fove interface transformation value from the unity main thread
    IEnumerator RecordFoveTransformCoroutine()
    {
        var nextFrameAwaiter = new WaitForEndOfFrame();

        while (true)
        {
            UpdateFoveInterfaceMatrices();
            yield return nextFrameAwaiter;
        }
    }

    // This is the collecting thread that collect and store data asynchronously from the rendering
    private void CollectThreadFunc()
    {
        while (collectThreadShouldLive)
        {
            headset.FetchEyeTrackingData();
            headset.FetchPoseData();

            RecordDatum(true);

            var result = headset.WaitForProcessedEyeFrame();
            if (result.Failed)
            {
                // In practice we observed 3 errors here: Connect_NotConnected/API_Timeout/UnknownError
                // API_Timeout and UnkownError happen just before/after a connect/disconnection
                // The three case the error Data_NoUpdate will be returned for all Get queries
                // so we don't mind stopping the recording

                Debug.LogWarning("GazeRecorder: An error happened while waiting for next eye frame. Next frame recording synchronization might be off. Error code:" + result.error);
                if (result.error != ErrorCode.API_Timeout)
                    Thread.Sleep(1000 / 120); // to avoid falling into an active infinite loop
            }
        }
    }

    private void WriteDataFromThread()
    {
        IDataWriter writer;
        var builder = new StringBuilder();

        while (!dataToWrite.IsEmpty)
        {
            if (!dataToWrite.TryDequeue(out writer))
                continue;

            writer.Append(builder);
        }

        try
        {
            File.AppendAllText(outputFileName, builder.ToString());
        }
        catch (Exception e)
        {
            Debug.LogWarning("GazeRecorder: Exception writing to data file:\n" + e);
            writeThreadShouldLive = false;
        }
    }

    // This is the writing thread. By offloading file writing to a thread, we are less likely to impact perceived
    // performance inside the Unity game loop, and thus more likely to have accurate, consistent results.
    private void WriteThreadFunc()
    {
        while (writeThreadShouldLive)
        {
            if (threadWaitHandle.WaitOne())
                WriteDataFromThread();
        }

        // Try to write one last time once the thread ends to catch any missed elements
        WriteDataFromThread();
    }

    class DataHeaderSerializer : IDataWriter
    {
        private readonly ExportSettings export;

        public DataHeaderSerializer(ExportSettings export)
        {
            this.export = export;
        }

        public void Append(StringBuilder builder)
        {
            Action<StringBuilder, string> appendValue = (b, h) =>
            {
                b.Append(h).Append(CsvSeparator);
            };
            Action<StringBuilder, string> append = (b, h) =>
            {
                appendValue(b, h);
                appendValue(b, h + " error");
            };
            Action<StringBuilder, string> appendLeftRight = (b, h) =>
            {
                append(b, h + " left");
                append(b, h + " right");
            };

            Action<StringBuilder, string> appendValueXY = (b, h) =>
            {
                appendValue(b, h + " x");
                appendValue(b, h + " y");
            };

            Action<StringBuilder, string> appendValueXYZ = (b, h) =>
            {
                appendValue(b, h + " x");
                appendValue(b, h + " y");
                appendValue(b, h + " z");
            };

            Action<StringBuilder, string> appendRay = (b, h) =>
            {
                appendValueXYZ(b, h + " pos");
                appendValueXYZ(b, h + " dir");
                appendValue(b, h + " error");
            };

            Action<StringBuilder, string> appendVector2 = (b, h) =>
            {
                appendValueXY(b, h);
                appendValue(b, h + " error");
            };

            Action<StringBuilder, string> appendVector3 = (b, h) =>
            {
                appendValueXYZ(b, h);
                appendValue(b, h + " error");
            };

            Action<StringBuilder, string> appendQuaternion = (b, h) =>
            {
                appendValueXYZ(b, h);
                appendValue(b, h + " w");
                appendValue(b, h + " error");
            };

            // Append the full data header to the builder

            builder.Append(CsvSeparator); // keep the first column for the input file

            if (export.ApplicationTime)
                appendValue(builder, "Application Time");

            if (export.EyeFrameId)
                appendValue(builder, "Eye Frame ID");

            if (export.EyeFrameTimestamp)
                appendValue(builder, "Eye Frame Timestamp (micro-seconds)");

            if (export.UserMark)
                appendValue(builder, "User Mark");

            if (export.HeadsetPosition)
                appendVector3(builder, "Headset Position");

            if (export.HeadsetOrientation)
                appendQuaternion(builder, "Headset Orientation Quaternion");

            if (export.CombinedRay)
                appendRay(builder, "Combined Gaze Ray");

            if (export.GazeDepth)
                append(builder, "Gaze Depth");

            if (export.EyeRays)
            {
                string EyeRayHeader = "Eye Ray";
                appendRay(builder, EyeRayHeader + " left");
                appendRay(builder, EyeRayHeader + " right");
            }

            if (export.EyeRaysRaw)
            {
                string EyeRayRawHeader = "Eye Ray Raw";
                appendRay(builder, EyeRayRawHeader + " left");
                appendRay(builder, EyeRayRawHeader + " right");
            }

            if (export.EyesState)
                appendLeftRight(builder, "Eye State");

            if (export.PupilsRadius)
                appendLeftRight(builder, "Pupil Radius (millimeters)");

            if (export.GazedObject)
                append(builder, "Gazed Object");

            if (export.EyeTorsion)
                appendLeftRight(builder, "Eye Torsion (degrees)");

            if (export.UserPresence)
                append(builder, "User Presence");

            if (export.UserAttentionShift)
                append(builder, "User Attention Shift");

            if (export.IPD)
                append(builder, "IPD (millimeters)");

            if (export.IOD)
                append(builder, "IOD (millimeters)");

            if (export.EyeballRadius)
                appendLeftRight(builder, "Eyeball Radius (millimiters)");

            if (export.ScreenGaze)
            {
                string ScreenGazeHeader = "Screen Gaze";
                appendVector2(builder, ScreenGazeHeader + " left");
                appendVector2(builder, ScreenGazeHeader + " right");
            }

            if (export.EyeShape)
            {
                for (int eye = 0; eye < 2; eye++)
                {
                    var h = "Eye Shape" + (eye == 0 ? " left" : " right");
                    for (int i = 0; i < Fove.Unity.EyeShape.OutlinePointCount; i++)
                        appendValueXY(builder, h + " point " + i);
                    appendValue(builder, h + " error");
                }
            }

            if (export.PupilShape)
            {
                for (int eye = 0; eye < 2; eye++)
                {
                    var h = "Pupil Shape" + (eye == 0 ? " left" : " right");
                    appendValueXY(builder, h + " center");
                    appendValueXY(builder, h + " size");
                    appendValue(builder, h + " angle (degrees)");
                    appendValue(builder, h + " error");
                }
            }

            builder.Remove(builder.Length - 1, 1); // remove the last separator of the line
            builder.AppendLine();
        }
    }

    class AggregatedDataSerializer : IDataWriter
    {
        private readonly ExportSettings export;

        public AggregatedData Data { get; set; }

        private string timeFormat;
        private string angleFormat;
        private string vectorFormat;
        private string eyeSizeFormat;
        private string eyePixelFormat;

        public AggregatedDataSerializer(ExportSettings export)
        {
            this.export = export;

            // Setup the significant digits argument strings used when serializing numbers to text for the CSV
            angleFormat = "{0:F3}";
            vectorFormat = "{0:F5}";
            timeFormat = "{0:F4}";
            eyeSizeFormat = "{0:F2}";
            eyePixelFormat = "{0:F2}";
        }

        private void AppendValue(StringBuilder builder, string value)
        {
            builder.Append(value);
            builder.Append(CsvSeparator);
        }
        private void AppendValue(StringBuilder builder, long value)
        {
            builder.Append(value);
            builder.Append(CsvSeparator);
        }

        private void AppendValue(StringBuilder builder, string format, float value)
        {
            builder.AppendFormat(CultureInfo.InvariantCulture, format, value);
            builder.Append(CsvSeparator);
        }

        private void AppendValue(StringBuilder builder, ErrorCode error)
        {
            if (error != ErrorCode.None)
                builder.Append(error.ToString());
            builder.Append(CsvSeparator);
        }

        private void Append(StringBuilder builder, string value, ErrorCode error)
        {
            AppendValue(builder, value);
            AppendValue(builder, error);
        }

        private void Append(StringBuilder builder, string format, float value, ErrorCode error)
        {
            AppendValue(builder, format, value);
            AppendValue(builder, error);
        }

        private void Append(StringBuilder builder, string format, Result<float> result, float multiplier = 1)
        {
            Append(builder, format, result.value * multiplier, result.error);
        }

        private void AppendValue(StringBuilder builder, string format, Vector2 v)
        {
            AppendValue(builder, format, v.x);
            AppendValue(builder, format, v.y);
        }

        private void AppendValue(StringBuilder builder, string format, Vector3 v)
        {
            AppendValue(builder, format, v.x);
            AppendValue(builder, format, v.y);
            AppendValue(builder, format, v.z);
        }

        private void Append(StringBuilder builder, Result<Ray> ray)
        {
            AppendValue(builder, vectorFormat, ray.value.origin);
            AppendValue(builder, vectorFormat, ray.value.direction);
            AppendValue(builder, ray.error);
        }

        private void Append(StringBuilder builder, Result<bool> state)
        {
            AppendValue(builder, state.value ? "1" : "0");
            AppendValue(builder, state.error);
        }

        private void Append(StringBuilder builder, Result<Vector2> point)
        {
            AppendValue(builder, vectorFormat, point.value);
            AppendValue(builder, point.error);
        }

        private void Append(StringBuilder builder, Result<Vector3> v)
        {
            AppendValue(builder, vectorFormat, v.value);
            AppendValue(builder, v.error);
        }

        private void Append(StringBuilder builder, Result<Quaternion> q)
        {
            AppendValue(builder, vectorFormat, q.value.x);
            AppendValue(builder, vectorFormat, q.value.y);
            AppendValue(builder, vectorFormat, q.value.z);
            AppendValue(builder, vectorFormat, q.value.w);
            AppendValue(builder, q.error);
        }

        public void Append(StringBuilder builder)
        {
            Debug.Log("Writing " + Data.Count + " lines");

            foreach (var datum in Data)
            {
                builder.Append(CsvSeparator);

                // This writes each element in the data list as a CSV-formatted line.
                if (export.ApplicationTime)
                    AppendValue(builder, timeFormat, datum.AppTime);

                if (export.EyeFrameId)
                    AppendValue(builder, datum.EyeFrameTimestamp.id);

                if (export.EyeFrameTimestamp)
                    AppendValue(builder, datum.EyeFrameTimestamp.timestamp);

                if (export.UserMark)
                {
                    if (datum.UserMark)
                        builder.Append('X');

                    builder.Append(CsvSeparator);
                }

                if (export.HeadsetPosition)
                    Append(builder, datum.HeadsetPosition);

                if (export.HeadsetOrientation)
                    Append(builder, datum.HeadsetOrientation);

                if (export.CombinedRay)
                    Append(builder, datum.CombinedRay);

                if (export.GazeDepth)
                    Append(builder, vectorFormat, datum.GazeDepth);

                if (export.EyeRays)
                {
                    Append(builder, datum.EyeRays.left);
                    Append(builder, datum.EyeRays.right);
                }

                if (export.EyeRaysRaw)
                {
                    Append(builder, datum.EyeRaysRaw.left);
                    Append(builder, datum.EyeRaysRaw.right);
                }

                if (export.EyesState)
                {
                    Append(builder, datum.EyesState.left.value.ToString(), datum.EyesState.left.error);
                    Append(builder, datum.EyesState.right.value.ToString(), datum.EyesState.right.error);
                }

                if (export.PupilsRadius)
                {
                    Append(builder, eyeSizeFormat, datum.PupilsRadius.left, 1000);
                    Append(builder, eyeSizeFormat, datum.PupilsRadius.right, 1000);
                }

                if (export.GazedObject)
                    Append(builder, datum.GazedObjectName.value, datum.GazedObjectName.error);

                if (export.EyeTorsion)
                {
                    Append(builder, angleFormat, datum.EyeTorsions.left);
                    Append(builder, angleFormat, datum.EyeTorsions.right);
                }

                if (export.UserPresence)
                    Append(builder, datum.UserPresence);

                if (export.UserAttentionShift)
                    Append(builder, datum.UserAttentionShift);

                if (export.IPD)
                    Append(builder, eyeSizeFormat, datum.IPD, 1000);

                if (export.IOD)
                    Append(builder, eyeSizeFormat, datum.IOD, 1000);

                if (export.EyeballRadius)
                {
                    Append(builder, eyeSizeFormat, datum.EyeballRadius.left, 1000);
                    Append(builder, eyeSizeFormat, datum.EyeballRadius.right, 1000);
                }

                if (export.ScreenGaze)
                {
                    Append(builder, datum.ScreenGaze.left);
                    Append(builder, datum.ScreenGaze.right);
                }

                if (export.EyeShape)
                {
                    foreach (var eyeShape in datum.EyeShape)
                    {
                        foreach (var point in eyeShape.value.Outline)
                            AppendValue(builder, eyePixelFormat, point);
                        AppendValue(builder, eyeShape.error);
                    }
                }

                if (export.PupilShape)
                {
                    foreach (var pupilShape in datum.PupilShape)
                    {
                        AppendValue(builder, eyePixelFormat, pupilShape.value.center);
                        AppendValue(builder, eyePixelFormat, pupilShape.value.size);
                        AppendValue(builder, angleFormat, pupilShape.value.angle);
                        AppendValue(builder, pupilShape.error);
                    }
                }

                if (builder.Length > 2) // remove the last "," of the line
                    builder.Remove(builder.Length - 1, 1);

                builder.AppendLine();
            }
        }
    }
}

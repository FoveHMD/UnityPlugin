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

    public enum RecordingRate
    {
        _70FPS, // synch with vsynch
        _120FPS, // synch with eye frame
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

    [Tooltip("Specify the rate at which gaze sampling is performed. 70FPS samples the gaze once every frame." +
        "120FPS samples the gaze once every new incoming eye data")]
    public RecordingRate recordingRate;

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
        public Result<Quaternion> HeadsetOrientation;
        public Result<Vector3> HeadsetPosition;
        public Result<string> GazedObjectName;
        public Result<Ray> CombinedRay;
        public Result<float> GazeDepth;
        public Stereo<Result<Ray>> EyeRays;
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
    private class UnityThreadData
    {
        public Matrix4x4 HMDToLocal;
        public Matrix4x4 HMDToWorld;
        public Result<string> gazedObject = new Result<string>("", ErrorCode.Data_NoUpdate);
        public bool markKeyDown;
        public Vector3 HMDPosition;
        public Quaternion HMDOrientation;
    }
    private UnityThreadData unityThreadData = new UnityThreadData();

    private Stopwatch stopwatch = new Stopwatch(); // Unity Time.time can't be use outside of main thread.
    
    // Use this for initialization.
    void Start () 
    {
        stopwatch.Start();
        if (!Stopwatch.IsHighResolution)
            Debug.LogWarning("High precision stopwatch is not supported on this machine. Recorded frame times may not be highly accurate.");

        // Check to make sure that the FOVE interface variable is assigned. This prevents a ton of errors
        // from filling your log if you forget to assign the interface through the inspector.
        if (fove == null)
        {
            Debug.LogWarning("Forgot to assign a Fove interface to the FOVERecorder object.");
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

        FoveManager.RegisterCapabilities(caps); 

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

            Debug.Log("Writing data to " + outputFileName);
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
            Debug.Log(shouldRecord ? "Starting" : "Stopping" + " data recording...");
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
        // ensure that the headset is connected and ready before starting any recording
        var nextFrameAwaiter = new WaitForEndOfFrame();
        while (!FoveManager.IsHardwareConnected())
            yield return nextFrameAwaiter;

        // if the recording rate is the same as the fove rendering rate,
        // we use a coroutine to be sure that recorded gazes are synchronized with frames
        if (recordingRate == RecordingRate._70FPS)
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
            Debug.LogError("Error setting the event to wake up the file writer thread on application quit");
    }

    private void UpdateFoveInterfaceMatrices(bool immediate)
    {
        var t = fove.transform;

        var markKeyDown = Input.GetKey(markFrameKey);
        var gazedObjectResult = FoveManager.GetGazedObject();
        var gazedObjectName = new Result<string>(gazedObjectResult.value? gazedObjectResult.value.name : "", gazedObjectResult.error);

        if (immediate)
        {
            // In the case of 120 FPS recording rate, we re-fetch the HMD latest pose
            // and locally recalculate the fove interface local transform
            var isStanding = fove.poseType == FoveInterface.PlayerPose.Standing;
            var hmdAdjustedPosition = FoveManager.GetHmdPosition(isStanding);
            var localPos = fove.fetchPosition? hmdAdjustedPosition : t.position;
            var localRot = fove.fetchOrientation? FoveManager.GetHmdRotation() : t.rotation;

            var parentTransfo = t.parent != null ? t.parent.localToWorldMatrix : Matrix4x4.identity;
            var localTransfo = Matrix4x4.TRS(localPos, localRot, t.localScale);

            lock (unityThreadData)
            {
                unityThreadData.HMDToWorld = parentTransfo * localTransfo;
                unityThreadData.HMDToLocal = localTransfo;
                unityThreadData.markKeyDown = markKeyDown;
                unityThreadData.gazedObject = gazedObjectName;
                unityThreadData.HMDPosition = localPos;
                unityThreadData.HMDOrientation = localRot;
            }
        }
        else
        {
            // no need to lock the object, we are in synchronize mode (access from the same thread)
            unityThreadData.HMDToWorld = t.localToWorldMatrix;
            unityThreadData.HMDToLocal = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
            unityThreadData.markKeyDown = markKeyDown;
            unityThreadData.gazedObject = gazedObjectName;
            unityThreadData.HMDPosition = t.localPosition;
            unityThreadData.HMDOrientation = t.localRotation;
        }
    }

    private void RecordDatum(bool immediate)
    {
        // If recording is stopped (which is it by default), loop back around next frame.
        if (!shouldRecord)
            return;

        if (!immediate) // we run in the same thread as unity we can update the transformations in a synchronized way
            UpdateFoveInterfaceMatrices(false);

        bool frameMarked;
        Result<string> gazedObjectName;
        Matrix4x4 transformMat;
        Vector3 hmdPosition;
        Quaternion hmdOrientation;
        lock (unityThreadData)
        {
            switch (gazeCoordinateSpace)
            {
                case CoordinateSpace.World:
                    transformMat = unityThreadData.HMDToWorld;
                    break;
                case CoordinateSpace.Local:
                    transformMat = unityThreadData.HMDToLocal;
                    break;
                default:
                    transformMat = Matrix4x4.identity;
                    break;
            }
            frameMarked = unityThreadData.markKeyDown;
            gazedObjectName = unityThreadData.gazedObject;
            hmdPosition = unityThreadData.HMDPosition;
            hmdOrientation = unityThreadData.HMDOrientation;
        }

        var eyeOffsets = FoveManager.GetEyeOffsets();
        var eyeVectorL = FoveManager.GetHmdGazeVector(Eye.Left);
        var eyeVectorR = FoveManager.GetHmdGazeVector(Eye.Right);

        Stereo<Result<Ray>> eyeRays;
        eyeRays.left = Utils.CalculateWorldGazeVector(ref transformMat, ref eyeOffsets.value.left, ref eyeVectorL);
        eyeRays.right = Utils.CalculateWorldGazeVector(ref transformMat, ref eyeOffsets.value.right, ref eyeVectorR);

        var gazeDepth = FoveManager.GetCombinedGazeDepth();
        var combinedRay = FoveManager.GetHmdCombinedGazeRay();
        combinedRay.value.origin = transformMat.MultiplyPoint(combinedRay.value.origin);
        combinedRay.value.direction = transformMat.MultiplyVector(combinedRay.value.direction).normalized;

        var pupilRadiusLeft = FoveManager.GetPupilRadius(Eye.Left);
        var pupilRadiusRight = FoveManager.GetPupilRadius(Eye.Right);

        var eyeStateL = FoveManager.GetEyeState(Eye.Left);
        var eyeStateR = FoveManager.GetEyeState(Eye.Right);

        var eyeTorsionL = FoveManager.GetEyeTorsion(Eye.Left);
        var eyeTorsionR = FoveManager.GetEyeTorsion(Eye.Right);

        // If you add new fields, be sure to write them here.
        var datum = new Datum
        {
            AppTime = (float)stopwatch.Elapsed.TotalSeconds,
            UserMark = frameMarked,
            GazedObjectName = gazedObjectName,
            HeadsetPosition = new Result<Vector3>(hmdPosition),
            HeadsetOrientation = new Result<Quaternion>(hmdOrientation),
            CombinedRay = combinedRay,
            GazeDepth = gazeDepth,
            EyeRays = eyeRays,
            EyesState = new Stereo<Result<EyeState>>(eyeStateL, eyeStateR),
            PupilsRadius = new Stereo<Result<float>>(pupilRadiusLeft, pupilRadiusRight),
            EyeTorsions = new Stereo<Result<float>>(eyeTorsionL, eyeTorsionR),
            UserPresence = FoveManager.IsUserPresent(),
            UserAttentionShift = FoveManager.IsUserShiftingAttention(),
            IPD = FoveManager.GetUserIPD(),
            IOD = FoveManager.GetUserIOD(),
            EyeballRadius = new Stereo<Result<float>>(FoveManager.GetEyeballRadius(Eye.Left), FoveManager.GetEyeballRadius(Eye.Right)),
            ScreenGaze = new Stereo<Result<Vector2>>(FoveManager.GetGazeScreenPosition(Eye.Left), FoveManager.GetGazeScreenPosition(Eye.Right)),
            EyeShape = new Stereo<Result<Fove.Unity.EyeShape>>(FoveManager.GetEyeShape(Eye.Left), FoveManager.GetEyeShape(Eye.Right)),
            PupilShape = new Stereo<Result<Fove.Unity.PupilShape>>(FoveManager.GetPupilShape(Eye.Left), FoveManager.GetPupilShape(Eye.Right)),
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
            UpdateFoveInterfaceMatrices(true);
            yield return nextFrameAwaiter;
        }
    }

    // This is the collecting thread that collect and store data asynchronously from the rendering
    private void CollectThreadFunc()
    {
        while (collectThreadShouldLive)
        {
            RecordDatum(true);

            var result = FoveManager.Headset.WaitAndFetchNextEyeTrackingData();
            if (result.Failed)
                Debug.LogError("An error happened while waiting for next eye frame. Error code:" + result.error);
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
            Debug.LogWarning("Exception writing to data file:\n" + e);
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
        private const string AppTimeHeader = "Application Time";
        private const string UserMarkHeader = "User Mark";
        private const string GazeObjectHeader = "Gazed Object";
        private const string HeadsetPositionHeader = "Headset Position";
        private const string HeadsetOrientationHeader = "Headset Orientation Quaternion";
        private const string CombinedRayHeader = "Combined Gaze Ray";
        private const string GazeDepthHeader = "Gaze Depth";
        private const string EyeRayHeader = "Eye Ray";
        private const string EyeStateHeader = "Eye State";
        private const string PupilRadiusHeader = "Pupil Radius (millimeters)";
        private const string EyeTorsionHeader = "Eye Torsion (degrees)";
        private const string UserPresenceHeader = "User Presence";
        private const string UserAttentionShiftHeader = "User Attention Shift";
        private const string IPDHeader = "IPD (millimeters)";
        private const string IODHeader = "IOD (millimeters)";
        private const string EyeballRadiusHeader = "Eyeball Radius (millimiters)";
        private const string ScreenGazeHeader = "Screen Gaze";
        private const string EyeShapeHeader = "Eye Shape";
        private const string PupilShapeHeader = "Pupil Shape";

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
                appendValue(builder, AppTimeHeader);

            if (export.UserMark)
                appendValue(builder, UserMarkHeader);

            if (export.HeadsetPosition)
                appendVector3(builder, HeadsetPositionHeader);

            if (export.HeadsetOrientation)
                appendQuaternion(builder, HeadsetOrientationHeader);

            if (export.CombinedRay)
                appendRay(builder, CombinedRayHeader);

            if (export.GazeDepth)
                append(builder, GazeDepthHeader);

            if (export.EyeRays)
            {
                appendRay(builder, EyeRayHeader + " left");
                appendRay(builder, EyeRayHeader + " right");
            }

            if (export.EyesState)
                appendLeftRight(builder, EyeStateHeader);

            if (export.PupilsRadius)
                appendLeftRight(builder, PupilRadiusHeader);

            if (export.GazedObject)
                append(builder, GazeObjectHeader);

            if (export.EyeTorsion)
                appendLeftRight(builder, EyeTorsionHeader);

            if (export.UserPresence)
                append(builder, UserPresenceHeader);

            if (export.UserAttentionShift)
                append(builder, UserAttentionShiftHeader);

            if (export.IPD)
                append(builder, IPDHeader);

            if (export.IOD)
                append(builder, IODHeader);

            if (export.EyeballRadius)
                appendLeftRight(builder, EyeballRadiusHeader);

            if (export.ScreenGaze)
            {
                appendVector2(builder, ScreenGazeHeader + " left");
                appendVector2(builder, ScreenGazeHeader + " right");
            }

            if (export.EyeShape)
            {
                for (int eye = 0; eye < 2; eye++)
                {
                    var h = EyeShapeHeader + (eye == 0 ? " left" : " right");
                    for (int i = 0; i < Fove.Unity.EyeShape.OutlinePointCount; i++)
                        appendValueXY(builder, h + " point " + i);
                    appendValue(builder, h + " error");
                }
            }

            if (export.PupilShape)
            {
                for (int eye = 0; eye < 2; eye++)
                {
                    var h = PupilShapeHeader + (eye == 0 ? " left" : " right");
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
        private string torsionFormat;
        private string vectorFormat;
        private string eyeSizeFormat;
        private string eyePixelFormat;

        public AggregatedDataSerializer(ExportSettings export)
        {
            this.export = export;

            // Setup the significant digits argument strings used when serializing numbers to text for the CSV
            torsionFormat = "{0:F3}";
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
                    Append(builder, torsionFormat, datum.EyeTorsions.left);
                    Append(builder, torsionFormat, datum.EyeTorsions.right);
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
                        AppendValue(builder, eyePixelFormat, pupilShape.value.angle);
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

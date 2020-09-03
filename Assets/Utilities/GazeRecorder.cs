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
using GazeConvergenceData = Fove.Unity.GazeConvergenceData;

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

        [Tooltip("The two eyes convergence info")]
        public bool GazeConvergence = true;

        [Tooltip("The gaze ray for each eye separately")]
        public bool EyeRays = true;

        [Tooltip("The open or closed status of the eyes")]
        public bool EyesClosed = true;

        [Tooltip("The current radius of the pupil")]
        public bool PupilsRadius = true;

        [Tooltip("The Unity object gazed by the user")]
        public bool GazedObject = true;
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

    [Tooltip("Specify the coordinate space used for gaze convergence and eye vector rays.")]
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
        public string GazeObjectName;
        public GazeConvergenceData GazeConvergence;
        public Stereo<Ray> EyeRays;
        public Stereo<bool> EyesClosed;
        public Stereo<float> PupilsRadius;
    }

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

    private HeadsetResearch researchHeadset;

    // Fove interface transformation matrices
    private class UnityThreadData
    {
        public Matrix4x4 HMDToLocal;
        public Matrix4x4 HMDToWorld;
        public string gazedObject = "";
        public bool markKeyDown;
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

        FoveManager.RegisterCapabilities(ClientCapabilities.Gaze); // needed to wait on eye frames
        researchHeadset = FoveManager.Headset.GetResearchHeadset(ResearchCapabilities.None);

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
        var gazedObject = FoveManager.GetGazedObject(true).value;
        var gazedObjectName = gazedObject ? gazedObject.name : "";

        if (immediate)
        {
            // In the case of 120 FPS recording rate, we re-fetch the HMD latest pose
            // and locally recalculate the fove interface local transform
            var pose = FoveManager.GetHMDPose(true).value;
            var isStanding = fove.poseType == FoveInterface.PlayerPose.Standing;
            var hmdAdjustedPosition = (isStanding ? pose.standingPosition : pose.position).ToVector3();
            var localPos = fove.fetchPosition? hmdAdjustedPosition : t.position;
            var localRot = fove.fetchOrientation? pose.orientation.ToQuaternion() : t.rotation;

            var parentTransfo = t.parent != null ? t.parent.localToWorldMatrix : Matrix4x4.identity;
            var localTransfo = Matrix4x4.TRS(localPos, localRot, t.localScale);

            lock (unityThreadData)
            {
                unityThreadData.HMDToWorld = parentTransfo * localTransfo;
                unityThreadData.HMDToLocal = localTransfo;
                unityThreadData.markKeyDown = markKeyDown;
                unityThreadData.gazedObject = gazedObjectName;
            }
        }
        else
        {
            // no need to lock the object, we are in synchronize mode (access from the same thread)
            unityThreadData.HMDToWorld = t.localToWorldMatrix;
            unityThreadData.HMDToLocal = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
            unityThreadData.markKeyDown = markKeyDown;
            unityThreadData.gazedObject = gazedObjectName;
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
        string gazedObjectName;
        Matrix4x4 transformMat;
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
        }

        var eyeOffsets = FoveManager.GetEyeOffsets(immediate);
        var eyeVectors = FoveManager.GetEyeVectors(immediate);

        Stereo<Ray> eyeRays;
        Utils.CalculateGazeRays(ref transformMat, ref eyeOffsets.value, ref eyeVectors.value, out eyeRays);

        var convergenceRay = FoveManager.GetHMDGazeConvergence().value;
        convergenceRay.ray.origin = transformMat.MultiplyPoint(convergenceRay.ray.origin);
        convergenceRay.ray.direction = transformMat.MultiplyVector(convergenceRay.ray.direction).normalized;

        ResearchGaze researchGaze;
        researchHeadset.GetGaze(out researchGaze);
        var pupilRadiusLeft = researchGaze.eyeDataLeft.pupilRadius;
        var pupilRadiusRight = researchGaze.eyeDataRight.pupilRadius;

        var eyeClosed = FoveManager.CheckEyesClosed(immediate).value;
        var eyeClosedLeft = (eyeClosed & Eye.Left) != 0;
        var eyeClosedRight = (eyeClosed & Eye.Right) != 0;

        // If you add new fields, be sure to write them here.
        var datum = new Datum
        {
            AppTime = (float)stopwatch.Elapsed.TotalSeconds,
            UserMark = frameMarked,
            GazeObjectName = gazedObjectName,
            GazeConvergence = convergenceRay,
            EyeRays = eyeRays,
            EyesClosed = new Stereo<bool>(eyeClosedLeft, eyeClosedRight),
            PupilsRadius = new Stereo<float>(pupilRadiusLeft, pupilRadiusRight),
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

            var error = FoveManager.Headset.WaitForNextEyeFrame();
            if (error != ErrorCode.None)
                Debug.LogError("An error happened while waiting for next eye frame. Error code:" + error);
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
        private const string GazeConvergenceHeader = "Gaze Convergence";
        private const string EyeRayHeader = "Eye ray";
        private const string EyeClosedHeader = "Eye closed";
        private const string PupilRadiusHeader = "Pupil radius";

        private readonly ExportSettings export;

        public DataHeaderSerializer(ExportSettings export)
        {
            this.export = export;
        }

        public void Append(StringBuilder builder)
        {
            Action<StringBuilder, string> append = (b, h) =>
            {
                b.Append(h);
                b.Append(',');
            };
            Action<StringBuilder, string> appendLeftRight = (b, h) =>
            {
                b.Append(h);
                b.Append(" left,");
                b.Append(h);
                b.Append(" right,");
            };

            Action<StringBuilder, string> appendXY = (b, h) =>
            {
                b.Append(h);
                b.Append(" x,");
                b.Append(h);
                b.Append(" y,");
            };

            Action<StringBuilder, string> appendXYZ = (b, h) =>
            {
                appendXY(b, h);
                b.Append(h);
                b.Append(" z,");
            };

            Action<StringBuilder, string> appendRay = (b, h) =>
            {
                appendXYZ(b, h + " pos");
                appendXYZ(b, h + " dir");
            };

            // Append the full data header to the builder

            builder.Append(','); // keep the first column for the input file

            if (export.ApplicationTime)
                append(builder, AppTimeHeader);

            if (export.UserMark)
                append(builder, UserMarkHeader);

            if (export.GazeConvergence)
            {
                appendRay(builder, GazeConvergenceHeader + " ray");
                append(builder, GazeConvergenceHeader + " distance");
            }

            if (export.EyeRays)
            {
                appendRay(builder, EyeRayHeader + " left");
                appendRay(builder, EyeRayHeader + " right");
            }

            if (export.EyesClosed)
                appendLeftRight(builder, EyeClosedHeader);

            if (export.PupilsRadius)
                appendLeftRight(builder, PupilRadiusHeader);

            if (export.GazedObject)
                append(builder, GazeObjectHeader);

            builder.Remove(builder.Length - 1, 1); // remove the last "," of the line
            builder.AppendLine();
        }
    }

    class AggregatedDataSerializer : IDataWriter
    {
        private readonly ExportSettings export;

        public AggregatedData Data { get; set; }

        private string timeFormat;
        private string vectorFormat;
        private string eyeSizeFormat;

        public AggregatedDataSerializer(ExportSettings export)
        {
            this.export = export;

            // Setup the significant digits argument strings used when serializing numbers to text for the CSV
            vectorFormat = "{0:F3},";
            timeFormat = "{0:F4},";
            eyeSizeFormat = "{0:F5},";
        }

        private void Append(StringBuilder builder, Vector3 vector)
        {
            builder.AppendFormat(vectorFormat, vector.x);
            builder.AppendFormat(vectorFormat, vector.y);
            builder.AppendFormat(vectorFormat, vector.z);
        }

        private void Append(StringBuilder builder, Ray ray)
        {
            Append(builder, ray.origin);
            Append(builder, ray.direction);
        }

        public void Append(StringBuilder builder)
        {
            Debug.Log("Writing " + Data.Count + " lines");

            foreach (var datum in Data)
            {
                builder.Append(',');

                // This writes each element in the data list as a CSV-formatted line.
                if (export.ApplicationTime)
                    builder.AppendFormat(timeFormat, datum.AppTime);

                if (export.UserMark)
                {
                    builder.Append(datum.UserMark ? 'X' : ' ');
                    builder.Append(',');
                }

                if (export.GazeConvergence)
                {
                    Append(builder, datum.GazeConvergence.ray);
                    builder.AppendFormat(vectorFormat, datum.GazeConvergence.distance);
                }

                if (export.EyeRays)
                {
                    Append(builder, datum.EyeRays.left);
                    Append(builder, datum.EyeRays.right);
                }

                if (export.EyesClosed)
                {
                    builder.AppendFormat("{0},", datum.EyesClosed.left);
                    builder.AppendFormat("{0},", datum.EyesClosed.right);
                }

                if (export.PupilsRadius)
                {
                    builder.AppendFormat(eyeSizeFormat, datum.PupilsRadius.left);
                    builder.AppendFormat(eyeSizeFormat, datum.PupilsRadius.right);
                }

                if (export.GazedObject)
                {
                    builder.Append(datum.GazeObjectName);
                    builder.Append(',');
                }

                if (builder.Length > 2) // remove the last "," of the line
                    builder.Remove(builder.Length - 1, 1);

                builder.AppendLine();
            }
        }
    }
}

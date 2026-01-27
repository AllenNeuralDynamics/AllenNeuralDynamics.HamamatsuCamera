using AllenNeuralDynamics.HamamatsuCamera.API;
using AllenNeuralDynamics.HamamatsuCamera.Exceptions;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using Bonsai;
using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using System.Xml;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    internal sealed class CameraCapture : IDisposable
    {
        private byte[] _mono8LookupTable;
        private ushort[] _mono16LookupTable;
        private const double _targetBundleRate = 10;
        private const double _targetConfigModeRate = 40;    // 40Hz
        private const int _channelCapacity = 4096;
        private readonly C13440 _instance;
        private List<RegionOfInterest> _regionsOfInterest;
        private Channel<AcquisitionPacket> _acqToProc;     // Single-writer, single-reader
        private Channel<FramePacket> _procToWrite;     // Single-writer, single-reader
        private Channel<IFrameContainer> _procToRx;
        private Subject<IFrameContainer> _subject;
        private CancellationTokenSource _cts;
        private IObserver<IFrameContainer> _observer;
        private DCAM_MANAGER _dcamManager;
        private UnmanagedFramePool _framePool;            // Preallocated unmanaged buffers
        private Thread _acquisitionThread;
        private Thread _processingThread;
        private Thread _rxThread;
        private Thread _writingThread;
        private double _internalFrameRate;
        private int _xOffset;
        private int _yOffset;
        private int _width;
        private int _height;
        private int _rowBytes;
        private int _bytesPerFrame;
        private int _bufferCount;
        private bool disposedValue;
        private bool isInitialized;
        private bool _isPaused;
        private volatile bool _isMono16;
        private volatile bool _acquiring = true;
        private CountdownEvent _pausedWorkers;
        private CountdownEvent _resumedWorkers;
        private int _numWorkers;
        private readonly ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _resumeEvent = new ManualResetEventSlim(false);

        private byte _processingDeinterleaveCount = 1;
        private bool _includeCsvWriter;
        private CsvWriterProperties _csvWriterProperties = new CsvWriterProperties();
        private bool _includeTiffWriter;
        private TiffWriterProperties _tiffWriterProperties = new TiffWriterProperties();



        public event EventHandler AcquisitionStarted;
        public event EventHandler BufferReleased;

        internal bool Acquiring
        {
            get { return _acquiring; }
            set { _acquiring = value; }
        }

        internal bool IsConfigMode { get; set; }
        public bool IsMono16 { get; set; }

        /// <summary>
        /// Stores the <see cref="C13440"/> instance and initializes the lookup tables.
        /// </summary>
        /// <param name="instance"><see cref="C13440"/> instance</param>
        internal CameraCapture(C13440 instance)
        {
            _instance = instance;

            _mono8LookupTable = new byte[byte.MaxValue + 1];
            _mono16LookupTable = new ushort[ushort.MaxValue + 1];
            for (var i = 0; i <= byte.MaxValue; i++)
                _mono8LookupTable[i] = (byte)i;
            for (var i = 0; i <= ushort.MaxValue; i++)
                _mono16LookupTable[i] = (ushort)i;
        }

        /// <summary>
        /// Tries to load the camera and LUT settings
        /// </summary>
        private void TryLoadSettings()
        {
            try
            {
                LoadCameraSettings();
                LoadLUT();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Check if camera settings file exists. If so, use an <see cref="XmlReader"/>
        /// to read in the camera settings, regions of interest, and crop mode, storing them
        /// in the <see cref="C13440"/> instance.
        /// </summary>
        private void LoadCameraSettings()
        {
            if (!IsRealFilePath(_instance.SettingsPath))
                return;

            var settings = new Dictionary<int, double>();
            var fullPath = Path.GetFullPath(_instance.SettingsPath);
            using (var reader = XmlReader.Create(fullPath))
            {
                _instance.Regions = new List<RegionOfInterest>();
                while (reader.Read())
                {
                    if (reader.Name == "Setting" && reader.HasAttributes && reader.AttributeCount == 2)
                    {
                        settings[int.Parse(reader[0])] = double.Parse(reader[1]);
                        reader.MoveToElement();
                    }
                    if (reader.Name == "Region" && reader.HasAttributes && reader.AttributeCount == 4)
                    {
                        _instance.Regions.Add(new RegionOfInterest(int.Parse(reader[0]), int.Parse(reader[1]), int.Parse(reader[2]), int.Parse(reader[3])));
                        reader.MoveToElement();
                    }
                    if (reader.Name == "CropMode" && reader.HasAttributes && reader.AttributeCount == 1)
                    {
                        bool auto = reader[0].Equals("Auto");
                        _instance.CropMode = auto ? CropMode.Auto : CropMode.Manual;
                    }
                }
            }
            var subarraySettings = settings.Where(pair => GetIsSubarray(pair.Key));
            var otherSettings = settings.Where(pair => !GetIsSubarray(pair.Key));
            foreach (var pair in subarraySettings)
                _instance.CameraProps.First(prop => prop.m_idProp == pair.Key).setvalue(pair.Value);
            foreach (var pair in otherSettings)
                _instance.CameraProps.First(prop => prop.m_idProp == pair.Key).setvalue(pair.Value);

        }

        /// <summary>
        /// Check if the LUT settings file exists. If so, use a <see cref="StreamReader"/>
        /// to store the points of interest in the <see cref="C13440"/> instance.
        /// From the points of interest populate the instance's LookupTable and update the
        /// lookup tables used in this class.
        /// </summary>
        private void LoadLUT()
        {
            if (!IsRealFilePath(_instance.LookupTablePath))
                return;

            var fullPath = Path.GetFullPath(_instance.LookupTablePath);
            using (var reader = new StreamReader(fullPath))
            {
                string line;
                var numCols = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    var values = line.Split(',');
                    if (numCols == 0)
                        numCols = values.Length;
                    if (numCols == 2)
                    {
                        var keySuccess = ushort.TryParse(values[0], out ushort key);
                        var valueSuccess =  ushort.TryParse(values[1], out ushort value);
                        if (keySuccess && valueSuccess)
                            _instance.PointsOfInterest[key] = value;
                    }
                    else if (numCols == 3)
                    {
                        var flagSuccess = ushort.TryParse(values[0], out ushort pointOfInterestFlag);
                        var isPointOfInterest = pointOfInterestFlag == 1;
                        var keySuccess = ushort.TryParse(values[1], out ushort key);
                        var valueSuccess = ushort.TryParse(values[2], out ushort value);
                        if (flagSuccess && isPointOfInterest && keySuccess && valueSuccess)
                            _instance.PointsOfInterest[key] = value;
                    }
                }
            }
            var pairs = _instance.PointsOfInterest.OrderBy(pair => pair.Key);
            for (int i = 0; i < pairs.Count() - 1; i++)
            {
                var former = pairs.ElementAt(i);
                var latter = pairs.ElementAt(i + 1);
                var dx = latter.Key - former.Key;
                var dy = latter.Value - former.Value;
                var m = (double)dy / (double)dx;
                var b = former.Value - m * former.Key;

                for (var j = (int)former.Key; j <= latter.Key; j++)
                {
                    var value = (ushort)Math.Min(ushort.MaxValue, Math.Max(0, Math.Round(m * j + b)));
                    _instance.LookupTable.Mono16[j] = value;
                }
            }
            var scale = ushort.MaxValue / byte.MaxValue;
            for (var i = 0; i <= byte.MaxValue; i++)
                _instance.LookupTable.Mono8[i] = (byte)(_instance.LookupTable.Mono16[i * scale] >> 8);

            UpdateLookupTable();
        }

        /// <summary>
        /// Checks if the file exists
        /// </summary>
        /// <param name="path">File full absolute path.</param>
        /// <returns>True if file exists</returns>
        private static bool IsRealFilePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
                return false;

            try
            {
                var full = Path.GetFullPath(path);
                var root = Path.GetPathRoot(full);
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                    return false;

                return File.Exists(full);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates an <see cref="IObservable{IFrameContainer}"/> that can be used by the <see cref="C13440"/> node.
        /// Starts the acquisition pipeline on subscription and stops it on disposal.
        /// </summary>
        /// <returns><see cref="IObservable{IFrameContainer}"/> that can be used by the <see cref="C13440"/> node.</returns>
        internal IObservable<IFrameContainer> Generate()
        {
            return Observable.Create<IFrameContainer>(observer =>
            {
                TryStartAcquisitionPipeline(observer);
                var sub = _subject.ObserveOn(TaskPoolScheduler.Default).Subscribe(observer);
                return Disposable.Create(() =>
                {
                    sub.Dispose();
                    StopAcquisitionPipeline();
                });
            });
        }

        /// <summary>
        /// Pauses all of the worker threads. Stop capture and releases the buffer.
        /// </summary>
        internal void PauseAndRelease()
        {
            if (_isPaused)
                return;
            _pausedWorkers = new CountdownEvent(_numWorkers);
            _pauseEvent.Set();
            _pausedWorkers.Wait();
            StopCapture();
            ReleaseBuffer();
            _isPaused = true;
        }

        /// <summary>
        /// Configures the camera properties, reallocates the buffer,
        /// clears channels between threads, disposes and reallocates the frame pool,
        /// starts capture, and resumes the worker threads.
        /// </summary>
        internal void ReallocateAndResume()
        {
            if (!_isPaused)
                return;
            ConfigProps();
            AllocateBuffer();
            ClearChannel(_acqToProc);
            _framePool.Dispose();
            _framePool = new UnmanagedFramePool(_bytesPerFrame, _bufferCount, true);
            StartCapture();
            _resumedWorkers = new CountdownEvent(_numWorkers);
            _pauseEvent.Reset();
            _pausedWorkers.Reset();
            _resumeEvent.Set();
            _resumedWorkers.Wait();
            _resumeEvent.Reset();
            _resumedWorkers.Reset();
            _isPaused = false;
        }

        /// <summary>
        /// Clears a channel between two thread. Used to return DataPtr back to the frame pool before disposing it.
        /// </summary>
        /// <param name="channel">Channel to clear.</param>
        private void ClearChannel(Channel<AcquisitionPacket> channel)
        {
            while (channel.Reader.TryRead(out var packet))
            {
                _framePool.Return(packet.DataPtr);
            }
        }

        /// <summary>
        /// Gets the status of the camera.
        /// </summary>
        /// <returns>The status of the camera.</returns>
        internal DCAMCAP_STATUS GetStatus()
        {
            return _dcamManager.cap_status();
        }

        private void TryStartAcquisitionPipeline(IObserver<IFrameContainer> observer)
        {
            try
            {
                StartAcquisitionPipeline(observer);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError(ex);
            }
        }

        /// <summary>
        /// Starts the acquisition pipeline.
        /// </summary>
        /// <param name="observer"></param>
        private void StartAcquisitionPipeline(IObserver<IFrameContainer> observer)
        {
            disposedValue = false;
            _isPaused = false;
            _observer = observer;
            _cts = new CancellationTokenSource();
            _subject = new Subject<IFrameContainer>();
            _acqToProc = Channel.CreateBounded<AcquisitionPacket>(
                new BoundedChannelOptions(_channelCapacity)
                {
                    SingleWriter = true,
                    SingleReader = true,
                    FullMode = BoundedChannelFullMode.DropOldest    // Prevent Blocking
                });
            _procToRx = Channel.CreateBounded<IFrameContainer>(
                new BoundedChannelOptions(_channelCapacity)
                {
                    SingleWriter = true,
                    SingleReader = true,
                    FullMode = BoundedChannelFullMode.DropOldest    // Prevent Blocking
                });
            ReadConfiguration();
            InitAndOpen();
            ConfigProps();
            CheckRegions();
            AllocateBuffer();

            _framePool = new UnmanagedFramePool(_bytesPerFrame, _bufferCount, true);

            _numWorkers = 2;
            _acquisitionThread = new Thread(() => AcquisitionLoop(_cts.Token))
            {
                IsBackground = true,
                Name = "Acquisition",
                Priority = ThreadPriority.Highest
            };
            _processingThread = new Thread(() => ProcessingLoop(_cts.Token))
            {
                IsBackground = true,
                Name = "Processing",
                Priority = ThreadPriority.AboveNormal
            };
            _rxThread = new Thread(() => RxLoop(_cts.Token))
            {
                IsBackground = true,
                Name = "Rx",
                Priority = ThreadPriority.AboveNormal
            };
            _acquisitionThread.Start();
            _processingThread.Start();
            _rxThread.Start();

            if (_includeCsvWriter || _includeTiffWriter)
            {
                _numWorkers++;
                _procToWrite = Channel.CreateBounded<FramePacket>(
                    new BoundedChannelOptions(_channelCapacity)
                    {
                        SingleWriter = true,
                        SingleReader = true,
                        FullMode = BoundedChannelFullMode.DropOldest
                    });
                _writingThread = new Thread(() => WritingLoop(_cts.Token))
                {
                    IsBackground = true,
                    Name = "Writing",
                    Priority = ThreadPriority.AboveNormal
                };
                _writingThread.Start();
            }
        }

        /// <summary>
        /// Looks for <see cref="EventWaitHandle"/> and <see cref="Mutex"/> generated by the <see cref="Processing"/>,
        /// <see cref="CsvWriter"/>, and <see cref="TiffWriter"/> nodes. If they are present then reading the properties from each of those nodes.
        /// </summary>
        private void ReadConfiguration()
        {
            var processingResult = EventWaitHandle.TryOpenExisting($"{nameof(Processing)}_Event", out var processingHandle);
            var csvResult = EventWaitHandle.TryOpenExisting($"{nameof(CsvWriter)}_Event", out var csvHandle);
            var tiffResult = EventWaitHandle.TryOpenExisting($"{nameof(TiffWriter)}_Event", out var tiffHandle);

            if (processingResult)
            {
                processingHandle.WaitOne();
                var mutexResult = Mutex.TryOpenExisting($"{nameof(Processing)}_Shared", out var mutex);
                if (mutexResult)
                {
                    mutex.WaitOne();
                    _processingDeinterleaveCount = ProcessingShared.DeinterleaveCount;
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
                else
                    _processingDeinterleaveCount = 1;
            }
            else
                _processingDeinterleaveCount = 1;

            if (csvResult)
            {
                csvHandle.WaitOne();
                var mutexResult = Mutex.TryOpenExisting($"{nameof(CsvWriter)}_Shared", out var mutex);
                if (mutexResult)
                {
                    mutex.WaitOne();
                    _includeCsvWriter = true;
                    _csvWriterProperties = CsvWriterShared.Properties;
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
                else
                {
                    _includeCsvWriter = false;
                    _csvWriterProperties.Clear();
                }
            }
            else
            {
                _includeCsvWriter = false;
                _csvWriterProperties.Clear();
            }

            if (tiffResult)
            {
                tiffHandle.WaitOne();
                var mutexResult = Mutex.TryOpenExisting($"{nameof(TiffWriter)}_Shared", out var mutex);
                if (mutexResult)
                {
                    mutex.WaitOne();
                    _includeTiffWriter = true;
                    _tiffWriterProperties = TiffWriterShared.Properties;
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
                else
                {
                    _includeTiffWriter = false;
                    _tiffWriterProperties.Clear();
                }
            }
            else
            {
                _includeTiffWriter = false;
                _tiffWriterProperties.Clear();
            }
        }

        /// <summary>
        /// Stop acquisition pipeline
        /// </summary>
        private void StopAcquisitionPipeline()
        {
            _observer?.OnCompleted();
            if (!_isPaused)
                StopCapture();
            Dispose();
        }

        /// <summary>
        /// Initialize the <see cref="DCAM_API_MANAGER"/> and open the camera. Also initialize the cameras props and optionally
        /// load settings from disk.
        /// </summary>
        internal void InitAndOpen()
        {
            if (isInitialized)
                return;
            if (!DCAM_API_MANAGER.init())
            {
                DCAM_API_MANAGER.uninit();
                throw new ApiInitializationException();
            }

            _dcamManager = new DCAM_MANAGER();
            if (!_dcamManager.dev_open(0))
            {
                DCAM_API_MANAGER.uninit();
                throw new OpenCameraException();
            }

            InitProps();
            TryLoadSettings();
            //if (_instance.CameraProps == null && !string.IsNullOrEmpty(_instance.SettingsPath))
            //{
            //    InitProps();
            //    TryLoadSettings();
            //}
            //else
            //    InitProps();

            isInitialized = true;
        }

        /// <summary>
        /// Close the camera and deinitialize the <see cref="DCAM_API_MANAGER"/>.
        /// </summary>
        private void CloseAndDeinit()
        {
            if (!_dcamManager.dev_close())
                throw new OpenCameraException("Failed to close camera");
            if (!DCAM_API_MANAGER.uninit())
                throw new ApiInitializationException("Failed to uninitialize DCAM API.");
            isInitialized = false;
        }

        /// <summary>
        /// Configure the pixel type and image data properties.
        /// </summary>
        private void ConfigProps()
        {
            ConfigRegions();
            GetSetPixelType();
            GetImageData();
        }

        /// <summary>
        /// Get a <see cref="DCAM_PROP_MANAGER"/> for each camera setting storing them in the <see cref="C13440"/> instance.
        /// If there are stored settings, then update the camera props with these stored settings.
        /// </summary>
        private void InitProps()
        {
            var propManagers = new List<DCAM_PROP_MANAGER>();
            var currentPropManager = new DCAM_PROP_MANAGER(_dcamManager, DCAMIDPROP.ZERO);
            while (currentPropManager.nextid())
            {
                currentPropManager.update_attr();
                propManagers.Add(currentPropManager.Clone());
            }
            _instance.CameraProps = propManagers.OrderBy(prop => prop.m_attr.iGroup);
            _instance.Regions = new List<RegionOfInterest>();
            _instance.LookupTable = new LookupTable();
            _instance.PointsOfInterest = new Dictionary<ushort, ushort>()
            {
                [0] = 0,
                [ushort.MaxValue] = ushort.MaxValue
            };
            //if (_instance.StoredSettings != null)
            //{
            //    var subarrayProps = _instance.CameraProps.Where(prop => GetIsSubarrayProp(prop.m_idProp));
            //    foreach (var prop in subarrayProps)
            //    {
            //        prop.update_attr();
            //        var isReadonly = prop.is_attr_readonly();
            //        var isPropertyStored = _instance.StoredSettings.TryGetValue(prop.m_idProp.getidprop(), out var storedValue);
            //        if (!isReadonly && isPropertyStored)
            //            prop.setvalue(storedValue);
            //    }

            //    var otherProps = _instance.CameraProps.Where(prop => !GetIsSubarrayProp(prop.m_idProp));
            //    foreach (var prop in otherProps)
            //    {
            //        prop.update_attr();
            //        var isReadonly = prop.is_attr_readonly();
            //        var isPropertyStored = _instance.StoredSettings.TryGetValue(prop.m_idProp.getidprop(), out var storedValue);
            //        if (!isReadonly && isPropertyStored)
            //            prop.setvalue(storedValue);
            //    }

            //    UpdateLookupTable();
            //}
        }

        /// <summary>
        /// Check if a property id (<see cref="int"/> representation) is related to the camera's subarray
        /// </summary>
        /// <param name="propId">Integer representation of the property id</param>
        /// <returns>True if the property id is related to the subarray</returns>
        private static bool GetIsSubarray(int propId)
        {
            return propId == DCAMIDPROP.SUBARRAYHPOS || propId == DCAMIDPROP.SUBARRAYHSIZE || propId == DCAMIDPROP.SUBARRAYVPOS || propId == DCAMIDPROP.SUBARRAYVSIZE || propId == DCAMIDPROP.SUBARRAYMODE;
        }

        /// <summary>
        /// Check if a property id (<see cref="DCAMIDPROP"/> representation) is related to the camera's subarray
        /// </summary>
        /// <param name="propId">DCAMIDPROP representation of the property id</param>
        /// <returns>True if the property id is related to the subarray</returns>
        private static bool GetIsSubarrayProp(DCAMIDPROP propId)
        {
            return propId == DCAMIDPROP.SUBARRAYHPOS || propId == DCAMIDPROP.SUBARRAYHSIZE || propId == DCAMIDPROP.SUBARRAYVPOS || propId == DCAMIDPROP.SUBARRAYVSIZE || propId == DCAMIDPROP.SUBARRAYMODE;
        }

        private void ConfigRegions()
        {
            var subarrayHPosProp = _instance.CameraProps.FirstOrDefault(prop => prop.m_idProp == DCAMIDPROP.SUBARRAYHPOS) ?? throw new PropertyException();
            var subarrayVPosProp = _instance.CameraProps.FirstOrDefault(prop => prop.m_idProp == DCAMIDPROP.SUBARRAYVPOS) ?? throw new PropertyException();
            var subarrayModeProp = _instance.CameraProps.FirstOrDefault(prop => prop.m_idProp == DCAMIDPROP.SUBARRAYMODE) ?? throw new PropertyException();
            var hPos = 0.0;
            var vPos = 0.0;
            var mode = 0.0;
            subarrayHPosProp.getvalue(ref hPos);
            subarrayVPosProp.getvalue(ref vPos);
            subarrayModeProp.getvalue(ref mode);

            if(mode == DCAMPROP.MODE.OFF)
            {
                _xOffset = 0;
                _yOffset = 0;
            }
            else
            {
                _xOffset = (int)hPos;
                _yOffset = (int)vPos;
            }
            _regionsOfInterest = new List<RegionOfInterest>(_instance.Regions);

            for (var i = 0; i < _regionsOfInterest.Count; i++)
            {
                var region = _regionsOfInterest[i];
                _regionsOfInterest[i] = new RegionOfInterest()
                {
                    X = region.X - _xOffset,
                    Y = region.Y - _yOffset,
                    Width = region.Width,
                    Height = region.Height
                };
            }
        }

        /// <summary>
        /// Get the pixel type and if it is not supported, set it to Mono16
        /// </summary>
        private void GetSetPixelType()
        {
            var pixelTypeProp = _instance.CameraProps.FirstOrDefault(prop => prop.m_idProp == DCAMIDPROP.IMAGE_PIXELTYPE) ?? throw new PropertyException();
            var pixelType = 0.0;
            pixelTypeProp.getvalue(ref pixelType);

            if ((int)pixelType == DCAM_PIXELTYPE.MONO8)
                IsMono16 = false;
            else if ((int)pixelType == DCAM_PIXELTYPE.MONO16)
                IsMono16 = true;
            else
            {
                pixelTypeProp.setvalue((uint)DCAM_PIXELTYPE.MONO16);
                IsMono16 = true;
            }
        }

        /// <summary>
        /// Store the image data setting.
        /// </summary>
        private void GetImageData()
        {
            var widthProp = _instance.CameraProps.FirstOrDefault(prop => prop.m_idProp == DCAMIDPROP.IMAGE_WIDTH);
            var heightProp = _instance.CameraProps.FirstOrDefault(prop => prop.m_idProp == DCAMIDPROP.IMAGE_HEIGHT);
            var rowBytesProp = _instance.CameraProps.FirstOrDefault(prop => prop.m_idProp == DCAMIDPROP.IMAGE_ROWBYTES);
            var internalFrameRateProp = _instance.CameraProps.FirstOrDefault(prop => prop.m_idProp == DCAMIDPROP.INTERNALFRAMERATE);

            if (widthProp == null || heightProp == null || rowBytesProp == null || internalFrameRateProp == null)
                throw new PropertyException();

            double width, height, rowBytes, internalFrameRate;
            width = height = rowBytes = internalFrameRate = 0.0;
            widthProp.getvalue(ref width);
            heightProp.getvalue(ref height);
            rowBytesProp.getvalue(ref rowBytes);
            internalFrameRateProp.getvalue(ref internalFrameRate);

            _width = (int)width;
            _height = (int)height;
            _rowBytes = (int)rowBytes;
            _bytesPerFrame = _rowBytes * _height;
            _isMono16 = _width != _rowBytes;
            _internalFrameRate = internalFrameRate;
        }

        /// <summary>
        /// Checks the legality of the regions of interest.
        /// If a region is partially outside the crop, then
        /// crop the region to be fully inside.
        /// If the region is fully outside the crop, then
        /// remove it.
        /// </summary>
        private void CheckRegions()
        {
            for (var i = _regionsOfInterest.Count - 1; i >= 0; i--)
            {
                var region = _regionsOfInterest[i];

                // Fully outside, remove
                if (region.X >= _width || region.Y >= _height)
                {
                    _regionsOfInterest.RemoveAt(i);
                    continue;
                }

                if (region.X < 0)
                    region.X = 0;
                if (region.Y < 0)
                    region.Y = 0;
                // Partially outside, clamp to crop bounds
                if (region.X + region.Width > _width)
                    region.Width = _width - region.X;

                if (region.Y + region.Height > _height)
                    region.Height = _height - region.Y;

                // Write back the modified struct
                _regionsOfInterest[i] = region;
            }
        }

        /// <summary>
        /// Allocate the buffer of the <see cref="DCAM_MANAGER"/>
        /// </summary>
        private void AllocateBuffer()
        {
            var frameRateProp = _instance.CameraProps.FirstOrDefault(prop => prop.m_idProp == DCAMIDPROP.INTERNALFRAMERATE);
            if (frameRateProp == null)
                throw new PropertyException();
            var frameRate = 0.0;
            frameRateProp.getvalue(ref frameRate);
            var targetBufferCount = Math.Ceiling(frameRate / _targetBundleRate);
            _bufferCount = (int)Math.Max(10, targetBufferCount);
            if (!_dcamManager.buf_alloc(_bufferCount))
                throw new BufferAllocationException();
        }

        /// <summary>
        /// Release the buffer of the <see cref="DCAM_MANAGER"/>
        /// </summary>
        private void ReleaseBuffer()
        {
            if (!_dcamManager.buf_release())
                throw new BufferAllocationException("Failed to release buffer.");

            BufferReleased?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Start capturing images
        /// </summary>
        private void StartCapture()
        {
            if (!_dcamManager.cap_start())
                throw new StartCaptureException();
            AcquisitionStarted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Stop capturing images
        /// </summary>
        private void StopCapture()
        {
            if (!_dcamManager.cap_stop())
                throw new StartCaptureException("Failed to stop capture.");
        }


        /// <summary>
        /// Acquisition loop on a dedicated high-priority thread. Acquires images from <see cref="DCAM_WAIT_MANAGER"/>
        /// copies the raw image data to the frame pool while passing the metadata through a <see cref="Channel{AcquisitionPacket}"/>.
        /// </summary>
        /// <param name="token">Cancellation token</param>
        private unsafe void AcquisitionLoop(CancellationToken token)
        {
            using (var waiter = new DCAM_WAIT_MANAGER(ref _dcamManager))
            {
                _dcamManager.m_capmode = DCAMCAP_START.SEQUENCE;

                // Initialize previous frame count and events variables
                DCAMWAIT eventMask = DCAMWAIT.CAPEVENT.FRAMEREADY | DCAMWAIT.CAPEVENT.STOPPED;
                DCAMWAIT eventHappened;

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        // Check for pause request
                        if (_pauseEvent.IsSet)
                        {
                            // Signal we are paused
                            _pausedWorkers.Signal();

                            // Wait until resume is signaled
                            _resumeEvent.Wait(token);
                            _resumedWorkers.Signal();
                        }
                        else if (_acquiring)
                        {
                            StartCapture();
                            var prevIndex = 0;
                            while (_acquiring && !token.IsCancellationRequested)
                            {
                                // Check for pause request
                                if (_pauseEvent.IsSet)
                                {
                                    // Signal we are paused
                                    _pausedWorkers.Signal();

                                    // Wait until resume is signaled
                                    _resumeEvent.Wait(token);
                                    _resumedWorkers.Signal();
                                    prevIndex = 0;
                                    Thread.SpinWait(10);
                                    continue;
                                }
                                eventHappened = DCAMWAIT.NONE;

                                // Wait for camera event (non-blocking in practice)
                                if (!waiter.start(eventMask, ref eventHappened))
                                    continue;

                                if ((eventHappened & DCAMWAIT.CAPEVENT.FRAMEREADY) == 0)
                                    continue;

                                // Retrieve most recent frame indices
                                int bufIndex = 0;
                                int frameId = 0;

                                if (!_dcamManager.cap_transferinfo(ref bufIndex, ref frameId))
                                    continue;

                                while (prevIndex % _bufferCount != bufIndex)
                                {
                                    var index = prevIndex % _bufferCount;
                                    var frame = new DCAMBUF_FRAME(index);
                                    if (!_dcamManager.buf_lockframe(ref frame))
                                        continue;
                                    // Rent unmanaged destination buffer
                                    if (!_framePool.TryRent(out IntPtr dstPtr))
                                        break;

                                    var srcBytes = frame.rowbytes * frame.height;

                                    // RAW pointer -> RAW pointer
                                    Buffer.MemoryCopy((void*)frame.buf, (void*)dstPtr, _framePool.FrameSize, srcBytes);

                                    // Package the frame
                                    var packet = new AcquisitionPacket(
                                        frame.framestamp,
                                        frame.timestamp,
                                        dstPtr,
                                        frame.rowbytes,
                                        frame.left,
                                        frame.top,
                                        frame.width,
                                        frame.height
                                    );

                                    // Immediate non-blocking enqueue
                                    _acqToProc.Writer.TryWrite(packet);
                                    prevIndex++;
                                }
                                prevIndex = bufIndex;
                            }
                            StopCapture();
                        }
                        else
                            Thread.SpinWait(1);
                    }

                }
                finally
                {
                    _dcamManager.cap_stop();
                }
            }
        }

        /// <summary>
        /// Processing loop on a dedicated high-priority thread.
        /// Reads packets from a <see cref="Channel{AcquisitionPacket}"/>
        /// and generates a <see cref="FramePacket"/> from it.
        /// If in configuration mode (i.e. <see cref="Calibration.CalibrationForm"/> is open)
        /// then greatly downsample, apply LUT, generate a <see cref="Frame"/> and pass it to the subject.
        /// Else if in Bundling Enabled, downsample the images while aggregating the frame data, and pass <see cref="FrameBundle"/>
        /// to the subject.
        /// Else, process the image and generate <see cref="Frame"/> to the subject.
        /// Additionally, <see cref="FramePacket"/> to the writer thread if a writer is present.
        /// </summary>
        /// <param name="token"></param>
        private void ProcessingLoop(CancellationToken token)
        {
            ushort prevRawId = 0;
            ulong overflow = 0;
            bool firstTs = true;
            double ts0 = 0.0;

            var frameDataList = new List<FrameData>();
            var imageArray = new IplImage[_processingDeinterleaveCount];
            int downsampleFactor = 1;
            if (IsConfigMode)
                downsampleFactor = Math.Max((int)(_internalFrameRate / _targetConfigModeRate), 1);
            else if (_instance.EnableBundling)
                downsampleFactor = Math.Max(_instance.BundleSize * _processingDeinterleaveCount, _processingDeinterleaveCount);
            int downsampleCounter = 0;

            while (!token.IsCancellationRequested)
            {
                // Check for pause request
                if (_pauseEvent.IsSet)
                {
                    // Signal we are paused
                    _pausedWorkers.Signal();

                    // Wait until resume is signaled
                    _resumeEvent.Wait(token);
                    _resumedWorkers.Signal();
                    prevRawId = 0;
                    overflow = 0;
                    firstTs = true;
                    ts0 = 0.0;

                    downsampleFactor = 1;
                    if (IsConfigMode)
                        downsampleFactor = Math.Max((int)(_internalFrameRate / _targetConfigModeRate), 1);
                    else if (_instance.EnableBundling)
                        downsampleFactor = Math.Max(_instance.BundleSize * _processingDeinterleaveCount, 1);
                    downsampleCounter = 0;
                    imageArray = new IplImage[_processingDeinterleaveCount];
                    frameDataList.Clear();
                    continue;
                }

                if (!_acqToProc.Reader.TryRead(out var acq))
                {
                    Thread.SpinWait(1);
                    continue;
                }

                // --- Frame ID unwrapping ---
                ushort raw = (ushort)acq.FrameId;
                if (raw < prevRawId)
                    overflow++;
                ulong frameIndex = overflow * 65536UL + raw;
                prevRawId = raw;

                // --- Timestamp conversion ---
                double ts = acq.Timestamp.sec + acq.Timestamp.microsec * 1e-6;
                if (firstTs) { ts0 = ts; firstTs = false; }
                double elapsed = ts - ts0;

                // --- Build packet ---
                var packet = new FramePacket
                {
                    DataPtr = acq.DataPtr,
                    Left = acq.Left,
                    Top = acq.Top,
                    Width = acq.Width,
                    Height = acq.Height,
                    RowBytes = acq.RowBytes,
                    FrameIndex = frameIndex,
                    DeinterleaveCount = _processingDeinterleaveCount,
                    ElapsedSeconds = elapsed,
                    CameraTimestamp = ts,
                    ComputerTimestamp = HighResolutionScheduler.Now.DateTime.TimeOfDay.TotalSeconds
                };

                if (IsConfigMode)
                {
                    downsampleCounter++;

                    if (downsampleCounter >= downsampleFactor)
                    {
                        ApplyLUT(acq.DataPtr, acq.Width, acq.Height, acq.RowBytes);
                        var frame = new Frame()
                        {
                            Image = CreateIplImageCopy(packet),
                            FrameCounter = packet.FrameIndex,
                            Timestamp = packet.ElapsedSeconds,
                            Left = packet.Left,
                            Top = packet.Top,
                            Width = packet.Width,
                            Height = packet.Height,
                            DeinterleaveCount = packet.DeinterleaveCount
                        };
                        _procToRx?.Writer.TryWrite(frame);
                        downsampleCounter = 0;
                    }
                    _framePool.Return(packet.DataPtr);
                }
                else if (_instance.EnableBundling)
                {
                    packet.RoiAverages = ProcessImage(
                        acq.DataPtr, acq.Width, acq.Height, acq.RowBytes);
                    var frameData = new FrameData()
                    {
                        FrameCounter = packet.FrameIndex,
                        Timestamp = packet.ElapsedSeconds,
                        Left = packet.Left,
                        Top = packet.Top,
                        Width = packet.Width,
                        Height = packet.Height,
                        DeinterleaveCount = packet.DeinterleaveCount,
                        RegionData = packet.RoiAverages
                    };
                    frameDataList.Add(frameData);
                    var deinterleaveIndex = (int)(packet.FrameIndex % (ulong)packet.DeinterleaveCount);
                    if (imageArray[deinterleaveIndex] == null)
                        imageArray[deinterleaveIndex] = CreateIplImageCopy(packet);

                    downsampleCounter++;
                    if (downsampleCounter >= downsampleFactor)
                    {
                        var frameBundle = new FrameBundle()
                        {
                            Images = imageArray,
                            Frames = frameDataList.ToArray()
                        };
                        _procToRx?.Writer.TryWrite(frameBundle);
                        downsampleCounter = 0;
                        imageArray = new IplImage[_processingDeinterleaveCount];
                        frameDataList.Clear();
                    }

                    if (!_includeTiffWriter)
                        _framePool.Return(packet.DataPtr);
                    _procToWrite?.Writer.TryWrite(packet);
                }
                else
                {
                    packet.RoiAverages = ProcessImage(
                        acq.DataPtr, acq.Width, acq.Height, acq.RowBytes);
                    var frame = new Frame()
                    {
                        Image = CreateIplImageCopy(packet),
                        FrameCounter = packet.FrameIndex,
                        Timestamp = packet.ElapsedSeconds,
                        Left = packet.Left,
                        Top = packet.Top,
                        Width = packet.Width,
                        Height = packet.Height,
                        DeinterleaveCount = packet.DeinterleaveCount,
                        RegionData = packet.RoiAverages
                    };
                    _procToRx?.Writer.TryWrite(frame);
                    if (!_includeTiffWriter)
                        _framePool.Return(packet.DataPtr);
                    _procToWrite?.Writer.TryWrite(packet);
                }
            }
        }

        /// <summary>
        /// Apply the LUT directly to the frame pool.
        /// </summary>
        /// <param name="dataPtr">Pointer to the image</param>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        /// <param name="rowBytes">Width in bytes</param>
        private unsafe void ApplyLUT(IntPtr dataPtr, int width, int height, int rowBytes)
        {
            var bytesPerPixel = rowBytes / width;
            byte* basePtr = (byte*)dataPtr;
            if (bytesPerPixel == 1)
            {
                fixed (byte* pLut = _mono8LookupTable)
                {
                    byte* pRow = basePtr;
                    for (int y = 0; y < height; y++)
                    {
                        byte* p = pRow;
                        byte* end = p + width;

                        // Fast inner loop
                        while (p < end)
                        {
                            *p = pLut[*p];
                            p++;
                        }

                        // Advance to next row
                        pRow += rowBytes;
                    }
                }
            }
            else
            {
                fixed (ushort* pLut = _mono16LookupTable)
                {
                    ushort* pRow = (ushort*)basePtr;
                    for (int y = 0; y < height; y++)
                    {
                        ushort* p = pRow;
                        ushort* end = p + width;

                        // Fast inner loop
                        while (p < end)
                        {
                            *p = pLut[*p];
                            p++;
                        }

                        // Advance to next row
                        pRow = (ushort*)((byte*)pRow + rowBytes);
                    }
                }
            }
        }

        /// <summary>
        /// Apply LUT and calculate the region of interest averages.
        /// </summary>
        /// <param name="dataPtr">Pointer to the image</param>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        /// <param name="rowBytes">Width in bytes</param>
        /// <returns>Values for region of interest averages</returns>
        private unsafe double[] ProcessImage(IntPtr dataPtr, int width, int height, int rowBytes)
        {
            var bytesPerPixel = rowBytes / width;
            byte* basePtr = (byte*)dataPtr;
            if (bytesPerPixel == 1)
            {
                fixed (byte* pLut = _mono8LookupTable)
                {
                    if (_regionsOfInterest == null || _regionsOfInterest.Count == 0)
                    {
                        var results = new double[1];

                        long sum = 0;
                        int pixelCount = width * height;

                        // First pixel of image
                        byte* pRow = basePtr;
                        for (int y = 0; y < height; y++)
                        {
                            byte* p = pRow;
                            byte* end = p + width;

                            // Fast inner loop
                            while (p < end)
                            {
                                *p = pLut[*p];
                                sum += *p++;
                            }

                            // Advance to next row
                            pRow += rowBytes;
                        }
                        double avg = sum / (double)pixelCount;
                        results[0] = avg;
                        return results;
                    }
                    else
                    {
                        // Apply LUT separately
                        byte* pRow = basePtr;
                        for (int y = 0; y < height; y++)
                        {
                            byte* p = pRow;
                            byte* end = p + width;

                            // Fast inner loop
                            while (p < end)
                            {
                                *p = pLut[*p];
                                p++;
                            }

                            // Advance to next row
                            pRow += rowBytes;
                        }

                        var results = new double[_regionsOfInterest.Count];

                        for (int r = 0; r < _regionsOfInterest.Count; r++)
                        {
                            var roi = _regionsOfInterest[r];

                            long sum = 0;
                            int pixelCount = roi.Width * roi.Height;

                            // First pixel of ROI
                            byte* pRowROI = (basePtr + roi.Y * rowBytes) + roi.X;

                            for (int y = 0; y < roi.Height; y++)
                            {
                                byte* p = pRowROI;
                                byte* end = p + roi.Width;

                                // Fast inner loop
                                while (p < end)
                                    sum += *p++;

                                // Advance to next row
                                pRowROI += rowBytes;
                            }

                            double avg = sum / (double)pixelCount;
                            results[r] = avg;
                        }

                        return results;
                    }
                }
            }
            else
            {
                fixed (ushort* pLut = _mono16LookupTable)
                {
                    if (_regionsOfInterest == null || _regionsOfInterest.Count == 0)
                    {
                        var results = new double[1];

                        long sum = 0;
                        int pixelCount = width * height;

                        // First pixel of image
                        ushort* pRow = (ushort*)basePtr;
                        for (int y = 0; y < height; y++)
                        {
                            ushort* p = pRow;
                            ushort* end = p + width;

                            // Fast inner loop
                            while (p < end)
                            {
                                *p = pLut[*p];
                                sum += *p++;
                            }

                            // Advance to next row
                            pRow = (ushort*)((byte*)pRow + rowBytes);
                        }
                        double avg = sum / (double)pixelCount;
                        results[0] = avg;
                        return results;
                    }
                    else
                    {
                        // Apply LUT separately
                        ushort* pRow = (ushort*)basePtr;
                        for (int y = 0; y < height; y++)
                        {
                            ushort* p = pRow;
                            ushort* end = p + width;

                            // Fast inner loop
                            while (p < end)
                            {
                                *p = pLut[*p];
                                p++;
                            }

                            // Advance to next row
                            pRow = (ushort*)((byte*)pRow + rowBytes);
                        }

                        var results = new double[_regionsOfInterest.Count];

                        for (int r = 0; r < _regionsOfInterest.Count; r++)
                        {
                            var roi = _regionsOfInterest[r];

                            long sum = 0;
                            int pixelCount = roi.Width * roi.Height;

                            // First pixel of ROI
                            ushort* pRowROI = (ushort*)(basePtr + roi.Y * rowBytes) + roi.X;

                            for (int y = 0; y < roi.Height; y++)
                            {
                                ushort* p = pRowROI;
                                ushort* end = p + roi.Width;

                                // Fast inner loop
                                while (p < end)
                                    sum += *p++;

                                // Advance to next row
                                pRowROI = (ushort*)((byte*)pRowROI + rowBytes);
                            }

                            double avg = sum / (double)pixelCount;
                            results[r] = avg;
                        }

                        return results;
                    }
                }
            }

        }

        private void RxLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!_procToRx.Reader.TryRead(out var item))
                {
                    Thread.Yield();
                    continue;
                }

                _subject.OnNext(item);
            }
        }

        /// <summary>
        /// Writing loop in a dedicated high-priority thread. If <see cref="CsvWriter"/> and/or <see cref="TiffWriter"/>
        /// are present in the workflow, then write the metadata to .csv and/or write the image data to .tif.
        /// </summary>
        /// <param name="token"></param>
        private void WritingLoop(CancellationToken token)
        {
            CsvWriterHelper csvWriter = null;
            if (_includeCsvWriter)
                csvWriter = new CsvWriterHelper(_csvWriterProperties, _regionsOfInterest);

            TiffWriterHelper tiffWriter = null;
            if (_includeTiffWriter)
                tiffWriter = new TiffWriterHelper(_tiffWriterProperties, _width, _height, _rowBytes / _width, _bytesPerFrame);

            while (!token.IsCancellationRequested)
            {
                // Check for pause request
                if (_pauseEvent.IsSet)
                {
                    // Signal we are paused
                    _pausedWorkers.Signal();

                    // Wait until resume is signaled
                    _resumeEvent.Wait(token);
                    _resumedWorkers.Signal();

                    continue;
                }
                if (!_procToWrite.Reader.TryRead(out var packet))
                {
                    Thread.SpinWait(1);
                    continue;
                }

                // --- CSV writer ---
                if (csvWriter != null)
                    csvWriter.Write(packet.ToString());
                // CSV does NOT touch pointer

                // --- TIFF writer ---
                if (tiffWriter != null)
                    tiffWriter.Write(packet);

                if(_includeTiffWriter)
                    _framePool.Return(packet.DataPtr);
            }

            if (csvWriter != null)
                csvWriter.Dispose();

            if (tiffWriter != null)
                tiffWriter.Dispose();
        }

        /// <summary>
        /// Copy image data from the frame pool to an <see cref="IplImage"/>
        /// </summary>
        /// <param name="p">Frame packet</param>
        /// <returns><see cref="IplImage"/> representation of frame.</returns>
        private unsafe IplImage CreateIplImageCopy(FramePacket p)
        {
            IplImage img;
            if(_isMono16)
                img = new IplImage(new Size(p.Width, p.Height), IplDepth.U16, 1);
            else
                img = new IplImage(new Size(p.Width, p.Height), IplDepth.U8, 1);
            // IplImage uses row-aligned buffers too
            Buffer.MemoryCopy((void*)p.DataPtr, (void*)img.ImageData,
                               p.RowBytes * p.Height,
                               p.RowBytes * p.Height);
            return img;
        }

        /// <summary>
        /// Update the lookup tables in an efficient thread safe manner.
        /// </summary>
        internal void UpdateLookupTable()
        {
            var newMono8LookupTable = new byte[byte.MaxValue + 1];
            var newMono16LookupTable = new ushort[ushort.MaxValue + 1];
            Buffer.BlockCopy(_instance.LookupTable.Mono8, 0, newMono8LookupTable, 0, byte.MaxValue + 1);
            Buffer.BlockCopy(_instance.LookupTable.Mono16, 0, newMono16LookupTable, 0, (ushort.MaxValue + 1) * sizeof(ushort));

            Interlocked.Exchange(ref _mono8LookupTable, newMono8LookupTable);
            Interlocked.Exchange(ref _mono16LookupTable, newMono16LookupTable);
        }

        /// <summary>
        /// Disposal process. Trigger the <see cref="CancellationTokenSource"/>,
        /// wait for threads to complete, Dispose all <see cref="Channel"/>, <see cref="UnmanagedFramePool"/>,
        /// and <see cref="CancellationTokenSource"/>. Finally, release the buffer, close the camera, and deinitialize the
        /// <see cref="DCAM_API_MANAGER"/>.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
            {
                try
                {
                    _cts.Cancel();

                    if(_isPaused)
                    {
                        _resumedWorkers = new CountdownEvent(_numWorkers);
                        _pauseEvent.Reset();
                        _pausedWorkers.Reset();
                        _resumeEvent.Set();
                        _resumedWorkers.Wait();
                        _resumeEvent.Reset();
                        _resumedWorkers.Reset();
                    }
                    // Wait threads to finish
                    _acquisitionThread?.Join();
                    _processingThread?.Join();
                    _writingThread?.Join();
                    _rxThread?.Join();
                }
                catch (Exception ex)
                {
                    ConsoleLogger.LogError(ex);
                }

                // Dispose channel
                _acqToProc.Writer.Complete();
                _procToWrite?.Writer.Complete();
                _procToRx.Writer.Complete();

                // Dispose frame pool
                _framePool?.Dispose();

                // Dispose CTS
                _cts.Dispose();

                if (!_isPaused)
                    ReleaseBuffer();
                CloseAndDeinit();
            }
            disposedValue = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

using Bonsai;
using AllenNeuralDynamics.HamamatsuCamera.API;
using AllenNeuralDynamics.HamamatsuCamera.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Xml.Serialization;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    /// <summary>
    /// The main interface to the Hamamatsu Camera. It is responsible for camera initialization, configuration, and image acquisition.
    /// </summary>
    [Description("Generates a sequence of images from Hamamatsu Cameras.")]
    [Editor("AllenNeuralDynamics.HamamatsuCamera.Calibration.C13440Editor, AllenNeuralDynamics.HamamatsuCamera", typeof(ComponentEditor))]
    public class C13440 : Source<IFrameContainer>
    {
        private readonly CameraCapture _capture;

        /// <summary>
        /// Contains the camera properties. Used to initialize the <see cref="Calibration.CalibrationForm"/> and to interface with
        /// the camera in <see cref="CameraCapture"/>.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public IEnumerable<DCAM_PROP_MANAGER> CameraProps { get; set; }

        /// <summary>
        /// Contains the camera properties stored by the <see cref="Calibration.CalibrationForm"/>.
        /// Used to specify the <see cref="CameraProps"/> inside of the <see cref="CameraCapture"/>
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<int, double> StoredSettings { get; set; }

        /// <summary>
        /// User defined points for generating the lookup tables.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<ushort, ushort> PointsOfInterest { get; set; }

        /// <summary>
        /// Lookup table configured within <see cref="Calibration.LUTEditor"/> and used within the <see cref="CameraCapture"/>
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        internal LookupTable LookupTable { get; set; }

        /// <summary>
        /// Regions of interest defined by <see cref="Calibration.CalibrationForm"/> and used within <see cref="CameraCapture"/>
        /// </summary>
        [Browsable(false)]
        public List<RegionOfInterest> Regions { get; set; }

        /// <summary>
        /// Defines whether to auto crop based on the <see cref="Regions"/> or manually crop based on the subarray.
        /// </summary>
        [Browsable(false)]
        public CropMode CropMode { get; set; } = CropMode.Auto;

        /// <summary>
        /// Toggles frame bundling. While enabled this will generate <see cref="IObservable{FrameBundle}"/>, while
        /// disabled this will generate <see cref="IObservable{Frame}"/>
        /// </summary>
        public bool EnableBundling { get; set; }

        /// <summary>
        /// Number of full cycles to include in a bundle.
        /// This times the <see cref="Processing.DeinterleaveCount"/> will be the true number of frames in a bundle.
        /// </summary>
        public int BundleSize { get; set; } = 1;

        /// <summary>
        /// Optional path to a settings .xml file.
        /// </summary>
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string SettingsPath { get; set; }

        /// <summary>
        /// Option path to a lookup table .csv file.
        /// </summary>
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string LookupTablePath { get; set; }

        /// <summary>
        /// Instance of <see cref="CameraCapture"/> for passing <see cref="IFrameContainer"/> to <see cref="Generate"/>
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        internal CameraCapture Capture
        {
            get { return _capture; }
        }

        /// <summary>
        /// Initializes the <see cref="CameraCapture"/>, <see cref="LookupTable"/>, and <see cref="PointsOfInterest"/>
        /// </summary>
        public C13440()
        {
            _capture = new CameraCapture(this);
            LookupTable = new LookupTable();
            PointsOfInterest = new Dictionary<ushort, ushort>()
            {
                [0] = 0,
                [ushort.MaxValue] = ushort.MaxValue
            };
            Regions = new List<RegionOfInterest>();
        }

        /// <summary>
        /// Generates an <see cref="IObservable{IFrameContainer}"/>. By default this will contain
        /// <see cref="Frame"/>, but if <see cref="EnableBundling"/>, then this will contain <see cref="FrameBundle"/>
        /// </summary>
        /// <returns></returns>
        public override IObservable<IFrameContainer> Generate()
        {
            return Capture.Generate().ObserveOn(TaskPoolScheduler.Default).Publish().RefCount();
        }
    }
}

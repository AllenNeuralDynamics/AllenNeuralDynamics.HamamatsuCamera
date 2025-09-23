using Bonsai;
using Bonsai.IO;
using AllenNeuralDynamics.HamamatsuCamera.API;
using AllenNeuralDynamics.HamamatsuCamera.Factories;
using AllenNeuralDynamics.HamamatsuCamera.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Xml.Serialization;

namespace AllenNeuralDynamics.HamamatsuCamera
{
    /// <summary>
    /// Connects with the C13440 Hamamatsu Camera and generates an observable sequence of
    /// <see cref="Frame"/> data. Has an associated <see cref="C13440CalibrationEditorForm"/>
    /// for calibrating the <see cref="CameraProps"/> and <see cref="Regions"/>. These properties
    /// and regions are stored within this class after closing the calibration editor. The Camera Properties
    /// are used to initialize the camera within the <see cref="FrameFactory"/>. The Regions are used 
    /// within the <see cref="ImageProcessing"/> node to confine analysis to the pre-defined regions.
    /// </summary>
    [Description("Generates a sequence of images from Hamamatsu Cameras.")]
    [Editor("AllenNeuralDynamics.HamamatsuCamera.Calibration.C13440Editor, AllenNeuralDynamics.HamamatsuCamera", typeof(ComponentEditor))]
    public class C13440 : Source<Frame>
    {

        [DisplayName("TIFF Properties")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public TiffProperties TiffProperties { get; set; } = new TiffProperties();


        [DisplayName("Image Processing Properties")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public ImageProcessingProperties ImageProcessingProperties { get; set; } = new ImageProcessingProperties();

        /// <summary>
        /// Contains the list of Camera Properties found 
        /// and calibrated within the <see cref="C13440CalibrationEditorForm"/>
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public IEnumerable<DCAM_PROP_MANAGER> CameraProps { get; set; }


        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<int, double> StoredSettings { get; set; }


        [Browsable(false)]
        public CropMode CropMode { get; set; } = CropMode.Auto;

        [XmlIgnore]
        [Browsable(false)]
        public FrameFactory FrameFactory;

        public bool Acquiring = true;

        /// <summary>
        /// Generates an observable sequence of <see cref="Frame"/> from 
        /// the created <see cref="FrameFactory"/>
        /// </summary>
        /// <returns></returns>
        public override IObservable<Frame> Generate()
        {
            // Create an observable sequence from a factory
            return Observable.Create<Frame>(observer =>
            {
                // Create the factory and initialize it with stored camera properties and regions
                FrameFactory = new FrameFactory(observer,this);
                return FrameFactory;
            }).Publish().RefCount();
        }
    }
}

﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace KinectKannon
{
    using Microsoft.Kinect;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Timers;
    using System.Windows.Input;
    using KinectKannon.Rendering;
    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        private AudioBeamFrameReader audioReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Describes an arbitrary number which represents how far left or right the cannon is position
        /// This range of this value is TBD
        /// </summary>
        private double cannonXPosition = 0.0f;

        /// <summary>
        /// Describes an arbitrary number which represents how high up or low the cannon is position
        /// This range of this value is TBD
        /// </summary>
        private double cannonYPosition = 0.0f;

        /// <summary>
        /// The current tracking mode of the system
        /// </summary>
        private TrackingMode trackingMode = TrackingMode.MANUAL;

        /// <summary>
        /// The skeleton that is requested to be tracked
        /// </summary>
        private SkeletalLetter requestedTrackedSkeleton = SkeletalLetter.A;

        /// <summary>
        /// The frame rate that will be displayed.
        /// </summary>
        private double debugFrameRate = 0.0f;

        /// <summary>
        /// The number which olds the amount of frames since the last invocation of the timer.
        /// </summary>
        private int elapsedFrames = 0;
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        /// 
        private ColorFrameRenderer colorRenderer;

        /// <summary>
        /// Responsible for drawing the HUD layer
        /// </summary>
        private HudRenderer hudRenderer;

        private string statusText = null;

        /// <summary>
        /// Last observed audio beam angle in radians, in the range [-pi/2, +pi/2]
        /// </summary>
        private float beamAngle = 0;

        /// <summary>
        /// Last observed audio beam angle confidence, in the range [0, 1]
        /// </summary>
        private float beamAngleConfidence = 0;

        /// <summary>
        /// Will be allocated a buffer to hold a single sub frame of audio data read from audio stream.
        /// </summary>
        private readonly byte[] audioBuffer = null;

        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            this.audioReader = this.kinectSensor.AudioSource.OpenReader();

            // get the depth (display) extents
            FrameDescription jointFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            colorRenderer = new ColorFrameRenderer(colorFrameDescription.Width, colorFrameDescription.Height, jointFrameDescription.Width, jointFrameDescription.Height);
            var drawingGroup = new DrawingGroup();
            var drawingImage = new DrawingImage(drawingGroup);
            hudRenderer = new HudRenderer(drawingGroup, drawingImage, colorFrameDescription.Width, colorFrameDescription.Height);

            AudioSource audioSource = this.kinectSensor.AudioSource;

            // Allocate 1024 bytes to hold a single audio sub frame. Duration sub frame 
            // is 16 msec, the sample rate is 16khz, which means 256 samples per sub frame. 
            // With 4 bytes per sample, that gives us 1024 bytes.
            this.audioBuffer = new byte[audioSource.SubFrameLengthInBytes];

            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            this.audioReader.FrameArrived += audioReader_FrameArrived;

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text TODO: change namespace name in resources
            this.StatusText = this.kinectSensor.IsAvailable ? Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.RunningStatusText
                                                            : Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.NoSensorStatusText;
            
            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            //register the code which will tell the system what to do when keys are pressed
            SetupKeyHandlers();

            //draw the headsup display initially
            this.hudRenderer.RenderHud(new HudRenderingParameters()
            {
                CannonX = this.CannonX,
                CannonY = this.CannonY,
                StatusText = this.statusText,
                SystemReady = (this.kinectSensor.IsAvailable && this.kinectSensor.IsOpen),
                FrameRate = this.FrameRate,
                TrackingMode = this.trackingMode
            });

            //debug start frame rate counter
            FPSTimerStart();
        }

        void audioReader_FrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            AudioBeamFrameReference frameReference = e.FrameReference;

            try
            {
                AudioBeamFrameList frameList = frameReference.AcquireBeamFrames();

                if (frameList != null)
                {
                    // AudioBeamFrameList is IDisposable
                    using (frameList)
                    {
                        // Only one audio beam is supported. Get the sub frame list for this beam
                        IReadOnlyList<AudioBeamSubFrame> subFrameList = frameList[0].SubFrames;

                        // Loop over all sub frames, extract audio buffer and beam information
                        foreach (AudioBeamSubFrame subFrame in subFrameList)
                        {
                            // Check if beam angle and/or confidence have changed
                            bool updateBeam = false;

                            if (subFrame.BeamAngle != this.beamAngle)
                            {
                                this.beamAngle = subFrame.BeamAngle;
                                updateBeam = true;
                            }

                            if (subFrame.BeamAngleConfidence != this.beamAngleConfidence)
                            {
                                this.beamAngleConfidence = subFrame.BeamAngleConfidence;
                                updateBeam = true;
                            }

                            if (updateBeam)
                            {
                                // Refresh display of audio beam
                                this.AudioBeamChanged();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore if the frame is no longer available
            }
        }

        /// <summary>
        /// Method called when audio beam angle and/or confidence have changed.
        /// </summary>
        private void AudioBeamChanged()
        {
            // Maximum possible confidence corresponds to this gradient width
            const float MinGradientWidth = 0.04f;

            // Set width of mark based on confidence.
            // A confidence of 0 would give us a gradient that fills whole area diffusely.
            // A confidence of 1 would give us the narrowest allowed gradient width.
            float halfWidth = Math.Max(1 - this.beamAngleConfidence, MinGradientWidth) / 2;

            // Update the gradient representing sound source position to reflect confidence
            this.beamBarGsPre.Offset = Math.Max(this.beamBarGsMain.Offset - halfWidth, 0);
            this.beamBarGsPost.Offset = Math.Min(this.beamBarGsMain.Offset + halfWidth, 1);

            // Convert from radians to degrees for display purposes
            float beamAngleInDeg = this.beamAngle * 180.0f / (float)Math.PI;

            // Rotate gradient to match angle
            beamBarRotation.Angle = -beamAngleInDeg;
            beamNeedleRotation.Angle = -beamAngleInDeg;
        }

        private void SetupKeyHandlers()
        {
            KeyDown += MainWindow_KeyDown;
        }

        /// <summary>
        /// Handles an 'controller' actions using the keyboard interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //TODO: This is where the logic for controlling the servos will be placed
            if(e.Key == System.Windows.Input.Key.Up && this.trackingMode == TrackingMode.MANUAL)
            {
                this.cannonYPosition += .1;
            }
            else if (e.Key == System.Windows.Input.Key.Down && this.trackingMode == TrackingMode.MANUAL)
            {
                this.cannonYPosition -= .1;
            }
            else if (e.Key == System.Windows.Input.Key.Left && this.trackingMode == TrackingMode.MANUAL)
            {
                this.cannonXPosition -= .1;
            }
            else if (e.Key == System.Windows.Input.Key.Right && this.trackingMode == TrackingMode.MANUAL)
            {
                this.cannonXPosition += .1;
            }
            else if (e.Key == Key.NumPad1 || e.Key == Key.D1)
            {
                this.trackingMode = TrackingMode.MANUAL;
                AudioViewBox.Visibility = Visibility.Hidden;
            }
            else if (e.Key == Key.NumPad2 || e.Key == Key.D2)
            {
                this.trackingMode = TrackingMode.SKELETAL;
                AudioViewBox.Visibility = Visibility.Hidden;
            }
            else if (e.Key == Key.NumPad3 || e.Key == Key.D3)
            {
                this.trackingMode = TrackingMode.AUDIBLE;
                AudioViewBox.Visibility = Visibility.Visible;
            }
            else if (this.trackingMode == TrackingMode.SKELETAL &&
                e.Key == Key.A)
            {
                this.requestedTrackedSkeleton = SkeletalLetter.A;
            }
            else if (this.trackingMode == TrackingMode.SKELETAL &&
                e.Key == Key.B)
            {
                this.requestedTrackedSkeleton = SkeletalLetter.B;
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            //render color layer
            this.colorRenderer.Reader_ColorFrameArrived(sender, e);
            elapsedFrames++;
            //draw the headsup display initially
            this.hudRenderer.RenderHud(new HudRenderingParameters()
            {
                CannonX = this.CannonX,
                CannonY = this.CannonY,
                StatusText = this.statusText,
                SystemReady = (this.kinectSensor != null && this.kinectSensor.IsAvailable && this.kinectSensor.IsOpen),
                FrameRate = this.FrameRate,
                TrackingMode = this.trackingMode
            });
        }

        private void FPSTimerStart()
        {
            var fpsTimer = new Timer(1000);
            fpsTimer.Elapsed += fpsTimer_Elapsed;
            fpsTimer.Enabled = true;
        }

        void fpsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.debugFrameRate = elapsedFrames;
            elapsedFrames = 0;
        }
        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.colorRenderer.ImageSource;
            }
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource HudSource
        {
            get
            {
                return this.hudRenderer.ImageSource;
            }
        }

        public string FrameRate
        {
            get
            {
                return String.Format("{0:0.00}", this.debugFrameRate);
            }
        }

        public string CannonX
        {
            get
            {
                return String.Format("{0:0.00}", this.cannonXPosition);
            }
        }

        public string CannonY
        {
            get
            {
                return String.Format("{0:0.00}", this.cannonYPosition);
            }
        }
        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                int? trackIndex = null;
                if((int)requestedTrackedSkeleton < this.bodies.Length 
                    && this.trackingMode == TrackingMode.SKELETAL){
                    trackIndex = (int)requestedTrackedSkeleton;
                }

                if (this.trackingMode == TrackingMode.SKELETAL)
                {
                    colorRenderer.DrawBodies(this.bodies, this.coordinateMapper, trackIndex);
                }
                
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.statusText = this.kinectSensor.IsAvailable ? Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.RunningStatusText
                                                            : Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.SensorNotAvailableStatusText;
        }

    }

    /// <summary>
    /// Used to determine which skeleton will be tracked if in SKELETAL tracking
    /// mode
    /// </summary>
    public enum SkeletalLetter
    {
        A = 0,
        B,
        C,
        D,
        E,
        F
    }

    /// <summary>
    /// The tracking state of the system. Used to determine if pan/tilt will be controlled
    /// autonomously or manually
    /// </summary>
    public enum TrackingMode
    {
        MANUAL,
        SKELETAL,
        AUDIBLE
    }
}
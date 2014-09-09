//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace KinectKannon
{
    //using J2i.Net.XInputWrapper;
    using Microsoft.Kinect;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using System.Windows.Input;
    using KinectKannon.Rendering;
    using System.Timers;
    using KinectKannon.Autonomy;
    using KinectKannon.Control;
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

        private const uint PAN_TILT_SPEED_LIMIT = 60;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Describes an arbitrary number which represents how far left or right the cannon is position
        /// This range of this value is TBD
        /// </summary>
        private double cannonXVelocity = 0.0f;

        /// <summary>
        /// Describes an arbitrary number which represents how high up or low the cannon is position
        /// This range of this value is TBD
        /// </summary>
        private double cannonYVelocity = 0.0f;

        /// <summary>
        /// Describes number which represents the theta angle, what direction from center the target is at. 
        /// </summary>
        private double cannonThetaPosition = 0.0f;

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
        /// Responsible for finding skeletal XY positions of a tracked body's SpineMid.
        /// </summary>
        private KannonAutonomy skeletonAutomator = new KannonAutonomy();

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

        /// <summary>
        /// Signifies Pan tilt out of range State
        /// </summary>
        private bool m_PanTiltTooFarLeft = false;
        private bool m_PanTiltTooFarRight = false;
        private bool m_PanTiltTooFarUp = false;
        private bool m_PanTiltTooFarDown = false;

        private PanTiltController panTilt;
        private FiringController firingControl;
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

	    //Setup Contoller
            /**
            _selectedController = XboxController.RetrieveController(0);
            _selectedController.StateChanged += _selectedController_StateChanged;
            XboxController.StartPolling();
             * **/
            //initialize
            panTilt = PanTiltController.GetOrCreatePanTiltController();
            firingControl = FiringController.GetOrCreateFiringController();

            var panTiltErr = panTilt.TryInitialize();
            var firingErr = firingControl.TryInitialize();
            if (panTiltErr != null)
            {
                //crash the app. we can't do anything if it doesn't intialize
                throw panTiltErr;
            }

            if (firingErr != null)
            {
                //crash the app. we can't do anything if it doesn't intialize
                throw firingErr;
            }

            string safetyText;
            if (this.firingControl.VirtualSafetyOn)
            {
                safetyText = Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.SafetyDisengagedText;
            }
            else
            {
                safetyText = Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.SafetyEngagedText;
            }
            panTilt.TryInitialize();

            //draw the headsup display initially
            this.hudRenderer.RenderHud(new HudRenderingParameters()
            {
                CannonX = this.CannonX,
                CannonY = this.CannonY,
                //CannonTheta = this.CannonTheta,
                StatusText = this.statusText,
                SystemReady = (this.kinectSensor.IsAvailable && this.kinectSensor.IsOpen && this.panTilt.IsReady),
                FrameRate = this.FrameRate,
                TrackingMode = this.trackingMode,
                FiringSafety = this.firingControl.VirtualSafetyOn,
                FiringSafetyText = safetyText
            });

            

            //debug start frame rate counter
            FPSTimerStart();

            // Try to use the controller
            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //XboxController.StopPolling();
            base.OnClosing(e);
        }
/**
        void _selectedController_StateChanged(object sender, XboxControllerStateChangedEventArgs e)
        {
            OnPropertyChanged("SelectedController");

            // Where the action happens with the controller. 
            if (SelectedController.IsAPressed) 
            {
                Console.WriteLine("A is pressed");
            }
            else if (SelectedController.IsBPressed)
            {
                Console.WriteLine("B is pressed");
            }
        }
        
        public XboxController SelectedController
        {
            get { return _selectedController; }
        }

        volatile bool _keepRunning;
        XboxController _selectedController;

        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                Action a = () => { PropertyChanged(this, new PropertyChangedEventArgs(name)); };
                Dispatcher.BeginInvoke(a, null);
            }
        }

         

        private void SelectedControllerChanged(object sender, RoutedEventArgs e)
        {
            _selectedController = XboxController.RetrieveController(((ComboBox)sender).SelectedIndex);
            OnPropertyChanged("SelectedController");
        }
        **/
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


        //Some way of using the controller
        // An event handler
        // Takes in XInput.Event args
        //  if (b.Key == XInput.Controller.Key."B")
        //  { this.trackingMode = TrackingMode.SKELETAL
        //    AudioViewBox.Visisbility = Visibility.Hidden;


        /// <summary>
        /// Handles an 'controller' actions using the keyboard interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ////////////////////////////////////////////////////////////////////////////
            //Manual Control Logic
            ////////////////////////////////////////////////////////////////////////////
            if (this.panTilt.IsReady)
            {
                try
                {
                    //Pan Up
                    if (e.Key == System.Windows.Input.Key.Down && !this.m_PanTiltTooFarDown && this.trackingMode == TrackingMode.MANUAL)
                    {
                        if (this.cannonYVelocity <= PAN_TILT_SPEED_LIMIT)
                        {
                            this.cannonYVelocity += 20;
                        }
                        //set too far to false. if its stil too far the next key event handler will set to true
                        this.m_PanTiltTooFarUp = false;
                        this.panTilt.PanY(this.cannonYVelocity);
                    }
                    else if (e.Key == System.Windows.Input.Key.Up && !this.m_PanTiltTooFarUp && this.trackingMode == TrackingMode.MANUAL)
                    {
                        if (this.cannonYVelocity >= -1 * PAN_TILT_SPEED_LIMIT)
                        {
                            this.cannonYVelocity -= 20;
                        }
                        //set too far to false. if its stil too far the next key event handler will set to true
                        this.m_PanTiltTooFarDown = false;
                        this.panTilt.PanY(this.cannonYVelocity);
                    }
                    else if (e.Key == System.Windows.Input.Key.Right && !this.m_PanTiltTooFarRight && this.trackingMode == TrackingMode.MANUAL)
                    {
                        if (this.cannonXVelocity >= -1 * PAN_TILT_SPEED_LIMIT)
                        {
                            this.cannonXVelocity -= 20;
                        }
                        //set too far to false. if its stil too far the next key event handler will set to true
                        this.m_PanTiltTooFarLeft = false;
                        this.panTilt.PanX(this.cannonXVelocity);
                    }
                    else if (e.Key == System.Windows.Input.Key.Left && !this.m_PanTiltTooFarLeft && this.trackingMode == TrackingMode.MANUAL)
                    {
                        if (this.cannonXVelocity <= PAN_TILT_SPEED_LIMIT)
                        {
                            this.cannonXVelocity += 20;
                        }
                        //set too far to false. if its stil too far the next key event handler will set to true
                        this.m_PanTiltTooFarRight = false;
                        this.panTilt.PanX(this.cannonXVelocity);
                    }
                }
                //anything goes wrong, stop pantilt and crash the app
                catch (Exception ex)
                {
                    this.panTilt.Disengage();
                    throw ex;
                }
                
            }
            else if (e.Key == Key.NumPad1 || e.Key == Key.D1)
            {
                this.trackingMode = TrackingMode.MANUAL;
                AudioViewBox.Visibility = Visibility.Hidden;
            }
            else if (e.Key == Key.NumPad2 || e.Key == Key.D2 )
            {
                this.trackingMode = TrackingMode.SKELETAL;
                AudioViewBox.Visibility = Visibility.Hidden;
            }
            else if (e.Key == Key.NumPad3 || e.Key == Key.D3 )
            {
                this.trackingMode = TrackingMode.AUDIBLE;
                AudioViewBox.Visibility = Visibility.Visible;
            }
            //The ordering rational of the key assignments is based on 
            //the bottom row of the keyboard
            else if (this.trackingMode == TrackingMode.SKELETAL &&
                e.Key == Key.Z)
            {
                    this.requestedTrackedSkeleton = SkeletalLetter.A;
            }
            else if (this.trackingMode == TrackingMode.SKELETAL &&
                e.Key == Key.X)
            {
                    this.requestedTrackedSkeleton = SkeletalLetter.B;
            }
            else if (this.trackingMode == TrackingMode.SKELETAL &&
                e.Key == Key.C)
            {
                    this.requestedTrackedSkeleton = SkeletalLetter.C;
            }
            else if (this.trackingMode == TrackingMode.SKELETAL &&
                e.Key == Key.V)
            {
                    this.requestedTrackedSkeleton = SkeletalLetter.D;
            }
            else if (this.trackingMode == TrackingMode.SKELETAL &&
                e.Key == Key.B)
            {
                    this.requestedTrackedSkeleton = SkeletalLetter.E;
            }
            else if (this.trackingMode == TrackingMode.SKELETAL &&
                e.Key == Key.N)
            {
                    this.requestedTrackedSkeleton = SkeletalLetter.F;
            }

            /////////////////////////////////////////////////////////
            //Rage Safteys - Makey Makey Board Provides These Events
            /////////////////////////////////////////////////////////
            try
            {
                //Cannon is too high
                if (e.Key == Key.A && !m_PanTiltTooFarUp)
                {
                    m_PanTiltTooFarUp = true;
                    this.cannonYVelocity = 0;
                    this.panTilt.PanY(this.cannonYVelocity);
                }
                //Cannon is too low
                else if (e.Key == Key.S && !m_PanTiltTooFarDown)
                {
                    m_PanTiltTooFarDown = true;
                    this.cannonYVelocity = 0;
                    this.panTilt.PanY(this.cannonYVelocity);
                }
                //Cannon is too far right
                else if (e.Key == Key.D && !m_PanTiltTooFarRight)
                {
                    this.m_PanTiltTooFarRight = true;
                    this.cannonXVelocity = 0;
                    this.panTilt.PanX(this.cannonXVelocity);
                }
                //Cannon is too far left
                else if (e.Key == Key.F && !m_PanTiltTooFarLeft)
                {
                    m_PanTiltTooFarLeft = true;
                    this.cannonXVelocity = 0;
                    this.panTilt.PanX(this.cannonXVelocity);
                }
            }
            //if anything at all goes wrong we gotta stop the pantilt and crash the app
            catch(Exception ex)
            {
                this.panTilt.Disengage();
                throw ex;
            }

            if (e.Key == Key.P)
            {
                //toggle the safety
                this.firingControl.VirtualSafetyOn = !this.firingControl.VirtualSafetyOn;
            }
            //MAYBE WE SHOULD PICK A HARDER TO PRESS KEY THAN THE SPACE BAR?
            if (e.Key == Key.Space)
            {
                await this.firingControl.Fire(300);
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

            var systemReady = (this.kinectSensor != null && this.kinectSensor.IsAvailable && this.kinectSensor.IsOpen && this.panTilt.IsReady);

            if (systemReady)
            {
                this.statusText = Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.RunningStatusText;
            }
            string safetyText;
            if (this.firingControl.VirtualSafetyOn)
            {
                safetyText = Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.SafetyDisengagedText;
            }
            else
            {
                safetyText = Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.SafetyEngagedText;
            }
            //draw the headsup display initially
            this.hudRenderer.RenderHud(new HudRenderingParameters()
            {
                CannonX = this.CannonX,
                CannonY = this.CannonY,
                //CannonTheta = this.CannonTheta,
                StatusText = this.statusText,
                SystemReady = systemReady,
                FrameRate = this.FrameRate,
                TrackingMode = this.trackingMode,
                FiringSafety = this.firingControl.VirtualSafetyOn,
                FiringSafetyText = safetyText
            });
        }

        private void FPSTimerStart()
        {
            var fpsTimer = new System.Timers.Timer(1000);
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
        public string Safety
        {
            get
            {
                if (true == this.firingControl.VirtualSafetyOn)
                {
                    return "Cannon Safety Engaged";
                }
                else
                {
                    return "Cannon Saftey Disengaged. !!WARNING Cannon Armed!!";
                }
            }
        }
        public string CannonX
        {
            get
            {
                return String.Format("{0:0.00}", this.cannonXVelocity);
            }
        }

        public string CannonY
        {
            get
            {
                return String.Format("{0:0.00}", this.cannonYVelocity);
            }
        }

        public string CannonTheta
        {
            get
            {
                return String.Format("{0:0.00}", this.cannonThetaPosition);
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
                // Count is used to iterate through Bodies[] and compared with the trackIndex.
                if (this.trackingMode == TrackingMode.SKELETAL)
                {
                    if(bodies != null && bodies.Length > trackIndex)
                    // Iterate through each body present in bodies[] and verify,
                    // if body.isTracked is true. If true send body to skeletal autonomator,
                    // to return X, Y, position and Theta angle of the SpineMid Joint.
                    {
                        skeletonAutomator.skeletalAutonomy(this.bodies, trackIndex);
                        this.cannonXVelocity = skeletonAutomator.getXDist;
                        this.cannonYVelocity = skeletonAutomator.getYDist;
                    }
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
            if (this.kinectSensor.IsAvailable)
            {
                if (this.panTilt.IsReady)
                {
                    this.statusText = Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.RunningStatusText;
                }
                else
                {
                    this.statusText = Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.PanTiltNotAvailableStatusText;
                }
            }
            else{
                 this.statusText = Microsoft.Samples.Kinect.BodyBasics.Properties.Resources.SensorNotAvailableStatusText;
            }
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

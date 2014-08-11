//------------------------------------------------------------------------------
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
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const int ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Color handClosedColor = Color.FromArgb(128, 255, 0, 0);

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Color handOpenColor = Color.FromArgb(128, 0, 255, 0);

        /// <summary>
        /// Brush used to draw cross Hairs
        /// </summary>
        private readonly Color crossHairColor = Color.FromArgb(128, 0, 0, 0);

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Color handLassoColor = Color.FromArgb(128, 0, 0, 255);

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Color trackedJointColor = Color.FromArgb(255, 68, 192, 68);

        /// <summary>
        /// Brush used for drawing joints that are currently inferred (yellow)
        /// </summary>        
        private readonly Color inferredJointColor = Color.FromArgb(255, 248, 254, 12);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Color inferredBoneColor = Color.FromArgb(255, 133, 128, 138);

        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private readonly uint bytesPerPixel = 0;

        /// <summary>
        /// The bitmap used to stream the camera
        /// </summary>
        private WriteableBitmap imageSource;

        /// <summary>
        /// The bitmap used to stream the camera
        /// </summary>
        private DrawingGroup hudDrawingGroup;

        /// <summary>
        /// The bitmap used to stream the camera
        /// </summary>
        private DrawingImage hudSource;

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

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int jointDisplayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int jointDisplayHeight;

        /// <summary>
        /// Width of display (color space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (color space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

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

        private TrackingMode trackingMode = TrackingMode.MANUAL;

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
        public MainWindow()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.jointDisplayWidth = frameDescription.Width;
            this.jointDisplayHeight = frameDescription.Height;
            
            //setup the drawing area for the HUD
            this.hudDrawingGroup = new DrawingGroup();

            this.hudSource = new DrawingImage(this.hudDrawingGroup);
            
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the bitmap to display
            this.imageSource = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Pbgra32, null);

            // rgba is 4 bytes per pixel
            this.bytesPerPixel = colorFrameDescription.BytesPerPixel;

            
            // get size of picture space
            this.displayWidth = colorFrameDescription.Width;
            this.displayHeight = colorFrameDescription.Height;

            // wire handler for frame arrival
            //this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            colorRenderer = new ColorFrameRenderer(colorFrameDescription.Width, colorFrameDescription.Height);

            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

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
            RenderHud();

            //debug start frame rate counter
            FPSTimerStart();
        }

        private void SetupKeyHandlers()
        {
            KeyDown += MainWindow_KeyDown;
        }

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
            }
            else if (e.Key == Key.NumPad2 || e.Key == Key.D2)
            {
                this.trackingMode = TrackingMode.SKELETAL;
            }
            else if (e.Key == Key.NumPad3 || e.Key == Key.D3)
            {
                this.trackingMode = TrackingMode.AUDIBLE;
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {

            this.colorRenderer.Reader_ColorFrameArrived(sender, e);
            elapsedFrames++;
            RenderHud();
        }

        private void RenderHud()
        {
            using (DrawingContext dc = this.hudDrawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                var statusBrush = Brushes.Green;
                if (!(kinectSensor.IsAvailable && kinectSensor.IsOpen))
                {
                    statusBrush = Brushes.Red;
                }

                //frame rate
                RenderHudText(dc, "FPS: " + this.FrameRate, Brushes.White, 20, new Point(1800, 0));
                //System Status
                RenderHudText(dc, "System Status: " + this.statusText, statusBrush, 20, new System.Windows.Point(0, 0));
                //Cannon Properties
                RenderHudText(dc, "Cannon Status: ", Brushes.White, 40, new Point(0,120));
                RenderHudText(dc, "X Position: " + this.CannonX, Brushes.YellowGreen, 20, new Point(0, 180));
                RenderHudText(dc, "Y Position: " + this.CannonY, Brushes.YellowGreen, 20, new Point(0, 200));

                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), new Pen(), new Rect(10, 300, 300, 300));
                RenderHudText(dc, "Tracking Mode", Brushes.White, 20, new Point(80, 310));
                RenderHudText(dc, trackingMode.ToString(), Brushes.YellowGreen, 40, new Point(70, 420));
            }
        }

        private void RenderHudText(DrawingContext dc, string text, Brush color, int fontSize, Point location)
        {
            //frame rate
            dc.DrawText(new FormattedText(text,
                      CultureInfo.GetCultureInfo("en-us"),
                      FlowDirection.LeftToRight,
                      new Typeface("Verdana"),
                      fontSize, color),
                      location);
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
                return this.hudSource;
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
                // Draw a transparent background to set the render size
                this.imageSource.DrawRectangle(0, 0, this.displayWidth, this.displayHeight, Color.FromArgb(255, 128,128,128));
                
                
                foreach (Body body in this.bodies)
                {
                    if (body.IsTracked)
                    {
                        this.colorRenderer.DrawClippedEdges(body);

                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                        // convert the joint points to depth (display) space
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                        foreach (JointType jointType in joints.Keys)
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = InferredZPositionClamp;
                            }

                            DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                            //convert joint space to colro space so that we can draw skeletons on top of color feed
                            jointPoints[jointType] = new Point((depthSpacePoint.X / this.jointDisplayWidth) * 1920, (depthSpacePoint.Y / this.jointDisplayHeight) * 1080);
                        }

                        this.colorRenderer.DrawBody(joints, jointPoints, this.trackedJointColor);

                        this.colorRenderer.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft]);
                        this.colorRenderer.DrawHand(body.HandRightState, jointPoints[JointType.HandRight]);
                    }
                }
                // prevent drawing outside of our render area
                //this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
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

        enum TrackingMode
        {
            MANUAL,
            SKELETAL,
            AUDIBLE
        }
    }
    public enum SkeletalLetter
    {
        A = 0,
        B,
        C,
        D,
        E,
        F
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Speech.Synthesis;
namespace KinectKannon.Rendering
{
    /// <summary>
    /// Handles all rendering of camera (color) feed as well as capabilities to draw bodies on top of the camera feed
    /// </summary>
    public class ColorFrameRenderer
    {
        //TODO: Make these properties so that they are accesible publicly for tweaking the frame UI
        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private readonly uint bytesPerPixel = 4;

        //TODO: Make these properties so that they are accesible publicly for tweaking the frame UI
        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private readonly int bytesPerInfraredPixel = 2;

        // <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const int ClipBoundsThickness = 10;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 10;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

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
        /// Intermediate storage for receiving color frame data from the sensor
        /// </summary>
        private byte[] colorPixels = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private ushort[] infraredPixels = null;

        /// <summary>
        /// Width of display (color space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (color space)
        /// </summary>
        private int displayHeight;

        private int jointDisplayWidth;

        private int jointDisplayHeight;

        /// <summary>
        /// The bitmap used to stream the camera
        /// </summary>
        private WriteableBitmap imageSource;

        /// <summary>
        /// The bitmap used to stream the camera
        /// </summary>
        private WriteableBitmap infraredSource;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;
        
        /// <summary>
        /// Triggers voice output when we have a skeleton
        /// </summary>
        private SpeechSynthesizer voiceSynth = new SpeechSynthesizer();

        /// <summary>
        /// Indiicates if we are actually tracking a real person
        /// </summary>
        private bool hasValidTracking = false;

        private int infraredWidth;
        private int infraredHeight;

        public DisplayMode DisplayMode
        {
            get;
            set;
        }
        public ImageSource ImageSource
        {
            get
            {
                if (DisplayMode == DisplayMode.COLOR)
                {
                    return this.imageSource;
                }
                else
                {
                    return this.infraredSource;
                }
            }
        }

        /// <summary>
        /// Handles the infrared frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            bool infraredFrameProcessed = false;

            // InfraredFrame is IDisposable
            using (InfraredFrame infraredFrame = e.FrameReference.AcquireFrame())
            {
                if (infraredFrame != null)
                {
                    
                    // the fastest way to process the infrared frame data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer infraredBuffer = infraredFrame.LockImageBuffer())
                    {
                        // verify data and write the new infrared frame data to the display bitmap
                        if (((infraredFrame.FrameDescription.Width * infraredFrame.FrameDescription.Height) == (infraredBuffer.Size / infraredFrame.FrameDescription.BytesPerPixel)) &&
                            (infraredFrame.FrameDescription.Width == this.infraredSource.PixelWidth) && (infraredFrame.FrameDescription.Height == this.infraredSource.PixelHeight))
                        {
                            this.ProcessInfraredFrameData(infraredBuffer.UnderlyingBuffer, infraredBuffer.Size);
                            infraredFrameProcessed = true;
                        }
                    }
                    
                }
            }

            if (infraredFrameProcessed)
            {
                this.RenderInfraredPixels();
            }
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the InfraredFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the infraredFrameData pointer.
        /// </summary>
        /// <param name="infraredFrameData">Pointer to the InfraredFrame image data</param>
        /// <param name="infraredFrameDataSize">Size of the InfraredFrame image data</param>
        private unsafe void ProcessInfraredFrameData(IntPtr infraredFrameData, uint infraredFrameDataSize)
        {
            // infrared frame data is a 16 bit value
            ushort* frameData = (ushort*)infraredFrameData;

            // convert depth to a visual representation
            // 2 => Byts per pixel for infrared frame
            for (int i = 0; i < (int)(infraredFrameDataSize / 2); ++i)
            {
                this.infraredPixels[i] = frameData[i];
            }
        }



        public ColorFrameRenderer(int frameWidth, int frameHeight, int jointFrameWidth, int jointFrameHeight, 
            int infraredFrameWidth, int infraredFrameHeight)
        {
            // allocate space to put the pixels to be rendered
            this.colorPixels = new byte[frameWidth * frameHeight * this.bytesPerPixel];
            this.displayWidth = frameWidth;
            this.displayHeight = frameHeight;
            this.jointDisplayWidth = jointFrameWidth;
            this.jointDisplayHeight = jointFrameHeight;
            this.infraredWidth = infraredFrameWidth;
            this.infraredHeight = infraredFrameHeight;

            //set voice synth to Hazel
            this.voiceSynth.SelectVoice("Microsoft Hazel Desktop");

            infraredPixels = new ushort[infraredFrameWidth * infraredFrameHeight];

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // create the bitmap to display
            this.imageSource = new WriteableBitmap(frameWidth, frameHeight, 96.0, 96.0, PixelFormats.Pbgra32, null);
            this.infraredSource = new WriteableBitmap(infraredFrameWidth, infraredFrameHeight, 96.0, 96.0, PixelFormats.Gray16, null);
        }


        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource InfraredSource
        {
            get
            {
                return this.infraredSource;
            }
        }

        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    // verify data and write the new color frame data to the display bitmap
                    if ((colorFrameDescription.Width == this.imageSource.PixelWidth) && (colorFrameDescription.Height == this.imageSource.PixelHeight))
                    {
                        if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                        {
                            colorFrame.CopyRawFrameDataToArray(this.colorPixels);
                        }
                        else
                        {
                            colorFrame.CopyConvertedFrameDataToArray(this.colorPixels, ColorImageFormat.Bgra);
                        }
                    }
                    this.RenderColorPixels();
                }
            }

        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderColorPixels()
        {
            this.imageSource.WritePixels(
                new Int32Rect(0, 0, this.imageSource.PixelWidth, this.imageSource.PixelHeight),
                this.colorPixels,
                this.imageSource.PixelWidth * (int)this.bytesPerPixel,
                0);
        }

        /// <summary>
        /// Renders the pixels to display into the writeableBitmap.
        /// </summary>
        private void RenderInfraredPixels()
        {
            this.infraredSource.WritePixels(
                new Int32Rect(0, 0, this.infraredSource.PixelWidth, this.infraredSource.PixelHeight),
                this.infraredPixels,
                this.infraredSource.PixelWidth * bytesPerInfraredPixel,
                0);
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        public void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, Color drawingColor)
        {

            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingColor);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Color drawColor = this.trackedJointColor;
                

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawColor = this.trackedJointColor;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawColor = this.trackedJointColor; 
                }

                if (drawColor != null)
                {
                    this.imageSource.DrawEllipse((int)jointPoints[jointType].X, (int)jointPoints[jointType].Y, (int)jointPoints[jointType].X + (int)JointThickness, (int)jointPoints[jointType].Y + (int)JointThickness, drawColor);
                }
            }
        }

        public void DrawBodies(Body[] bodies, CoordinateMapper coordinateMapper, DisplayMode displayMode, int? targetIndex)
        {
            // Draw a transparent background to set the render size
            this.imageSource.DrawRectangle(0, 0, this.displayWidth, this.displayHeight, Color.FromArgb(255, 128, 128, 128));
            int count = 0;
            bool trackedBody = false;
            foreach (Body body in bodies)
            {
                if (body.IsTracked)
                {
                    this.DrawClippedEdges(body);

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

                        DepthSpacePoint depthSpacePoint = coordinateMapper.MapCameraPointToDepthSpace(position);
                        

                        //convert joint space to color space so that we can draw skeletons on top of color feed
                        if (displayMode == DisplayMode.COLOR) {
                            jointPoints[jointType] = new Point((depthSpacePoint.X / this.jointDisplayWidth) * this.displayWidth, (depthSpacePoint.Y / this.jointDisplayHeight) * this.displayHeight);
                        }
                        else
                        {
                            jointPoints[jointType] = new Point(depthSpacePoint.X / this.jointDisplayWidth,depthSpacePoint.Y / this.jointDisplayHeight);
                        }

                        //check if this is the skeleton that has been targeted by system
                        //if it is we'll draw a big red circle on the skelton chest
                        if (targetIndex != null && targetIndex == count)
                        {
                            if (jointType == JointType.Head)
                            {
                                var joint = jointPoints[jointType];

                                if (!hasValidTracking)
                                {
                                    voiceSynth.SpeakAsync("Target Aquired!");
                                    hasValidTracking = true;
                                }

                                trackedBody = true;
                                if (displayMode == DisplayMode.COLOR)
                                {
                                    this.imageSource.FillEllipse((int)joint.X, (int)joint.Y, (int)joint.X + 50, (int)joint.Y + 50, Color.FromArgb(128, 255, 0, 0));
                                }
                                else if(displayMode == DisplayMode.INFRARED)
                                {
                                    //we can't draw on a grayscale 16bit bitmap. So TODO on implementing the drawing here
                                    //var pgra32Version = BitmapFactory.ConvertToPbgra32Format(this.infraredSource);
                                    //pgra32Version.FillEllipse((int)joint.X, (int)joint.Y, (int)joint.X + 50, (int)joint.Y + 50, Colors.Black);
                                    
                                }


                            }
                        }
                    }

                    this.DrawBody(joints, jointPoints, this.trackedJointColor);

                    this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft]);
                    this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight]);

                    count++;
                }

                
            }

            if (!trackedBody)
            {
                hasValidTracking = false;
            }
            // prevent drawing outside of our render area
            //this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
        }
        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        public void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, Color drawingColor)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Color drawColor = this.inferredBoneColor;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawColor = drawingColor;
            }
            //the numbers here shouldn't be larger than an int for our use
            this.imageSource.DrawLine((int)jointPoints[jointType0].X, (int)jointPoints[jointType0].Y, (int)jointPoints[jointType1].X, (int)jointPoints[jointType1].Y, drawColor);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        public void DrawHand(HandState handState, Point handPosition)
        {
            switch (handState)
            {
                case HandState.Closed:
                    this.imageSource.DrawEllipse((int)handPosition.X, (int)handPosition.Y, (int)handPosition.X + (int)HandSize, (int)handPosition.Y + (int)HandSize, this.handClosedColor);
                    break;

                case HandState.Open:
                    this.imageSource.DrawEllipse((int)handPosition.X, (int)handPosition.Y, (int)handPosition.X + (int)HandSize, (int)handPosition.Y + (int)HandSize, this.handOpenColor);
                    break;

                case HandState.Lasso:
                    this.imageSource.DrawEllipse((int)handPosition.X, (int)handPosition.Y, (int)handPosition.X + (int)HandSize, (int)handPosition.Y + (int)HandSize, this.handLassoColor);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        public void DrawClippedEdges(Body body)
        {

            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                this.imageSource.DrawRectangle(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness, Color.FromRgb(255, 0, 0));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                this.imageSource.DrawRectangle(0, 0, this.displayWidth, ClipBoundsThickness, Color.FromRgb(255, 0, 0));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                this.imageSource.DrawRectangle(0, 0, ClipBoundsThickness, this.displayHeight, Color.FromRgb(255, 0, 0));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                this.imageSource.DrawRectangle(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight, Color.FromRgb(255, 0, 0));

            }
        }

    }
}

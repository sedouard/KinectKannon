using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Windows;
using KinectKannon;
namespace KinectKannon.Rendering
{
    public struct HudRenderingParameters
    {
        public bool SystemReady {get;set;}
        public string FrameRate { get; set; }
        public string StatusText { get; set; }
        public string CannonX { get; set; }
        public string CannonY { get; set; }

        
        public TrackingMode TrackingMode { get; set; }
    }
    class HudRenderer
    {
        /// <summary>
        /// The drawing group for the HUD
        /// </summary>
        private DrawingGroup drawingGroup;
        private DrawingImage drawingImage;
        private int displayWidth;
        private int displayHeight;

        public ImageSource ImageSource
        {
            get
            {
                return drawingImage;
            }
        }



        public HudRenderer(DrawingGroup drawGroup, DrawingImage drawImage, int width, int height)
        {
            drawingGroup = drawGroup;
            drawingImage = drawImage;
            displayHeight = height;
            displayWidth = width;
        }

        public void RenderHud(HudRenderingParameters renderingParams)
        {
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                var statusBrush = Brushes.Green;
                if (!(renderingParams.SystemReady))
                {
                    statusBrush = Brushes.Red;
                }

                //frame rate
                RenderHudText(dc, "FPS: " + renderingParams.FrameRate, Brushes.White, 20, new Point(1800, 0));
                //System Status
                RenderHudText(dc, "System Status: " + renderingParams.StatusText, statusBrush, 20, new System.Windows.Point(0, 0));
                //Cannon Properties
                RenderHudText(dc, "Cannon Status: ", Brushes.White, 40, new Point(0, 120));
                RenderHudText(dc, "X Position: " + renderingParams.CannonX, Brushes.YellowGreen, 20, new Point(0, 180));
                RenderHudText(dc, "Y Position: " + renderingParams.CannonY, Brushes.YellowGreen, 20, new Point(0, 200));

                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), new Pen(), new Rect(10, 300, 300, 300));
                RenderHudText(dc, "Tracking Mode", Brushes.White, 20, new Point(80, 310));
                RenderHudText(dc, renderingParams.TrackingMode.ToString(), Brushes.YellowGreen, 40, new Point(70, 420));
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
    }
}

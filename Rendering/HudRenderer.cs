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
        public bool FiringSafety { get; set; }
        public string FiringSafetyText { get; set; }
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
                var infoBrush = Brushes.YellowGreen;
                if (!(renderingParams.SystemReady))
                {
                    statusBrush = Brushes.Red;
                }

                //frame rate
                RenderHudText(dc, "FPS: " + renderingParams.FrameRate, Brushes.White, 20, new Point(1800, 0));
                //System Status
                RenderHudText(dc, "System Status: " + renderingParams.StatusText, statusBrush, 20, new System.Windows.Point(0, 0));
                //Cannon Properties

                //Safety Status
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), new Pen(), new Rect(0, 1150, 875, 65));
                RenderHudText(dc, "Safety Status: ", infoBrush, 40, new System.Windows.Point(0, 1155));

                if (renderingParams.FiringSafety)
                {
                    RenderHudText(dc, renderingParams.FiringSafetyText, infoBrush, 40, new Point(310, 1155));
                }
                else {
                    RenderHudText(dc, renderingParams.FiringSafetyText, statusBrush, 40, new Point(310, 1155));
                }

                //Canon Status: XY Area 
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), new Pen(), new Rect(0, 1100, 490, 50));
                RenderHudText(dc, "Cannon Status: ", Brushes.YellowGreen, 40, new Point(0, 1100));
                RenderHudText(dc, "X Position: " + renderingParams.CannonX, infoBrush, 20, new Point(320, 1105));
                RenderHudText(dc, "Y Position: " + renderingParams.CannonY, infoBrush, 20, new Point(320, 1120));

                //The Tracking Mode Area 
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), new Pen(), new Rect(1800, 1150, 180, 65));
                RenderHudText(dc, "Tracking Mode", new SolidColorBrush(Color.FromArgb(178, 48, 9, 0)), 20, new Point(1800, 1150));
                RenderHudText(dc, renderingParams.TrackingMode.ToString(), infoBrush, 40, new Point(1800, 1170));
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
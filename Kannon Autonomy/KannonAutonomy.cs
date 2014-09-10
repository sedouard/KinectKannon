using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using KinectKannon;
using KinectKannon.Rendering;

namespace KinectKannon.Autonomy
{
    /// <summary>
    /// Handles calculation of X and Y positions if the SpineMid Joint for an Indexed Body during Skeletal Tracking Mode.
    /// </summary>
    class KannonAutonomy
    {
        private double originXDist;
        private double originYDist;
        private double originTheta;

        public KannonAutonomy()
        {
            originXDist = 0;
            originYDist = 0;
            originTheta = 0;
        }
        public double getXDist
        {
            get
            {
                return originXDist;
            }
        }
        public double getYDist
        {
            get
            {
                return originYDist;
            }
        }
        public double getTheta(Double xDist, Double yDist)
        {
            if (xDist != 0 && yDist != 0)
            {
                originTheta = Math.Atan(yDist / xDist) * 180 / Math.PI;
            }
            return originTheta;
        }
        public void skeletalAutonomy(Body[] targetBodies, int? indexNumb)
        {
            int x = 0;
            foreach (Body body in targetBodies)
            {
                if (body.IsTracked && indexNumb == x)
                {
                    Joint spine = body.Joints[JointType.Neck];
                    originXDist = spine.Position.X;
                    originYDist = spine.Position.Y;
                    getTheta(originXDist, originYDist);
                    Console.Write("This is X" + originXDist + "This is Y" + originYDist);
                    x++;
                }
            }
        }
        public void audioAutonomy(float audioAngle)
        {
            //Do Something

        }
    }



}


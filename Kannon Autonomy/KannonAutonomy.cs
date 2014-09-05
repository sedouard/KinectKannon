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
        private double SkeletalXDist;
        private double SkeletalYDist;
        private double SkeletalTheta;

        public KannonAutonomy()
        {
            SkeletalXDist = 0;
            SkeletalYDist = 0;
            SkeletalTheta = 0;
        }
        public double getXDist
        {
            get
            {
                return SkeletalXDist;
            }
        }
        public double getYDist
        {
            get
            {
                return SkeletalYDist;
            }
        }
        public double getTheta(Double xDist, Double yDist)
        {
            if (xDist != 0 && yDist != 0)
            {
                SkeletalTheta = Math.Atan(yDist / xDist) * 180 / Math.PI;
            }
            return SkeletalTheta;
        }
        public void skeletalAutonomy(Body[] targetBodies, int? indexNumb)
        {
            int x = 0;
            foreach (Body body in targetBodies)
            {
                if (body.IsTracked && indexNumb == x)
                {
                    Joint spine = body.Joints[JointType.Neck];
                    SkeletalXDist = spine.Position.X;
                    SkeletalYDist = spine.Position.Y;
                    getTheta(SkeletalXDist, SkeletalYDist);
                    Console.Write("This is X" + SkeletalXDist + "This is Y" + SkeletalYDist);
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


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
        //Return the Calculated X, y and theta of selected object
        private double originXDist;
        private double originYDist;
        /// <summary>
        /// Range for screen in respect to X Plane
        /// </summary>
        public const double RANGE_X=1;
        /// <summary>
        /// Range for screen in respect to Y Plane
        /// </summary>
        public const double RANGE_Y =1;

        //Initializes the distances to be 0
       public KannonAutonomy()
        {
            originXDist = 0;
            originYDist = 0;
        }
        //Returns the horizontal distance from the object to the origin
        public double getXDist
        {
            get
            {
                return originXDist;
            }
        }
        //Returns the virtical distance from the object to the origin
        public double getYDist
        {
            get
            {
                return originYDist;
            }
        }
        // This method takes in an array of bodies and returns the x and y positions 
        // of the selected body that is passed by user (indexNumb)
        public void skeletalAutonomy(Body[] targetBodies, int? indexNumb)
        {
            int x = 0;
            // Iterate through the array of bodies
            foreach (Body body in targetBodies)
            {
                // Check if bod is tracked and the index position in array is the same as inputted indexBumb
                if (body.IsTracked && indexNumb == x)
                {
                    Joint spine = body.Joints[JointType.Head];
                    originXDist = spine.Position.X;
                    originYDist = spine.Position.Y;
                    Console.Write("This is X" + originXDist + "This is Y" + originYDist);
                    x++;
                }
                //If a body entity is not tracked, return to default values
                else if(!body.IsTracked && indexNumb == x)
                {
                    originXDist = 0;
                    originYDist = 0;
                }
            }
        }
    }



}


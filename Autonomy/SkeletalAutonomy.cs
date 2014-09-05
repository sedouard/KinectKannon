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

   class SkeletalAutonomy
   {
       private double SkeletalXDist;
       private double SkeletalYDist;
       private double SkeletalZDist;
       public SkeletalAutonomy()
       {
           SkeletalXDist = 0;
           SkeletalYDist = 0;
           SkeletalZDist = 0;
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
       public double getZDist
       {
           get
           {
               return SkeletalZDist;
           }
       }
       public void SkeletonAutonomy(Body[] targetBodies, int? indexNumb)
       {
           if (targetBodies != null)
           {
               
               int count = indexNumb ?? default(int);
               Body trackedBody = targetBodies[count];
               if (targetBodies[count].IsTracked)
               {
                   Joint spineMid = targetBodies[count].Joints[JointType.SpineMid];

                   SkeletalXDist = spineMid.Position.X;
                   SkeletalYDist = spineMid.Position.Y;
                   SkeletalZDist = spineMid.Position.Z;
                   

                   Console.Write("This is X", SkeletalXDist, "This is Y", SkeletalYDist, "This is Z", SkeletalZDist);
               }
           }
       
       }
   
   
   }



}


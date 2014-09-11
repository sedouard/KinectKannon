using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectKannon;
using KinectKannon.Control;
using System.Windows.Input;
using System.Windows;
using J2i.Net.XInputWrapper;
namespace KinectKannon.Control
{
    public class UserInputControl
    {
        private static bool s_PanTiltTooFarDown = false;
        private static bool s_PanTiltTooFarUp = false;
        private static bool s_PanTiltTooFarLeft = false;
        private static bool s_PanTiltTooFarRight = false;
        private const uint PAN_TILT_SPEED_LIMIT = 60;
        private const int XINPUT_RESTING_X = -1200;
        private const int XINPUT_MAX_X = 32768;
        private const int XINPUT_MAX_Y = 32768;
        private const int XINPUT_RESTING_Y = -2631;
        private const int VIBRATION_INTENSITY = 50;
        
   

        /// <summary>
        /// The time the valve will remain open on a single fire
        /// </summary>
        private const uint FIRING_PIN_TIME_MSEC = 300;

        private enum UserAction {

            PAN_LEFT,
            PAN_RIGHT,
            PAN_UP,
            PAN_DOWN,
            TRACKING_SKELETAL,
            TRACKING_MANUAL,
            TRACKING_AUDIBLE,
            SAFTEY_TOGGLE
        }

        //Static for now... not sure if we actually need to make this class instatiable
        public static async Task HandleInput(MainWindow mainWindow, PanTiltController panTilt, FiringController firingController, 
            Key? key, XboxController handHeldController){
            ////////////////////////////////////////////////////////////////////////////
            //Manual Control Logic
            ////////////////////////////////////////////////////////////////////////////
            if (panTilt.IsReady)
            {
            try
            {
                //Pan Up
                    if ((key == System.Windows.Input.Key.Down || handHeldController.LeftThumbStick.Y < 0) && !s_PanTiltTooFarDown && mainWindow.TrackingMode == TrackingMode.MANUAL)
                {
                    if (mainWindow.CannonYVelocity <= PAN_TILT_SPEED_LIMIT)
                    {
                        mainWindow.CannonYVelocity += 20;
                    }
                    //set too far to false. if its stil too far the next key event handler will set to true
                    s_PanTiltTooFarUp = false;
                    panTilt.PanY(mainWindow.CannonYVelocity);
                }
                else if (key == System.Windows.Input.Key.Up && !s_PanTiltTooFarUp && mainWindow.TrackingMode == TrackingMode.MANUAL)
                {
                    if (mainWindow.CannonYVelocity >= -1 * PAN_TILT_SPEED_LIMIT)
                    {
                        mainWindow.CannonYVelocity -= 20;
                    }
                    //set too far to false. if its stil too far the next key event handler will set to true
                    s_PanTiltTooFarDown = false;
                    panTilt.PanY(mainWindow.CannonYVelocity);
                }
                else if (key == System.Windows.Input.Key.Right && !s_PanTiltTooFarRight && mainWindow.TrackingMode == TrackingMode.MANUAL)
                {
                    if (mainWindow.CannonXVelocity >= -1 * PAN_TILT_SPEED_LIMIT)
                    {
                        mainWindow.CannonXVelocity -= 20;
                    }
                    //set too far to false. if its stil too far the next key event handler will set to true
                    s_PanTiltTooFarLeft = false;
                    panTilt.PanX(mainWindow.CannonXVelocity);
                }
                else if (key == System.Windows.Input.Key.Left && !s_PanTiltTooFarLeft && mainWindow.TrackingMode == TrackingMode.MANUAL)
                {
                    if (mainWindow.CannonXVelocity <= PAN_TILT_SPEED_LIMIT)
                    {
                        mainWindow.CannonXVelocity += 20;
                    }
                    //set too far to false. if its stil too far the next key event handler will set to true
                    s_PanTiltTooFarRight = false;
                    panTilt.PanX(mainWindow.CannonXVelocity);
                }
            }
            //anything goes wrong, stop pantilt and crash the app
            catch (Exception ex)
            {
                panTilt.Disengage();
                throw ex;
            }
                
        }

        //Static for now... not sure if we actually need to make this class instatiable
        public static async Task HandleInput(MainWindow mainWindow, PanTiltController panTilt, FiringController firingController, 
            Key? key, XboxController handHeldController){
            ////////////////////////////////////////////////////////////////////////////
            //Manual Control Logic
            ////////////////////////////////////////////////////////////////////////////
            if (panTilt.IsReady)
            {
                if (key != null)
                {
                    await HandlePanTilt(mainWindow, panTilt, firingController, (Key)key);
                }
                else
                {
                    await HandleXboxInputPanTilt(mainWindow, panTilt, firingController, handHeldController);
                }
            }
            
            if (key == Key.NumPad1 || key == Key.D1 || handHeldController.IsXPressed)
            {
                mainWindow.TrackingMode = TrackingMode.MANUAL;
                //mainWindow.AudioViewBox.Visibility = Visibility.Hidden;
            }
            else if (key == Key.NumPad2 || key == Key.D2 || handHeldController.IsAPressed)
            {
                mainWindow.TrackingMode = TrackingMode.SKELETAL;
                //mainWindow.AudioViewBox.Visibility = Visibility.Hidden;
            }
            else if (key == Key.NumPad3 || key == Key.D3 || handHeldController.IsBPressed)
            {
                mainWindow.TrackingMode = TrackingMode.AUDIBLE;
                //mainWindow.AudioViewBox.Visibility = Visibility.Visible;
            }
            //The ordering rational of the key assignments is based on 
            //the bottom row of the keyboard
            else if (mainWindow.TrackingMode == TrackingMode.SKELETAL &&
                key == Key.Z)
            {
                mainWindow.RequestedTrackedSkeleton = SkeletalLetter.A;
            }
            else if (mainWindow.TrackingMode == TrackingMode.SKELETAL &&
                key == Key.X)
            {
                mainWindow.RequestedTrackedSkeleton = SkeletalLetter.B;
            }
            else if (mainWindow.TrackingMode == TrackingMode.SKELETAL &&
                key == Key.C)
            {
                mainWindow.RequestedTrackedSkeleton = SkeletalLetter.C;
            }
            else if (mainWindow.TrackingMode == TrackingMode.SKELETAL &&
                key == Key.V)
            {
                mainWindow.RequestedTrackedSkeleton = SkeletalLetter.D;
            }
            else if (mainWindow.TrackingMode == TrackingMode.SKELETAL &&
                key == Key.B)
            {
                mainWindow.RequestedTrackedSkeleton = SkeletalLetter.E;
            }
            else if (mainWindow.TrackingMode == TrackingMode.SKELETAL &&
                key == Key.N)
            {
                mainWindow.RequestedTrackedSkeleton = SkeletalLetter.F;
            }

            if (handHeldController.IsDPadRightPressed)
            {
                mainWindow.RequestedTrackedSkeleton++;
                //Console.WriteLine(mainWindow.RequestedTrackedSkeleton);
            }
            if (handHeldController.IsDPadLeftPressed)
            {
                mainWindow.RequestedTrackedSkeleton--;
            }
            if (handHeldController.IsDPadDownPressed)
            {
                mainWindow.RequestedTrackedSkeleton = SkeletalLetter.A;
            }
            /////////////////////////////////////////////////////////
            //Rage Safteys - Makey Makey Board Provides These Events
            /////////////////////////////////////////////////////////
            try
            {
                //mainWindow.Cannon is too high
                if (key == Key.A && !s_PanTiltTooFarUp)
                {
                    s_PanTiltTooFarUp = true;
                    mainWindow.CannonYVelocity = 0;
                    panTilt.PanY(mainWindow.CannonYVelocity);
                }
                //mainWindow.Cannon is too low
                else if (key == Key.S && !s_PanTiltTooFarDown)
                {
                    s_PanTiltTooFarDown = true;
                    mainWindow.CannonYVelocity = 0;
                    panTilt.PanY(mainWindow.CannonYVelocity);
                }
                //mainWindow.Cannon is too far right
                else if (key == Key.D && !s_PanTiltTooFarRight)
                {
                    s_PanTiltTooFarRight = true;
                    mainWindow.CannonXVelocity = 0;
                    panTilt.PanX(mainWindow.CannonXVelocity);
                }
                //mainWindow.Cannon is too far left
                else if (key == Key.F && !s_PanTiltTooFarLeft)
                {
                    s_PanTiltTooFarLeft = true;
                    mainWindow.CannonXVelocity = 0;
                    panTilt.PanX(mainWindow.CannonXVelocity);
                }
            }
            //if anything at all goes wrong we gotta stop the pantilt and crash the app
            catch(Exception ex)
            {
                panTilt.Disengage();
                throw ex;
            }

            if (key == Key.P || (handHeldController.IsLeftShoulderPressed & handHeldController.IsRightShoulderPressed))
            {
                //toggle the safety
                firingController.VirtualSafetyOn = !firingController.VirtualSafetyOn;
                //handHeldController.Vibrate(40.0, 40.0, 10.0);
            }
            //MAYBE WE SHOULD PICK A HARDER TO PRESS KEY THAN THE SPACE BAR?
            if (key == Key.Space || (handHeldController.RightTrigger > 250 && handHeldController.LeftTrigger > 250))
            {
                handHeldController.Vibrate(VIBRATION_INTENSITY, VIBRATION_INTENSITY, new TimeSpan(1));
                await firingController.Fire(300);
            }

            
        }
    }
}

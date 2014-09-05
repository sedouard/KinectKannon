using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Phidgets;
using Phidgets.Events;
namespace KinectKannon.Control
{
    public class PanTiltController
    {
        public delegate void PanTiltReadyEventHandler(PanTiltController sender, PanTiltReadyArgs args);
        public event PanTiltReadyEventHandler PanTiltReady;
        
        public bool IsReady{
            get;
            set;
        }

        public bool Engaged{
            get;
            set;
        }

        //2 Servo slots because this system will always have 2
        private AdvancedServo m_ServoController = new AdvancedServo();
        private AdvancedServoServo[] m_Servos = new AdvancedServoServo[2];
        private static PanTiltController s_Singleton;
        private const ServoServo.ServoType SERVO_TYPE = ServoServo.ServoType.HITEC_HS322HD;
        private double ZERO_MOVEMENT_POINT = 120.0;

        /// <summary>
        /// Returns the singleton instance. We will always have only 1 pan/tilt controller
        /// </summary>
        /// <returns></returns>
        public static PanTiltController GetOrCreatePanTiltController()
        {
            if (s_Singleton == null)
            {
                s_Singleton = new PanTiltController();
            }

            return s_Singleton;
        }

        /// <summary>
        /// Creates new PanTilt Controller, so this hide this constructor
        /// </summary>
        private PanTiltController()
        {
            m_ServoController.Attach +=m_ServoController_Attach;

            
        }

        /// <summary>
        /// Trys to initialize the controller. Returns an exception (no throw). Ensure
        /// you are registered to the PanTiltReady event prior to calling
        /// </summary>
        /// <returns></returns>
        public PanTiltControllerException TryInitialize(){

            try{
                m_ServoController.open();

            }
            catch(PhidgetException e){
                return new PanTiltControllerException("Could not open connection to servo controller. Chec driver installation. Is it attached?",e);
            }

            return null;
        }

        public void Engage (){
            if(!IsReady){
                ThrowNotInitialized();
            }

            //engage all servos
            m_Servos[0].Engaged = true;
            m_Servos[1].Engaged = true;
        }

        public void Disengage()
        {
            if (!IsReady)
            {
                ThrowNotInitialized();
            }

            //engage all servos
            m_Servos[0].Engaged = false;
            m_Servos[1].Engaged = false;
        }

        public void PanLeft(double degrees){
            degrees *= -1;
            
            Pan(degrees, m_Servos[0]);
        }

        public void PanRight(double degrees){
            
            Pan(degrees, m_Servos[0]);
        }

        private void Pan(double degrees, AdvancedServoServo servo){

            if(!IsReady){
                ThrowNotInitialized();
            }
            
            if(Math.Abs(degrees) > ZERO_MOVEMENT_POINT){
                throw new PanTiltControllerException("Cannot specify a movement value with magnitude greater than " + ZERO_MOVEMENT_POINT, null);
            }

            //LEFT/RIGHT Servo is index 0 in the m_Servos array
            servo.Position = ZERO_MOVEMENT_POINT + degrees;
        }

        public void PanUp(double degrees){
            Pan(degrees, m_Servos[1]);
        }

        public void PanDown(double degrees){
            degrees *= -1;
            Pan(degrees, m_Servos[1]);
        }

        private void ThrowNotInitialized(){
            throw new PanTiltControllerException("You must Initialize this class before calling this method", null);
        }
        void m_ServoController_Attach(object sender, AttachEventArgs e)
        {
            //add servos 0 and 1 (as listed on the board) to the array
            int servoCount = 0;
            foreach (var s in m_ServoController.servos)
            {
                //this should definatley be true
                Debug.Assert(s is AdvancedServoServo, "Expected AdvancedServoServo, something is wrong with the Phidgets library");

                //we only care about the first 2 servos on the board
                if (servoCount >= 2)
                {
                    break;
                }

                m_Servos[servoCount] = (AdvancedServoServo)s;
                servoCount++;
            }

            //engage all servos.
            foreach (AdvancedServoServo s in m_Servos)
            {
                s.Engaged = true;

            }

 	        if(null != PanTiltReady){
                IsReady = true;
                PanTiltReady(this, new PanTiltReadyArgs());
            }

            
        }


    }

    /// <summary>
    /// The event arguments sent when PanTiltController is correctly initalized
    /// </summary>
    public class PanTiltReadyArgs : EventArgs
    {

    }

    /// <summary>
    /// Exception class for all things related to the Pan/Tilt controller
    /// </summary>
    public class PanTiltControllerException : Exception{

        public PanTiltControllerException(string message, Exception inner) :
        base(message, inner)
        {
        }
    }
}
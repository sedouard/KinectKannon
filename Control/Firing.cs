using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phidgets;
using Phidgets.Events;
namespace KinectKannon.Control
{
    /// <summary>
    /// Super Duper simple class over the relay that controls the firing valve
    /// </summary>
    public class FiringController
    {
        private static FiringController s_Instance;
        public delegate void FiringControlEventHandler(FiringController sender, FiringControllerReadyArgs args);
        public event FiringControlEventHandler FiringControlReady;
        private InterfaceKit m_RelayBoard = new InterfaceKit();
        private bool isReady = false;
        
        public bool IsReady
        {
            get
            {
                return isReady;
            }
        }
        /// <summary>
        /// This represents a safety. You must set this to true in order to turn the controller 'hot'
        /// </summary>
        public bool VirtualSafetyOn
        {
            get;
            set;
        }

        public bool IsFiring()
        {
            if (!IsReady)
            {
                throw new FiringControllerException("You must intialize first with TryInitialize before attempting to use the FiringControl", null);
            }

            return this.m_RelayBoard.outputs[0];
        }
        public static FiringController GetOrCreateFiringController()
        {
            if (s_Instance == null)
            {
                s_Instance = new FiringController();
            }

            return s_Instance;
        }
        public FiringControllerException TryInitialize()
        {
            try
            {
                m_RelayBoard.open();
                return null;
            }
            catch (PhidgetException e)
            {
                return new FiringControllerException("An error occured opening connection to relay board.", e);
            }
        }
        private FiringController()
        {
            VirtualSafetyOn = true;
            m_RelayBoard.Attach += m_RelayBoard_Attach;
        }

        void m_RelayBoard_Attach(object sender, AttachEventArgs e)
        {
            isReady = true;
            if (FiringControlReady != null)
            {
                FiringControlReady(this, new FiringControllerReadyArgs());
            }
        }

        public async Task Fire(int miliseconds)
        {
            if (!IsReady)
            {
                throw new FiringControllerException("You must intialize first with TryInitialize before attempting to use the FiringControl", null);
            }

            if (!VirtualSafetyOn)
            {
                //open the firing valve
                this.m_RelayBoard.outputs[0] = true;
                //Create a delay task
                var t = System.Threading.Tasks.Task.Delay(miliseconds);
                //await the delay task before turning the valve off
                //this won't block the calling thread
                await t.ContinueWith((completedDelay) =>
                {
                    this.m_RelayBoard.outputs[0] = false;
                });
            }
        }
    }

    /// <summary>
    /// The event arguments sent when FiringController is correctly initalized
    /// </summary>
    public class FiringControllerReadyArgs : EventArgs
    {

    }

    /// <summary>
    /// Exception class for all things related to the FiringController controller
    /// </summary>
    public class FiringControllerException : Exception
    {

        public FiringControllerException(string message, Exception inner) :
            base(message, inner)
        {
        }
    }
}

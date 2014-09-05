using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using KinectKannon.Control;
namespace KinectKannon.Tests
{
    [TestClass]
    public class PanTiltTests
    {
        [TestMethod]
        public void PanUninitialized()
        {
            PanTiltController controller = PanTiltController.GetOrCreatePanTiltController();

            try
            {
                controller.PanDown(20);
                Assert.Fail("Failed to throw on Pan");
            }
            catch (PanTiltControllerException ex)
            {
                //we expect an exception
                Assert.IsNotNull(ex, "Exception object expected");
            }

            try
            {
                controller.PanUp(20);
                Assert.Fail("Failed to throw on Pan");
            }
            catch (PanTiltControllerException ex)
            {
                //we expect an exception
                Assert.IsNotNull(ex, "Exception object expected");
            }

            try
            {
                controller.PanLeft(20);
                Assert.Fail("Failed to throw on Pan");
            }
            catch (PanTiltControllerException ex)
            {
                //we expect an exception
                Assert.IsNotNull(ex, "Exception object expected");
            }

            try
            {
                controller.PanRight(20);
                Assert.Fail("Failed to throw on Pan");
            }
            catch (PanTiltControllerException ex)
            {
                //we expect an exception
                Assert.IsNotNull(ex, "Exception object expected");
            }

            
        }

        [TestMethod]
        public void PanNormalRange()
        {
            bool completed = false;
            PanTiltController controller = PanTiltController.GetOrCreatePanTiltController();

            PanTiltControllerException error = null;

            controller.PanTiltReady += (sender, args) =>
            {
                Console.WriteLine("Pan tilt ready");
                try
                {
                    sender.PanRight(20);
                    sender.PanUp(20);
                }
                catch (Exception e)
                {
                    Assert.Fail("Pan Tilt failed", e);
                    completed = true;
                }
                

                //Pause 2 seconds, then test servos other way
                Thread.Sleep(2000);

                try
                {
                    sender.PanLeft(20);
                    sender.PanDown(20);
                }
                catch (Exception e)
                {
                    Assert.Fail("Pan Tilt failed", e);
                    completed = true;
                }

                //Pause 2 seconds to confirm directional switch
                Thread.Sleep(2000);
                completed = true;
            };

            try
            {
                error = controller.TryInitialize();
            }
            catch (Exception e)
            {
                Assert.Fail("TryInitialize should not throw", e);
            }

            Assert.AreEqual(null, error, "An error occured intializing Pan/Tilt controller - " + error);
            
            while(!completed){

            }

            controller.Disengage();
        }
    }
}
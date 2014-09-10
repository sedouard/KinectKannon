using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectKannon.Control;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace KinectKannon.Tests
{
    [TestClass]
    public class FiringControlTests
    {
        bool isFiring = false;
        [TestMethod]
        public void Fire()
        {
            FiringController f = FiringController.GetOrCreateFiringController();

            f.FiringControlReady += f_FiringControlReady;
            f.TryInitialize();
            System.Threading.Thread.Sleep(1000);
            Assert.AreEqual(false, isFiring);
        }

        void f_FiringControlReady(FiringController sender, FiringControllerReadyArgs args)
        {
            sender.VirtualSafetyOn = false;
            sender.Fire(500);
            isFiring = sender.IsFiring();
        }
    }
}

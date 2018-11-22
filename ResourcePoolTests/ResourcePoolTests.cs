using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourcePool;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ResourcePoolTests
{
    [TestClass]
    public class ResourcePoolTests
    {
        [TestMethod]
        public void CheckoutReturnsItem()
        {
            using (var testPool = new ResourcePool<SmtpClient>(5, () => new SmtpClient(), s => s.Dispose()))
            {
                var client = testPool.CheckOut();

                Assert.IsNotNull(client);
                Assert.IsInstanceOfType(client, typeof(SmtpClient));
            }
        }

        [TestMethod]
        public async Task CheckoutAsyncReturnsItem()
        {
            using (var testPool = new ResourcePool<SmtpClient>(5, () => new SmtpClient(), s => s.Dispose()))
            {
                var client = await testPool.CheckOutAsync();

                Assert.IsNotNull(client);
                Assert.IsInstanceOfType(client, typeof(SmtpClient));
            }
        }


        [TestMethod]
        public async Task CheckoutAllowsCorrectThreadNum()
        {
            using (var testPool = new ResourcePool<SmtpClient>(1, () => new SmtpClient(), s => s.Dispose()))
            {
                var client = testPool.CheckOut();
                var delayTask = Task.Delay(100);

                var task = await Task.WhenAny(testPool.CheckOutAsync(), delayTask);

                Assert.IsNotNull(client);
                Assert.IsInstanceOfType(client, typeof(SmtpClient));
                Assert.AreEqual(task, delayTask);
            }
        }


        [TestMethod]
        public async Task CheckoutAsyncAllowsCorrectThreadNum()
        {
            using (var testPool = new ResourcePool<SmtpClient>(1, () => new SmtpClient(), s => s.Dispose()))
            {
                var client = await testPool.CheckOutAsync();
                var delayTask = Task.Delay(100);

                var task = await Task.WhenAny(testPool.CheckOutAsync(), delayTask);

                Assert.IsNotNull(client);
                Assert.IsInstanceOfType(client, typeof(SmtpClient));
                Assert.AreEqual(task, delayTask);
            }
        }


        [TestMethod]
        public async Task CheckinAllowsOtherThreadToCheckout()
        {
            using (var testPool = new ResourcePool<SmtpClient>(1, () => new SmtpClient(), s => s.Dispose()))
            {
                var client = await testPool.CheckOutAsync();
                testPool.CheckIn(client);
                var client2 = await testPool.CheckOutAsync();

                Assert.IsNotNull(client2);
                Assert.AreEqual(client, client2);
            }
        }

        [TestMethod]
        public async Task NullObjectCheckinAllowsOtherThreadToCheckout()
        {
            using (var testPool = new ResourcePool<SmtpClient>(1, () => new SmtpClient(), s => s.Dispose()))
            {
                var client = await testPool.CheckOutAsync();
                client = null;
                testPool.CheckIn(client);

                var client2 = await testPool.CheckOutAsync();

                Assert.IsNotNull(client2);
            }
        }

        /* This test will fail as if you keep a reference to the object after checking in 
           you can still mutate the state of the object. Not sure if theres any way to sove this 
           from the connection pool side */
        [TestMethod]
        public async Task CheckoutReferenceMaintainedDoesntEffectOtherInstance()
        {
            using (var testPool = new ResourcePool<SmtpClient>(1, () => new SmtpClient(), s => s.Dispose()))
            {
                var client = await testPool.CheckOutAsync();
                testPool.CheckIn(client);
                var client2 = await testPool.CheckOutAsync();

                client2.EnableSsl = false;
                client.EnableSsl = true;

                Assert.IsFalse(client2.EnableSsl);
            }
        }
    }
}

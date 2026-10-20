using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Input;
using NUnit.Framework;

namespace DoomsdayTunnels.Tests
{
    public class HapticsTests
    {
        [Test]
        public void EventsMapToTheRightBuzz()
        {
            Assert.AreEqual(Haptic.Heavy, Haptics.For(SimEventKind.Hit));
            Assert.AreEqual(Haptic.Light, Haptics.For(SimEventKind.Scrape));
            Assert.AreEqual(Haptic.Medium, Haptics.For(SimEventKind.BoostOn));
            Assert.AreEqual(Haptic.Warning, Haptics.For(SimEventKind.BoostDry));
            Assert.AreEqual(Haptic.Success, Haptics.For(SimEventKind.Clear));
            Assert.AreEqual(Haptic.Success, Haptics.For(SimEventKind.SectorClear));
            Assert.AreEqual(Haptic.Error, Haptics.For(SimEventKind.ShipLost));
            Assert.AreEqual(Haptic.None, Haptics.For(SimEventKind.Laser), "shots do not buzz: there are hundreds");
            Assert.AreEqual(Haptic.None, Haptics.For(SimEventKind.Popup));
            Haptics.Enabled = false; Haptics.Play(Haptic.Heavy); Haptics.Enabled = true; Haptics.Play(Haptic.None);   // silent off the phone, and never throws
        }
    }
}

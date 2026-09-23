#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;

namespace Sleepet.Tests
{
    public sealed class P0Tests
    {
        [Test]
        public void StorePersistsPreferencesWithoutSceneObjects()
        {
            string root = Path.GetFullPath(Path.Combine("Validation", "StoreTestData", Guid.NewGuid().ToString("N")));
            var store = new SleepetStore(root);
            var p = store.Preferences;
            p.reminderTime = "22:15";
            p.wakeTime = "07:45";
            p.petName = "Luna";
            Assert.IsTrue(store.SavePreferences(p));
            var loaded = new SleepetStore(root);
            Assert.AreEqual("22:15", loaded.Preferences.reminderTime);
            Assert.AreEqual("07:45", loaded.Preferences.wakeTime);
            Assert.AreEqual("Luna", loaded.Preferences.petName);
        }

        [Test]
        public void InactivityDetectorKeepsItsStateBetweenFrames()
        {
            var detector = new SleepDetector(2, 3);
            detector.Start();
            detector.Tick(1.9f);
            Assert.AreEqual(SleepState.Awake, detector.State);
            detector.Tick(0.1f);
            Assert.AreEqual(SleepState.LowAttention, detector.State);
            detector.Tick(3f);
            Assert.AreEqual(SleepState.LikelyAsleep, detector.State);
            detector.Activity();
            Assert.AreEqual(SleepState.Awake, detector.State);
            detector.End();
            Assert.AreEqual(SleepState.Ended, detector.State);
        }
    }
}
#endif

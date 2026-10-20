using AFirma.Core;
using NUnit.Framework;
using UnityEngine;

namespace AFirma.Tests
{
    public class SaveServiceTests
    {
        const string Key = "afirma.test.save";
        PlayerPrefsSaveService service;

        [SetUp]
        public void SetUp()
        {
            service = new PlayerPrefsSaveService(Key);
            service.DeleteSave();
        }

        [TearDown]
        public void TearDown() => service.DeleteSave();

        [Test]
        public void Load_WithoutSave_ReturnsNullAndNoNotice()
        {
            Assert.IsFalse(service.HasSave);
            Assert.IsNull(service.Load(out var notice));
            Assert.IsNull(notice);
        }

        [Test]
        public void SaveThenLoad_KeepsData()
        {
            var state = new GameState { money = 1234, fame = 3 };
            state.crew.Add(new ThiefState { id = "a", firstName = "Tião", type = ThiefType.Driver, level = 4 });
            service.Save(state);

            var loaded = service.Load(out var notice);
            Assert.IsNull(notice);
            Assert.AreEqual(1234, loaded.money);
            Assert.AreEqual(3, loaded.fame);
            Assert.AreEqual(ThiefType.Driver, loaded.crew[0].type);
            Assert.AreEqual(4, loaded.crew[0].level);
        }

        [Test]
        public void CorruptedMain_LoadsBackupWithNotice()
        {
            service.Save(new GameState { money = 10 });
            service.Save(new GameState { money = 20 }); // o save de 10 vira backup
            PlayerPrefs.SetString(Key, "{isso não é json");

            var loaded = service.Load(out var notice);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(10, loaded.money);
            Assert.IsNotNull(notice);
        }

        [Test]
        public void MainAndBackupCorrupted_ReturnsNullWithNotice()
        {
            PlayerPrefs.SetString(Key, "lixo");
            PlayerPrefs.SetString(Key + ".backup", "mais lixo");

            var loaded = service.Load(out var notice);

            Assert.IsNull(loaded);
            Assert.IsNotNull(notice);
        }

        [Test]
        public void SaveFromNewerVersion_IsRejected()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(new GameState { version = GameState.CurrentVersion + 1 }));
            var loaded = service.Load(out _);
            Assert.IsNull(loaded);
        }
    }
}

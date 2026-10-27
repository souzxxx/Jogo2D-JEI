using AFirma.HeistScene;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AFirma.Tests
{
    public class VehicleViewTests
    {
        VehicleView van;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("Van");
            van = go.AddComponent<VehicleView>();
            van.sr = go.AddComponent<SpriteRenderer>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(van.gameObject);

        [TestCase(1f / 1000f)]
        [TestCase(1f / 60f)]
        public void DriveTo_KeepsItsSpeedAtAnyFrameRate(float frame)
        {
            van.Park(Vector3.zero);
            van.DriveTo(new Vector3(-30f, 0f, 0f), 2f, 8f);
            for (float t = 0f; t < 3f; t += frame) van.Tick(frame);
            Assert.Less(van.transform.position.x, -20f, "Em 3 s a van passa de 20 unidades, com qualquer FPS.");
        }

        [Test]
        public void DriveTo_StaysOnThePixelGrid()
        {
            van.Park(Vector3.zero);
            van.DriveTo(new Vector3(-30f, 0f, 0f), 2f, 8f);
            for (int i = 0; i < 777; i++) van.Tick(1f / 1000f);
            float px = van.transform.position.x * 32f;
            Assert.AreEqual(Mathf.Round(px), px, 1e-4f, "A van fica alinhada no pixel.");
        }
    }
}

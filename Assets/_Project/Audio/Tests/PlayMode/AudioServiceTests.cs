using System.Collections;
using System.Linq;
using AFirma.Audio;
using AFirma.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AFirma.Tests
{
    public class AudioServiceTests
    {
        static string Audible() => GameObject.Find("[Áudio]").GetComponents<AudioSource>()
            .Single(s => s.loop && s.clip != null && s.volume > 0.01f).clip.name;

        [UnityTest]
        public IEnumerator Music_FollowsTheHeist()
        {
            Assert.IsInstanceOf<AudioService>(Services.Audio, "O installer tem que trocar o mock pelo serviço real.");
            Services.Audio.PlayMusic(MusicTrack.Menu);
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual("Menu", Audible());

            GameEvents.RaiseHeistStarted(null);
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual("Heist", Audible(), "Golpe começou: música do golpe.");

            GameEvents.RaiseSirenStarted(10f);
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual("Siren", Audible());

            GameEvents.RaiseHeistFinished(null);
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual("Hideout", Audible());
        }

        [Test]
        public void Volume_IsSaved()
        {
            float before = Services.Audio.GetVolume("music");
            Services.Audio.SetVolume("music", 0.4f);
            Assert.AreEqual(0.4f, PlayerPrefs.GetFloat("afirma.volume.music"), 0.001f);
            Services.Audio.SetVolume("music", 2f);
            Assert.AreEqual(1f, Services.Audio.GetVolume("music"), "Volume fica entre 0 e 1.");
            Services.Audio.SetVolume("music", before);
        }
    }
}

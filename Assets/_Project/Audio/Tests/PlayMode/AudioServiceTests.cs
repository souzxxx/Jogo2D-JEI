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

        [UnityTest]
        public IEnumerator HeistLayers_ComeInWithTheAlert()
        {
            Services.Audio.PlayMusic(MusicTrack.Menu);
            GameEvents.RaiseHeistStarted(null);
            yield return new WaitForSecondsRealtime(2f);
            GameEvents.RaiseAlertChanged(90f);
            yield return new WaitForSecondsRealtime(2.5f);
            var playing = GameObject.Find("[Áudio]").GetComponents<AudioSource>().Where(s => s.loop && s.clip != null && s.volume > 0.01f).ToList();
            CollectionAssert.AreEquivalent(new[] { "Heist", "Heist_1", "Heist_2" }, playing.Select(s => s.clip.name), "Alerta alto: base e as duas camadas.");
            Assert.That(playing.Select(s => s.clip.samples).Distinct().Count(), Is.EqualTo(1), "As camadas têm o tamanho da base, senão saem do tempo no loop.");
            GameEvents.RaiseHeistFinished(null);
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

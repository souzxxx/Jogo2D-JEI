using System.Collections;
using AFirma.Core;
using AFirma.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static AFirma.Tests.UiTestSupport;

namespace AFirma.Tests
{
    public class NoticesTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            UseMemorySave();
            Services.StartNewGame();
            SceneManager.LoadScene(SceneFlow.Hideout);
            yield return WaitForScene(SceneFlow.Hideout);
        }

        [UnityTest]
        public IEnumerator Bips_OneAtATimeThenOfflinePopup()
        {
            var root = Root<HideoutScreen>();
            GameEvents.RaiseBip("Dona Cida", "Primeiro bip do teste.");
            GameEvents.RaiseBip("Dona Cida", "Segundo bip do teste.");
            yield return WaitForPager(root, "Primeiro bip do teste.");
            yield return Print("bip");
            Click(root, "pager");
            yield return WaitForPager(root, "Segundo bip do teste.");

            GameEvents.RaiseOfflineEarnings(1234, 2 * 3600 + 5 * 60);
            yield return new WaitForSecondsRealtime(0.3f);
            var popup = root.Query(className: "dialog").Last();
            StringAssert.Contains("2 h 5 min", popup.Query<Label>().Last().text);
            yield return Print("offline");
        }

        /// <summary>Espera o bip com esse texto; os que estavam antes na fila (boas-vindas da Dona Cida) vão sendo clicados.</summary>
        static IEnumerator WaitForPager(VisualElement root, string text)
        {
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                var pager = root.Q<Button>("pager");
                if (pager == null) continue;
                if (pager.Query<Label>().Last().text == text) yield break;
                Click(pager);
            }
            Assert.Fail($"O bip \"{text}\" não apareceu.");
        }
    }
}

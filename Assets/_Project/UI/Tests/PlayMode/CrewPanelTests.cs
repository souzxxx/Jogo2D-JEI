using System.Collections;
using System.Linq;
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
    public class CrewPanelTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            UseMemorySave();
            Services.StartNewGame();
            Services.Economy.Add(100000, "teste");
            SceneManager.LoadScene(SceneFlow.Hideout);
            yield return WaitForScene(SceneFlow.Hideout);
        }

        [UnityTest]
        public IEnumerator Contacts_RecruitAddsToTheCrew()
        {
            var root = Root<HideoutScreen>();
            int before = Services.Crew.Crew.Count;
            Click(root, "contacts");
            yield return null;
            yield return Print("contatos");

            Click(root, "hire-" + Services.Crew.Candidates[0].thief.id);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(before + 1, Services.Crew.Crew.Count);
        }

        [UnityTest]
        public IEnumerator Crew_PayBailThenFireWithConfirmation()
        {
            var root = Root<HideoutScreen>();
            var thief = Services.Crew.Crew[0];
            Services.Crew.Jail(thief, 120f);
            Click(root, "crew");
            yield return new WaitForSecondsRealtime(0.4f);
            StringAssert.StartsWith("Preso", root.Q(className: "card").Query<Label>().ToList().Last().text);
            yield return Print("crew_preso");

            Click(root, "bail-" + thief.id);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(ThiefStatus.Available, thief.status, "Pagou a fiança, saiu.");

            int before = Services.Crew.Crew.Count;
            Click(root, "fire-" + thief.id);
            yield return null;
            Assert.AreEqual(before, Services.Crew.Crew.Count, "Demitir pede confirmação antes.");
            Click(root.Q(className: "dialog").Query<Button>().Last());
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(before - 1, Services.Crew.Crew.Count);
        }
    }
}

using UnityEngine;

namespace AFirma.Core
{
    /// <summary>Cena Boot: os serviços já subiram no GameLoop; só segue pro menu.</summary>
    public class BootLoader : MonoBehaviour
    {
        void Start() => SceneFlow.GoToMainMenu();
    }
}

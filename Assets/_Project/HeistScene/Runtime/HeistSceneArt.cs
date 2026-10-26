using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Tudo que a Cena do Golpe usa e não pertence a um prédio: salas padrão por RoomKind, ladrão genérico,
    /// ícones, anel de progresso, balão, fonte, veículos e sons. Preenchido pelo HeistSceneBuilder.
    /// </summary>
    [CreateAssetMenu(menuName = "A Firma/Cena do Golpe/Arte da cena", fileName = "HeistSceneArt")]
    public class HeistSceneArt : ScriptableObject
    {
        [Header("Salas modulares (índice = RoomKind)")]
        public RoomView[] roomPrefabs = new RoomView[8];

        [Header("Personagens")]
        public ThiefActor thiefPrefab;
        [Tooltip("Ladrão sem arte ainda (ThiefTypeDef.animator vazio): base + roupa tingida com a cor do tipo.")]
        public FlipbookAnim[] genericThief;

        [Header("Ícones")]
        public Sprite lockIcon;
        public Sprite systemIcon;
        public Sprite personIcon;
        public Sprite checkIcon;
        public Sprite failIcon;
        public Sprite alertIcon;
        public Sprite doubtIcon;
        public Sprite sleepIcon;
        public Sprite handcuffsIcon;
        public Sprite noSpecialistIcon;
        public Sprite eventIcon;
        public Sprite delayIcon;
        public Sprite[] ringFrames;
        public Sprite[] bagSprites;
        public Sprite note;
        public Sprite coin;
        public Sprite spark;
        public Sprite dust;
        public Sprite smoke;
        public Sprite splinter;

        [Header("Balão e texto")]
        public Sprite bubble;
        public Sprite bubbleTail;
        public PixelFont font;

        [Header("Veículos")]
        public Sprite[] vanFrames;
        public Sprite[] policeFrames;

        [Header("Sons da cena (tocam pelo Services.Audio)")]
        public AudioClip lootSfx;
        public AudioClip doorSfx;

        public Sprite ObstacleIcon(AFirma.Core.ObstacleType obstacle)
        {
            switch (obstacle)
            {
                case AFirma.Core.ObstacleType.Lock: return lockIcon;
                case AFirma.Core.ObstacleType.System: return systemIcon;
                default: return personIcon;
            }
        }

        public Sprite BagSprite(double bag)
        {
            if (bagSprites == null || bagSprites.Length == 0) return null;
            int i = bag >= 20000 ? 2 : bag >= 1500 ? 1 : 0;
            return bagSprites[Mathf.Min(i, bagSprites.Length - 1)];
        }
    }
}

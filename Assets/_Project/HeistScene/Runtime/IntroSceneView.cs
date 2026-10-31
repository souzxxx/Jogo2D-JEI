using System.Collections.Generic;
using AFirma.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene
{
    /// <summary>
    /// O que a abertura no Banco Central (ESCOPO 5) tem a mais que um golpe qualquer. O IntroDirector toca a abertura
    /// como um golpe normal e o HeistView conta ele; esta peça só entra enquanto IntroDirector.IsPlaying:
    ///
    /// salas caindo rápido (a cena anda 2,5x até a sirene) · chuva de dinheiro quando o cofre abre · as duas placas
    /// na porta, METER O PÉ e SÓ MAIS UMA, e a crew indo na "só mais uma" · a câmera vai até a rua ver a viatura
    /// do Delegado Bastos chegando e ele descendo · todo mundo algemado · flash de fotógrafo e a cena congela em
    /// sépia, que é quando a capa do jornal entra. Nada aqui mexe no save nem no golpe.
    /// </summary>
    public class IntroSceneView : MonoBehaviour
    {
        public HeistView view;
        [Tooltip("Velocidade da cena enquanto as salas caem, até a sirene (o ESCOPO pede ~3x).")]
        public float fastForward = 2.5f;
        [Tooltip("Falas do Bastos: chegando na rua e dentro da sala.")]
        public string bastosArrives = "Tão lá embaixo! Ninguém sai!";
        public string bastosCatches = "Perdeu, Chefe. Acabou a firma!";

        // tempos (s de abertura) da parte que é só da cena, encaixados na tabela do IntroHeistService
        const float StreetAt = IntroHeistService.ArrestAt - 4.5f;   // câmera vai pra rua: viatura do Bastos chega
        const float BackInsideAt = IntroHeistService.ArrestAt - 1.6f; // volta pra crew: Bastos entra e algema

        bool active;
        GameObject bastos;
        SpriteFlipbook bastosBook;
        SpeechBubble bastosBubble;
        VehicleView bastosCar;
        bool streetDone, cuffed, frozen;
        SpriteRenderer signBack, signForward;
        float signsT = -1f, chosenT = -1f;
        readonly List<FallingNote> rain = new List<FallingNote>();
        SpriteRenderer flash;
        float flashT = -1f;
        Volume freezeVolume;
        ColorAdjustments freezeColor;

        HeistSceneArt Art => view != null ? view.art : null;

        void OnEnable()
        {
            GameEvents.HeistStarted += OnHeistStarted;
            GameEvents.RoomCleared += OnRoomCleared;
            GameEvents.DoorDecisionRequested += OnDoorRequested;
            GameEvents.DoorDecisionMade += OnDoorMade;
            GameEvents.HeistFinished += OnFinished;
            GameEvents.IntroFinished += Cleanup;
        }

        void OnDisable()
        {
            GameEvents.HeistStarted -= OnHeistStarted;
            GameEvents.RoomCleared -= OnRoomCleared;
            GameEvents.DoorDecisionRequested -= OnDoorRequested;
            GameEvents.DoorDecisionMade -= OnDoorMade;
            GameEvents.HeistFinished -= OnFinished;
            GameEvents.IntroFinished -= Cleanup;
            Cleanup();
        }

        static float IntroTime => IntroDirector.Current != null ? IntroDirector.Current.Time : 0f;

        void OnHeistStarted(HeistDef def)
        {
            Cleanup();
            if (!IntroDirector.IsPlaying) return;
            active = true;
            SceneClock.IntroBoost = fastForward;
        }

        // ---------------- Cofre: chuva de dinheiro ----------------

        void OnRoomCleared(int index, RoomData room, double loot)
        {
            if (!active || view.Building == null || index != IntroHeistService.RoomsBeforeTheDoor - 1) return;
            var rv = view.Building.rooms[index];
            float top = rv.transform.position.y + 3f;
            float left = rv.Center.x - 2.8f, right = rv.Center.x + 2.8f;
            float floor = view.Building.transform.position.y + RoomView.FeetY - 0.05f;
            for (int i = 0; i < 70; i++)
            {
                var sprite = i % 4 == 3 && Art.coin != null ? Art.coin : Art.note;
                var start = new Vector3(Random.Range(left, right), top + Random.Range(0f, 2.5f), 0f);
                rain.Add(FallingNote.Spawn(sprite, start, floor + Random.Range(-0.05f, 0.1f), view.World));
            }
            view.CameraRig?.Shake(0.1f, 0.8f);
            if (Art.lootSfx != null && Services.IsInitialized) Services.Audio.PlaySfx(Art.lootSfx, 1f);
        }

        // ---------------- A porta das duas placas ----------------

        void OnDoorRequested(DoorDecision decision)
        {
            if (!active || view.Building == null) return;
            var room = view.Building.rooms[Mathf.Clamp(view.RoomIndex, 0, view.Building.rooms.Count - 1)];
            var at = room.exitDoor != null ? room.exitDoor.transform.position : room.ExitPosition;
            float y = view.Building.transform.position.y + RoomView.FeetY + 2.65f;
            signBack = MakeSign("Placa: METER O PÉ", Art.signBack, new Vector3(at.x - 1.55f, y, 0f));
            signForward = MakeSign("Placa: SÓ MAIS UMA", Art.signForward, new Vector3(at.x + 1.55f, y, 0f));
            signsT = 0f;
            chosenT = -1f;
        }

        SpriteRenderer MakeSign(string name, Sprite sprite, Vector3 at)
        {
            if (sprite == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(view.World, false);
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = SortOrder.Badge;
            go.transform.localScale = Vector3.zero;
            return sr;
        }

        void OnDoorMade(bool goToNext)
        {
            if (!active || signForward == null) return;
            chosenT = 0f;
            view.CameraRig?.Shake(0.06f, 0.3f);
        }

        void UpdateSigns(float dt)
        {
            if (signsT < 0f) return;
            signsT += dt;
            float pop = Mathf.Clamp01(signsT / 0.35f);
            // entra crescendo com um quique e balança pendurada nas correntes
            float scale = Mathf.SmoothStep(0f, 1f, pop) * (1f + Mathf.Sin(pop * Mathf.PI) * 0.25f);
            float swing = Mathf.Sin(signsT * 2.4f) * 4f * (1f - Mathf.Clamp01(signsT / 4f));
            if (signBack != null)
            {
                signBack.transform.localScale = Vector3.one * scale;
                signBack.transform.localRotation = Quaternion.Euler(0f, 0f, swing);
            }
            if (signForward != null)
            {
                signForward.transform.localScale = Vector3.one * scale;
                signForward.transform.localRotation = Quaternion.Euler(0f, 0f, -swing);
            }

            // a crew grita "só mais uma": a placa vermelha pisca. Escolheu: ela pulsa e a verde apaga.
            float t = IntroTime;
            if (chosenT < 0f && t >= IntroHeistService.ShoutAt && signForward != null)
                signForward.color = Mathf.Repeat(t * 4f, 1f) < 0.5f ? Color.white : new Color(1f, 0.75f, 0.75f);
            if (chosenT >= 0f)
            {
                chosenT += dt;
                if (signForward != null)
                {
                    signForward.color = Color.white;
                    signForward.transform.localScale = Vector3.one * (1f + 0.3f * Mathf.Exp(-chosenT * 5f));
                }
                if (signBack != null) signBack.color = Color.Lerp(Color.white, new Color(0.35f, 0.35f, 0.4f), chosenT * 2f);
                if (chosenT > 3.5f)
                {
                    float a = Mathf.Clamp01(1f - (chosenT - 3.5f) / 0.6f);
                    if (signForward != null) signForward.color = new Color(1f, 1f, 1f, a);
                    if (signBack != null) signBack.color = new Color(0.35f, 0.35f, 0.4f, a);
                }
            }
        }

        // ---------------- A polícia: Bastos chega e algema ----------------

        void StreetShot()
        {
            streetDone = true;
            var b = view.Building;
            var entry = new List<Vector3>(b.EntryPoints());
            var door = entry.Count > 1 ? entry[1] : b.vanPoint.position;
            float street = b.transform.position.y + b.streetY;

            // a viatura do Bastos vem cantando pneu da direita e para na boca do túnel
            if (view.policePrefab != null)
            {
                bastosCar = Instantiate(view.policePrefab, view.World);
                bastosCar.name = "Viatura do Bastos";
                bastosCar.Park(new Vector3(door.x + 14f, street, 0f), faceLeft: true);
                bastosCar.DriveTo(new Vector3(door.x + 2.2f, street, 0f), 11f, 0f);
                bastosCar.SetFlashing(true);
            }

            bastos = new GameObject("Delegado Bastos");
            bastos.transform.SetParent(view.World, false);
            bastos.transform.position = new Vector3(door.x + 0.85f, street + RoomView.FeetY, 0f); // desce do lado do motorista
            var sr = bastos.AddComponent<SpriteRenderer>();
            sr.sortingOrder = SortOrder.Vehicle + 1;
            bastosBook = bastos.AddComponent<SpriteFlipbook>();
            bastosBook.body = sr;
            bastosBook.anims = Art.bastos ?? new FlipbookAnim[0];
            bastosBook.Play("idle");
            bastosBook.SetFlip(true);
            sr.enabled = false; // desce da viatura quando ela para
            bastosBubble = SpeechBubble.Create(Art, view.World);

            view.CameraRig?.Follow(new Vector2(door.x + 1.5f, view.CameraY), false);
        }

        void UpdateBastos(float dt)
        {
            if (bastos == null) return;
            var sr = bastosBook.body;
            if (!sr.enabled && (bastosCar == null || bastosCar.Arrived))
            {
                sr.enabled = true;
                bastosBook.Play("apontar");
                bastosBubble?.Show(bastosArrives, bastos.transform, new Vector3(0f, 30f / 32f, 0f), 2.2f);
                view.CameraRig?.Shake(0.05f, 0.25f);
            }
        }

        void CuffEveryone()
        {
            cuffed = true;
            var b = view.Building;
            if (b == null) return;
            // volta pra dentro: o Bastos já está na porta da sala, com a algema na mão
            var room = b.rooms[Mathf.Clamp(view.RoomIndex, 0, b.rooms.Count - 1)];
            if (bastos != null)
            {
                bastos.transform.position = new Vector3(room.EntryPosition.x + 0.3f, b.transform.position.y + RoomView.FeetY, 0f);
                bastosBook.body.enabled = true;
                bastosBook.SetFlip(false);
                bastosBook.Play("algemar");
                bastosBubble?.Show(bastosCatches, bastos.transform, new Vector3(0f, 30f / 32f, 0f), 3f);
            }
            foreach (var actor in view.Actors)
            {
                actor.WalkTo(actor.transform.position);
                actor.Face(true);
                actor.SetPose(ThiefActor.Idle);
                actor.ShowMarker(Art.handcuffsIcon);
                PopIcon.Spawn(Art.handcuffsIcon, actor.HeadPosition + new Vector3(0f, 0.25f, 0f), 1.2f, 0.4f);
            }
            view.CameraRig?.Follow(new Vector2(room.Center.x, view.CameraY), true);
            view.CameraRig?.Shake(0.08f, 0.4f);
        }

        // ---------------- Fim: flash e congela ----------------

        void OnFinished(HeistResult result)
        {
            if (!active) return;
            if (!cuffed) CuffEveryone();
            Freeze();
        }

        void Freeze()
        {
            frozen = true;
            var cam = view.CameraRig != null ? view.CameraRig.cam : Camera.main;
            if (cam != null)
            {
                var go = new GameObject("Flash do fotógrafo");
                go.transform.SetParent(cam.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, 1f);
                flash = go.AddComponent<SpriteRenderer>();
                var tex = Texture2D.whiteTexture;
                flash.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
                flash.sortingOrder = 1000;
                float h = cam.orthographicSize * 2f + 1f;
                go.transform.localScale = new Vector3(h * cam.aspect + 1f, h, 1f);
                flashT = 0f;
            }
            freezeVolume = new GameObject("Congelado (sépia)").AddComponent<Volume>();
            freezeVolume.transform.SetParent(transform, false);
            freezeVolume.isGlobal = true;
            freezeVolume.priority = 50f;
            freezeVolume.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Congelado";
            freezeColor = profile.Add<ColorAdjustments>(true);
            freezeColor.saturation.Override(-100f);
            freezeColor.contrast.Override(25f);
            freezeColor.colorFilter.Override(new Color(1f, 0.92f, 0.78f));
            freezeVolume.profile = profile;
        }

        void UpdateFreeze(float realDt)
        {
            if (flashT < 0f) return;
            flashT += realDt;
            if (flash != null) flash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - flashT / 0.45f));
            // congela logo depois do flash: tudo que anda pelo SceneClock para no lugar
            if (flashT > 0.08f) SceneClock.Frozen = true;
            if (freezeVolume != null) freezeVolume.weight = Mathf.Clamp01((flashT - 0.05f) / 0.5f);
            if (flashT > 0.12f && view.World != null)
                foreach (var ps in view.World.GetComponentsInChildren<ParticleSystem>())
                    if (ps.isPlaying) ps.Pause();
        }

        // ---------------- Todo frame ----------------

        void Update()
        {
            if (!active) return;
            if (!IntroDirector.IsPlaying || IntroDirector.Current == null || view.Building == null)
            {
                Cleanup();
                return;
            }
            float t = IntroTime;
            float realDt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float dt = SceneClock.Delta;

            if (!frozen) SceneClock.IntroBoost = t < IntroHeistService.SirenAt ? fastForward : 1f;
            if (!streetDone && t >= StreetAt) StreetShot();
            if (!cuffed && t >= BackInsideAt) CuffEveryone();
            // enquanto a câmera está na rua, o HeistView não puxa ela de volta (a crew está parada trabalhando)
            if (streetDone && !cuffed && bastos != null) view.CameraRig?.Follow(new Vector2(bastos.transform.position.x - 0.5f, view.CameraY));

            UpdateSigns(dt);
            UpdateBastos(dt);
            for (int i = rain.Count - 1; i >= 0; i--)
            {
                if (rain[i] == null) { rain.RemoveAt(i); continue; }
                rain[i].Tick(dt);
            }
            UpdateFreeze(realDt);
        }

        void Cleanup()
        {
            SceneClock.IntroBoost = 1f;
            SceneClock.Frozen = false;
            if (!active) return;
            active = false;
            streetDone = cuffed = frozen = false;
            signsT = chosenT = flashT = -1f;
            foreach (var n in rain) if (n != null) Destroy(n.gameObject);
            rain.Clear();
            if (bastos != null) Destroy(bastos);
            if (bastosBubble != null) Destroy(bastosBubble.gameObject);
            if (bastosCar != null) Destroy(bastosCar.gameObject);
            if (signBack != null) Destroy(signBack.gameObject);
            if (signForward != null) Destroy(signForward.gameObject);
            if (flash != null) Destroy(flash.gameObject);
            if (freezeVolume != null) Destroy(freezeVolume.gameObject);
            bastos = null;
            bastosCar = null;
        }

        /// <summary>Uma cédula da chuva do cofre: cai girando e balançando, quica no chão e some.</summary>
        class FallingNote : MonoBehaviour
        {
            SpriteRenderer sr;
            Vector3 velocity;
            float floorY, spin, sway, life;
            bool landed;

            public static FallingNote Spawn(Sprite sprite, Vector3 at, float floorY, Transform parent)
            {
                var go = new GameObject("Cédula caindo");
                go.transform.SetParent(parent, false);
                go.transform.position = at;
                var n = go.AddComponent<FallingNote>();
                n.sr = go.AddComponent<SpriteRenderer>();
                n.sr.sprite = sprite;
                n.sr.sortingOrder = SortOrder.Fx;
                n.floorY = floorY;
                n.velocity = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.5f, 0.5f), 0f);
                n.spin = Random.Range(-360f, 360f);
                n.sway = Random.Range(0f, 6.28f);
                return n;
            }

            public void Tick(float dt)
            {
                if (dt <= 0f) return;
                life += dt;
                if (!landed)
                {
                    velocity.y = Mathf.Max(velocity.y - 9f * dt, -3.2f);       // cédula cai devagar (resistência do ar)
                    var p = transform.position + velocity * dt;
                    p.x += Mathf.Sin(life * 5f + sway) * 0.6f * dt;
                    if (p.y <= floorY)
                    {
                        p.y = floorY;
                        landed = true;
                    }
                    transform.position = p;
                    transform.Rotate(0f, 0f, spin * dt);
                }
                if (life > 3.2f)
                {
                    float a = Mathf.Clamp01(1f - (life - 3.2f) / 0.8f);
                    sr.color = new Color(1f, 1f, 1f, a);
                    if (a <= 0f) Destroy(gameObject);
                }
            }
        }
    }
}

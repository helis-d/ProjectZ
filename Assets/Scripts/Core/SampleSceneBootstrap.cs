using FishNet.Object;
using ProjectZ.Economy;
using ProjectZ.GameMode;
using ProjectZ.Map;
using ProjectZ.Player;
using ProjectZ.Sphere;
using ProjectZ.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectZ.Core
{
    [DefaultExecutionOrder(-10000)]
    public class SampleSceneBootstrap : MonoBehaviour
    {
        private const string TargetSceneName = "SampleScene";
        private bool _uiBound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateBootstrap()
        {
            if (SceneManager.GetActiveScene().name != TargetSceneName)
                return;

            if (FindFirstObjectByType<SampleSceneBootstrap>() != null)
                return;

            GameObject bootstrapObject = new GameObject("ProjectZ Sample Bootstrap");
            bootstrapObject.AddComponent<SampleSceneBootstrap>();
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().name != TargetSceneName)
            {
                Destroy(gameObject);
                return;
            }

            EnsureGameplaySystems();
            EnsureSphereSystems();
            EnsureBuyZones();
            EnsureHud();
            EnsureCrosshair();
        }

        private void Update()
        {
            if (_uiBound)
                return;

            PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            foreach (PlayerHealth player in players)
            {
                if (player == null || !player.IsOwner)
                    continue;

                CrosshairUI crosshair = FindFirstObjectByType<CrosshairUI>();
                if (crosshair != null)
                    crosshair.BindLocalPlayer(player.gameObject);

                HUDManager.Instance?.BindLocalPlayer(player.gameObject);
                _uiBound = true;
                return;
            }
        }

        private static void EnsureGameplaySystems()
        {
            GameObject systems = GameObject.Find("Gameplay Systems");
            if (systems == null)
                systems = new GameObject("Gameplay Systems");

            EnsureComponent<NetworkObject>(systems);
            RankedGameMode rankedMode = EnsureComponent<RankedGameMode>(systems);
            TeamManager teamManager = EnsureComponent<TeamManager>(systems);
            EnsureComponent<RoundManager>(systems);
            EnsureComponent<EconomyManager>(systems);

            EnsureSpawnPoints(teamManager, systems.transform);

            if (rankedMode != null)
                rankedMode.enabled = true;
        }

        private static void EnsureSphereSystems()
        {
            GameObject sphereManagerObject = GameObject.Find("Sphere Manager");
            if (sphereManagerObject == null)
                sphereManagerObject = new GameObject("Sphere Manager");

            EnsureComponent<NetworkObject>(sphereManagerObject);
            SphereManager sphereManager = EnsureComponent<SphereManager>(sphereManagerObject);

            GameObject sitesRoot = GameObject.Find("Sphere Sites");
            if (sitesRoot == null)
                sitesRoot = new GameObject("Sphere Sites");

            EnsureSite(sitesRoot.transform, "Site A", "A", new Vector3(0f, 0.5f, 0f), 2.5f);

            if (sphereManagerObject.transform.position == Vector3.zero)
                sphereManagerObject.transform.position = new Vector3(0f, 0.5f, 0f);

            sphereManager.enabled = true;
        }

        private static void EnsureBuyZones()
        {
            GameObject buyRoot = GameObject.Find("Buy Zones");
            if (buyRoot == null)
                buyRoot = new GameObject("Buy Zones");

            EnsureZone(buyRoot.transform, "Attacker Buy Zone", Team.Attacker, new Vector3(0f, 1f, -11f), new Vector3(8f, 2f, 6f));
            EnsureZone(buyRoot.transform, "Defender Buy Zone", Team.Defender, new Vector3(0f, 1f, 11f), new Vector3(8f, 2f, 6f));
        }

        private static void EnsureHud()
        {
            if (FindFirstObjectByType<HUDManager>() != null)
                return;

            Canvas canvas = EnsureOverlayCanvas();
            GameObject hudRoot = new GameObject("HUD Root");
            hudRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = hudRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CreateHudText(hudRoot.transform, "ModeText", new Vector2(20f, -20f), TextAlignmentOptions.TopLeft, 20f, "RANKED");
            CreateHudText(hudRoot.transform, "PhaseText", new Vector2(20f, -52f), TextAlignmentOptions.TopLeft, 20f, "BUY PHASE");
            CreateHudText(hudRoot.transform, "RoundText", new Vector2(20f, -84f), TextAlignmentOptions.TopLeft, 18f, "ROUND 1");
            CreateHudText(hudRoot.transform, "ScoreText", new Vector2(0f, -20f), TextAlignmentOptions.Top, 24f, "ATK 0 - 0 DEF");
            CreateHudText(hudRoot.transform, "HeroIdentityText", new Vector2(20f, 24f), TextAlignmentOptions.BottomLeft, 18f, "OPERATIVE");
            CreateHudText(hudRoot.transform, "UltimateStatusText", new Vector2(-20f, 24f), TextAlignmentOptions.BottomRight, 18f, "ULT 0%");
            CreateHudText(hudRoot.transform, "HealthText", new Vector2(-20f, 84f), TextAlignmentOptions.BottomRight, 20f, "100");
            CreateHudText(hudRoot.transform, "ArmorText", new Vector2(-20f, 56f), TextAlignmentOptions.BottomRight, 18f, "50");
            CreateHudText(hudRoot.transform, "AmmoText", new Vector2(20f, 84f), TextAlignmentOptions.BottomLeft, 24f, "0 / 0");
            CreateBadge(hudRoot.transform, "PistolRoundBadge", new Vector2(0f, -56f), "PISTOL");
            CreateBadge(hudRoot.transform, "OvertimeBadge", new Vector2(0f, -88f), "OVERTIME");

            hudRoot.AddComponent<HUDManager>();
        }

        private static void EnsureCrosshair()
        {
            if (FindFirstObjectByType<CrosshairUI>() != null)
                return;

            Canvas canvas = EnsureOverlayCanvas();
            GameObject crosshairRoot = new GameObject("Crosshair");
            crosshairRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = crosshairRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(64f, 64f);
            rootRect.anchoredPosition = Vector2.zero;

            CreateCrosshairLine(crosshairRoot.transform, "TopLine", new Vector2(2f, 12f), new Vector2(0f, 12f));
            CreateCrosshairLine(crosshairRoot.transform, "BottomLine", new Vector2(2f, 12f), new Vector2(0f, -12f));
            CreateCrosshairLine(crosshairRoot.transform, "LeftLine", new Vector2(12f, 2f), new Vector2(-12f, 0f));
            CreateCrosshairLine(crosshairRoot.transform, "RightLine", new Vector2(12f, 2f), new Vector2(12f, 0f));
            crosshairRoot.AddComponent<CrosshairUI>();
        }

        private static void EnsureSpawnPoints(TeamManager teamManager, Transform parent)
        {
            GameObject spawnRoot = GameObject.Find("Bootstrap Spawn Points");
            if (spawnRoot == null)
            {
                spawnRoot = new GameObject("Bootstrap Spawn Points");
                spawnRoot.transform.SetParent(parent, false);
            }

            Transform[] attackerSpawns = new Transform[5];
            Transform[] defenderSpawns = new Transform[5];

            for (int i = 0; i < attackerSpawns.Length; i++)
            {
                attackerSpawns[i] = EnsureMarker(
                    spawnRoot.transform,
                    $"Attacker Spawn {i + 1}",
                    new Vector3(-4f + (i * 2f), 1f, -12f),
                    Quaternion.identity);

                defenderSpawns[i] = EnsureMarker(
                    spawnRoot.transform,
                    $"Defender Spawn {i + 1}",
                    new Vector3(-4f + (i * 2f), 1f, 12f),
                    Quaternion.Euler(0f, 180f, 0f));
            }

            teamManager.SetDynamicSpawns(attackerSpawns, defenderSpawns);
        }

        private static void EnsureSite(Transform parent, string name, string siteId, Vector3 position, float radius)
        {
            Transform existing = parent.Find(name);
            GameObject siteObject = existing != null ? existing.gameObject : new GameObject(name);
            siteObject.transform.SetParent(parent, false);
            siteObject.transform.position = position;

            SphereCollider collider = EnsureComponent<SphereCollider>(siteObject);
            collider.isTrigger = true;
            collider.radius = radius;

            SphereSite site = EnsureComponent<SphereSite>(siteObject);
            site.SiteID = siteId;
        }

        private static void EnsureZone(Transform parent, string name, Team team, Vector3 position, Vector3 size)
        {
            Transform existing = parent.Find(name);
            GameObject zoneObject = existing != null ? existing.gameObject : new GameObject(name);
            zoneObject.transform.SetParent(parent, false);
            zoneObject.transform.position = position;

            BoxCollider collider = EnsureComponent<BoxCollider>(zoneObject);
            collider.isTrigger = true;
            collider.size = size;

            BuyZone buyZone = EnsureComponent<BuyZone>(zoneObject);
            buyZone.Configure(team);
        }

        private static Transform EnsureMarker(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            Transform existing = parent.Find(name);
            GameObject marker = existing != null ? existing.gameObject : new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.rotation = rotation;
            return marker.transform;
        }

        private static Canvas EnsureOverlayCanvas()
        {
            GameObject bootstrapCanvasObject = GameObject.Find("Bootstrap Canvas");
            if (bootstrapCanvasObject != null && bootstrapCanvasObject.TryGetComponent(out Canvas bootstrapCanvas))
                return bootstrapCanvas;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas;

            GameObject canvasObject = new GameObject("Bootstrap Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateCrosshairLine(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);

            Image image = lineObject.AddComponent<Image>();
            image.color = Color.white;

            RectTransform rect = lineObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
        }

        private static void CreateHudText(Transform parent, string name, Vector2 anchoredPosition, TextAlignmentOptions alignment, float fontSize, string defaultText)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = GetAnchor(alignment);
            rect.anchorMax = GetAnchor(alignment);
            rect.pivot = GetPivot(alignment);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(320f, 40f);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
        }

        private static void CreateBadge(Transform parent, string name, Vector2 anchoredPosition, string label)
        {
            GameObject badgeObject = new GameObject(name);
            badgeObject.transform.SetParent(parent, false);

            RectTransform rect = badgeObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(140f, 28f);

            Image image = badgeObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(badgeObject.transform, false);

            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 16f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            badgeObject.SetActive(false);
        }

        private static Vector2 GetAnchor(TextAlignmentOptions alignment)
        {
            if (alignment == TextAlignmentOptions.TopLeft)
                return new Vector2(0f, 1f);

            if (alignment == TextAlignmentOptions.Top)
                return new Vector2(0.5f, 1f);

            if (alignment == TextAlignmentOptions.BottomLeft)
                return new Vector2(0f, 0f);

            if (alignment == TextAlignmentOptions.BottomRight)
                return new Vector2(1f, 0f);

            return new Vector2(0.5f, 0.5f);
        }

        private static Vector2 GetPivot(TextAlignmentOptions alignment)
        {
            if (alignment == TextAlignmentOptions.TopLeft)
                return new Vector2(0f, 1f);

            if (alignment == TextAlignmentOptions.Top)
                return new Vector2(0.5f, 1f);

            if (alignment == TextAlignmentOptions.BottomLeft)
                return new Vector2(0f, 0f);

            if (alignment == TextAlignmentOptions.BottomRight)
                return new Vector2(1f, 0f);

            return new Vector2(0.5f, 0.5f);
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
                component = target.AddComponent<T>();
            return component;
        }
    }
}

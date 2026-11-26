using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Aetherlands.Fishing
{
    /// <summary>
    /// Code-generated pixel-art UGUI overlay for the Aetherlands fishing system:
    /// cast power gauge, Stardew-style vertical reel minigame bar, caught/escaped toast,
    /// and subtle water-ready prompt. Zero per-frame allocations.
    /// </summary>
    public sealed class FishingOverlayView : MonoBehaviour
    {
        static readonly Color32 ProgressLow = new Color32(191, 91, 67, 255);
        static readonly Color32 ProgressMid = new Color32(240, 187, 102, 255);
        static readonly Color32 ProgressHigh = new Color32(114, 166, 79, 255);

        readonly List<Object> ownedResources = new List<Object>(32);
        readonly Dictionary<string, Sprite> fishSpriteCache = new Dictionary<string, Sprite>(8);

        Canvas rootCanvas;
        Font uiFont;
        Sprite defaultFishSprite;

        // Cast power gauge.
        GameObject castPanel;
        Image castFillImage;

        // Vertical reel minigame panel.
        GameObject minigamePanel;
        RectTransform waterTrackRect;
        RectTransform catchBarRect;
        Image catchBarImage;
        RectTransform fishIconRect;
        Image fishIconImage;
        RectTransform progressTrackRect;
        RectTransform progressFillRect;
        Image progressFillImage;
        GameObject perfectTagObj;

        // Caught / escaped toast.
        GameObject toastPanel;
        Image toastFishIcon;
        Text toastTitleText;
        Text toastDetailsText;
        Text toastCountText;

        // Idle cast prompt.
        GameObject promptPanel;

        bool initialized;
        string activeFishSpriteId;

        public void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            Sprite panelSprite = FishingPixelArt.CreateFramedPanelSprite(FishingPixelArt.Parchment, ownedResources);
            Sprite darkPanelSprite = FishingPixelArt.CreateFramedPanelSprite(FishingPixelArt.Navy, ownedResources);
            Sprite waterColumnSprite = FishingPixelArt.CreateWaterColumnSprite(ownedResources);
            Sprite catchBarSprite = FishingPixelArt.CreateCatchBarSprite(ownedResources);
            Sprite solidWhite = FishingPixelArt.CreateSolidSprite(Color.white, ownedResources);
            defaultFishSprite = FishingPixelArt.CreateFishIconSprite(null, ownedResources);

            // Root Canvas.
            var canvasGo = new GameObject("FishingOverlayCanvas");
            canvasGo.transform.SetParent(transform, false);
            rootCanvas = canvasGo.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 40;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildPrompt(canvasGo.transform, darkPanelSprite);
            BuildCastGauge(canvasGo.transform, darkPanelSprite, solidWhite);
            BuildMinigamePanel(canvasGo.transform, darkPanelSprite, waterColumnSprite, catchBarSprite, solidWhite);
            BuildToastPanel(canvasGo.transform, panelSprite);

            HideAll();
        }

        void BuildPrompt(Transform parent, Sprite frameSprite)
        {
            promptPanel = CreateImageObject("CastPrompt", parent, frameSprite, Image.Type.Sliced, out var rect);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 22f);
            rect.sizeDelta = new Vector2(260f, 36f);

            var label = CreateText("PromptLabel", promptPanel.transform, "Hold [Space / E / Click] to Cast", 14, FishingPixelArt.Cream, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 10f, 4f, 10f, 4f);
        }

        void BuildCastGauge(Transform parent, Sprite frameSprite, Sprite solidWhite)
        {
            castPanel = CreateImageObject("CastPowerPanel", parent, frameSprite, Image.Type.Sliced, out var rect);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 42f);
            rect.sizeDelta = new Vector2(240f, 48f);

            var label = CreateText("CastTitle", castPanel.transform, "CAST POWER", 12, FishingPixelArt.Gold, TextAnchor.UpperCenter);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            label.rectTransform.sizeDelta = new Vector2(0f, 18f);

            var trackGo = CreateImageObject("CastTrack", castPanel.transform, solidWhite, Image.Type.Simple, out var trackRect);
            trackGo.GetComponent<Image>().color = FishingPixelArt.Ink;
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = new Vector2(0f, 9f);
            trackRect.sizeDelta = new Vector2(-24f, 14f);

            var fillGo = CreateImageObject("CastFill", trackGo.transform, solidWhite, Image.Type.Filled, out var fillRect);
            castFillImage = fillGo.GetComponent<Image>();
            castFillImage.fillMethod = Image.FillMethod.Horizontal;
            castFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            castFillImage.color = FishingPixelArt.CatchGreen;
            Stretch(fillRect, 2f, 2f, 2f, 2f);
        }

        void BuildMinigamePanel(Transform parent, Sprite frameSprite, Sprite waterSprite, Sprite catchBarSprite, Sprite solidWhite)
        {
            minigamePanel = CreateImageObject("FishingMinigamePanel", parent, frameSprite, Image.Type.Sliced, out var rect);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(175f, 10f);
            rect.sizeDelta = new Vector2(98f, 344f);

            // Water column track on the left.
            var waterGo = CreateImageObject("WaterTrack", minigamePanel.transform, waterSprite, Image.Type.Simple, out waterTrackRect);
            waterTrackRect.anchorMin = new Vector2(0f, 0f);
            waterTrackRect.anchorMax = new Vector2(0f, 1f);
            waterTrackRect.pivot = new Vector2(0f, 0.5f);
            waterTrackRect.anchoredPosition = new Vector2(14f, 0f);
            waterTrackRect.sizeDelta = new Vector2(46f, -28f);

            // Green catch bar inside water track.
            var barGo = CreateImageObject("CatchBar", waterGo.transform, catchBarSprite, Image.Type.Sliced, out catchBarRect);
            catchBarImage = barGo.GetComponent<Image>();
            catchBarRect.anchorMin = new Vector2(0.5f, 0f);
            catchBarRect.anchorMax = new Vector2(0.5f, 0f);
            catchBarRect.pivot = new Vector2(0.5f, 0f);
            catchBarRect.sizeDelta = new Vector2(42f, 76f);

            // Fish sprite inside water track.
            var fishGo = CreateImageObject("FishIcon", waterGo.transform, defaultFishSprite, Image.Type.Simple, out fishIconRect);
            fishIconImage = fishGo.GetComponent<Image>();
            fishIconImage.preserveAspect = true;
            fishIconRect.anchorMin = new Vector2(0.5f, 0f);
            fishIconRect.anchorMax = new Vector2(0.5f, 0f);
            fishIconRect.pivot = new Vector2(0.5f, 0.5f);
            fishIconRect.sizeDelta = new Vector2(42f, 28f);

            // Right-side vertical progress bar background.
            var progBgGo = CreateImageObject("ProgressTrack", minigamePanel.transform, solidWhite, Image.Type.Simple, out progressTrackRect);
            progBgGo.GetComponent<Image>().color = FishingPixelArt.Ink;
            progressTrackRect.anchorMin = new Vector2(1f, 0f);
            progressTrackRect.anchorMax = new Vector2(1f, 1f);
            progressTrackRect.pivot = new Vector2(1f, 0.5f);
            progressTrackRect.anchoredPosition = new Vector2(-14f, 0f);
            progressTrackRect.sizeDelta = new Vector2(16f, -28f);

            var progFillGo = CreateImageObject("ProgressFill", progBgGo.transform, solidWhite, Image.Type.Simple, out progressFillRect);
            progressFillImage = progFillGo.GetComponent<Image>();
            progressFillImage.color = ProgressMid;
            progressFillRect.anchorMin = new Vector2(0f, 0f);
            progressFillRect.anchorMax = new Vector2(1f, 0f);
            progressFillRect.pivot = new Vector2(0.5f, 0f);
            progressFillRect.anchoredPosition = new Vector2(0f, 2f);
            progressFillRect.sizeDelta = new Vector2(-4f, 60f);

            // Small "PERFECT" badge above the bar.
            perfectTagObj = CreateImageObject("PerfectBadge", minigamePanel.transform, frameSprite, Image.Type.Sliced, out var perfRect);
            perfRect.anchorMin = new Vector2(0.5f, 1f);
            perfRect.anchorMax = new Vector2(0.5f, 1f);
            perfRect.pivot = new Vector2(0.5f, 0f);
            perfRect.anchoredPosition = new Vector2(0f, 6f);
            perfRect.sizeDelta = new Vector2(96f, 24f);
            var perfText = CreateText("PerfectLabel", perfectTagObj.transform, "PERFECT", 11, FishingPixelArt.LanternGlow, TextAnchor.MiddleCenter);
            Stretch(perfText.rectTransform, 4f, 2f, 4f, 2f);
        }

        void BuildToastPanel(Transform parent, Sprite frameSprite)
        {
            toastPanel = CreateImageObject("CatchToastPanel", parent, frameSprite, Image.Type.Sliced, out var rect);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -22f);
            rect.sizeDelta = new Vector2(340f, 84f);

            var iconGo = CreateImageObject("ToastFishIcon", toastPanel.transform, defaultFishSprite, Image.Type.Simple, out var iconRect);
            toastFishIcon = iconGo.GetComponent<Image>();
            toastFishIcon.preserveAspect = true;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(16f, 0f);
            iconRect.sizeDelta = new Vector2(64f, 44f);

            toastTitleText = CreateText("ToastTitle", toastPanel.transform, "Caught Anchovy!", 17, FishingPixelArt.Ink, TextAnchor.UpperLeft);
            toastTitleText.fontStyle = FontStyle.Bold;
            toastTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            toastTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            toastTitleText.rectTransform.pivot = new Vector2(0f, 1f);
            toastTitleText.rectTransform.anchoredPosition = new Vector2(90f, -12f);
            toastTitleText.rectTransform.sizeDelta = new Vector2(-104f, 24f);

            toastDetailsText = CreateText("ToastDetails", toastPanel.transform, "18.5 cm  |  Common", 13, FishingPixelArt.TimberDark, TextAnchor.MiddleLeft);
            toastDetailsText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            toastDetailsText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            toastDetailsText.rectTransform.pivot = new Vector2(0f, 0.5f);
            toastDetailsText.rectTransform.anchoredPosition = new Vector2(90f, -2f);
            toastDetailsText.rectTransform.sizeDelta = new Vector2(-104f, 22f);

            toastCountText = CreateText("ToastCount", toastPanel.transform, "Total Caught: 1", 12, FishingPixelArt.Navy, TextAnchor.LowerLeft);
            toastCountText.rectTransform.anchorMin = new Vector2(0f, 0f);
            toastCountText.rectTransform.anchorMax = new Vector2(1f, 0f);
            toastCountText.rectTransform.pivot = new Vector2(0f, 0f);
            toastCountText.rectTransform.anchoredPosition = new Vector2(90f, 12f);
            toastCountText.rectTransform.sizeDelta = new Vector2(-104f, 20f);
        }

        public void HideAll()
        {
            if (!initialized) return;
            promptPanel.SetActive(false);
            castPanel.SetActive(false);
            minigamePanel.SetActive(false);
            toastPanel.SetActive(false);
        }

        public void UpdatePrompt(bool showCastPrompt)
        {
            EnsureInitialized();
            if (promptPanel.activeSelf != showCastPrompt)
            {
                promptPanel.SetActive(showCastPrompt);
            }
        }

        public void UpdateCastGauge(bool visible, float normalizedPower)
        {
            EnsureInitialized();
            if (castPanel.activeSelf != visible)
            {
                castPanel.SetActive(visible);
            }

            if (!visible) return;
            float p = Mathf.Clamp01(normalizedPower);
            castFillImage.fillAmount = p;
            castFillImage.color = p < 0.5f
                ? Color.Lerp(FishingPixelArt.MintFoam, FishingPixelArt.Gold, p * 2f)
                : Color.Lerp(FishingPixelArt.Gold, FishingPixelArt.Coral, (p - 0.5f) * 2f);
        }

        public void UpdateMinigame(FishingMinigameSession session, FishSpeciesDefinition speciesDef)
        {
            EnsureInitialized();
            bool active = session != null;
            if (minigamePanel.activeSelf != active)
            {
                minigamePanel.SetActive(active);
            }

            if (!active) return;

            string speciesId = speciesDef != null ? speciesDef.id : session.Fish.Id;
            if (activeFishSpriteId != speciesId)
            {
                activeFishSpriteId = speciesId;
                fishIconImage.sprite = GetOrCreateFishSprite(speciesDef);
            }

            float trackHeight = waterTrackRect.rect.height;
            if (trackHeight <= 1f) trackHeight = 316f;

            // Green catch bar position and height.
            float barHeight = Mathf.Max(20f, session.BarSize * trackHeight);
            catchBarRect.sizeDelta = new Vector2(catchBarRect.sizeDelta.x, barHeight);
            catchBarRect.anchoredPosition = new Vector2(0f, session.BarBottom * trackHeight);
            catchBarImage.color = session.IsOverlapping ? Color.white : new Color(0.72f, 0.80f, 0.72f, 0.82f);

            // Fish sprite position.
            fishIconRect.anchoredPosition = new Vector2(0f, session.FishPosition * trackHeight);

            // Vertical progress bar fill and color ramp.
            float progTrackHeight = progressTrackRect.rect.height - 4f;
            if (progTrackHeight <= 1f) progTrackHeight = 312f;
            float progress = Mathf.Clamp01(session.CatchProgress);
            progressFillRect.sizeDelta = new Vector2(progressFillRect.sizeDelta.x, Mathf.Max(2f, progress * progTrackHeight));
            progressFillImage.color = progress < 0.5f
                ? Color.Lerp(ProgressLow, ProgressMid, progress * 2f)
                : Color.Lerp(ProgressMid, ProgressHigh, (progress - 0.5f) * 2f);

            if (perfectTagObj.activeSelf != session.IsPerfect)
            {
                perfectTagObj.SetActive(session.IsPerfect);
            }
        }

        public void ShowCaughtToast(CaughtFishResult result, FishSpeciesDefinition speciesDef, int totalCatchCount)
        {
            EnsureInitialized();
            toastPanel.SetActive(true);
            toastFishIcon.enabled = true;
            toastFishIcon.sprite = GetOrCreateFishSprite(speciesDef);

            toastTitleText.text = result.IsPerfect
                ? string.Concat("PERFECT! ", result.Species.DisplayName)
                : string.Concat("Caught ", result.Species.DisplayName, "!");
            toastDetailsText.text = string.Format("{0:0.0} cm  |  {1}", result.SizeCm, result.Species.Rarity);
            toastCountText.text = string.Format("Total Caught: {0}", totalCatchCount);
        }

        public void ShowEscapedToast(FishSpec species, int totalCatchCount)
        {
            EnsureInitialized();
            toastPanel.SetActive(true);
            toastFishIcon.enabled = false;
            toastTitleText.text = "The fish got away!";
            toastDetailsText.text = string.Concat(species.DisplayName, " slipped the hook.");
            toastCountText.text = string.Format("Total Caught: {0}", totalCatchCount);
        }

        public void HideToast()
        {
            if (!initialized) return;
            if (toastPanel.activeSelf)
            {
                toastPanel.SetActive(false);
            }
        }

        Sprite GetOrCreateFishSprite(FishSpeciesDefinition speciesDef)
        {
            if (speciesDef == null) return defaultFishSprite;

            string key = string.IsNullOrEmpty(speciesDef.id) ? speciesDef.name : speciesDef.id;
            if (fishSpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite created = FishingPixelArt.CreateFishIconSprite(speciesDef, ownedResources);
            fishSpriteCache[key] = created;
            return created;
        }

        static GameObject CreateImageObject(string name, Transform parent, Sprite sprite, Image.Type imageType, out RectTransform rect)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            rect = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = imageType;
            img.raycastTarget = false;
            return go;
        }

        Text CreateText(string name, Transform parent, string content, int fontSize, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var text = go.AddComponent<Text>();
            text.font = uiFont;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        void OnDestroy()
        {
            fishSpriteCache.Clear();
            for (int i = 0; i < ownedResources.Count; i++)
            {
                if (ownedResources[i] != null)
                {
                    Destroy(ownedResources[i]);
                }
            }

            ownedResources.Clear();
        }
    }
}

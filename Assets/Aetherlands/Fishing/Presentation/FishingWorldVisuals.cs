using System.Collections.Generic;
using UnityEngine;

namespace Aetherlands.Fishing
{
    /// <summary>
    /// Renders world-space procedural fishing props: bobber, parabolic fishing line,
    /// animated water ripples, and the '!' bite cue.
    /// Can be driven by the local FishingController or by remote multiplayer state.
    /// </summary>
    public sealed class FishingWorldVisuals : MonoBehaviour
    {
        const int LineSegments = 16;

        static readonly Quaternion BillboardTilt = Quaternion.Euler(37f, -9f, 0f);
        static readonly Quaternion FlatWaterRotation = Quaternion.Euler(90f, 0f, 0f);

        readonly List<Object> ownedResources = new List<Object>(16);
        readonly Vector3[] linePoints = new Vector3[LineSegments];

        SpriteRenderer bobberRenderer;
        SpriteRenderer rippleRenderer;
        SpriteRenderer cueRenderer;
        LineRenderer lineRenderer;
        Sprite[] rippleFrames;
        Material lineMaterial;
        bool initialized;

        public void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null)
            {
                spriteShader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            Material sharedSpriteMat = spriteShader != null ? new Material(spriteShader) : null;
            if (sharedSpriteMat != null)
            {
                ownedResources.Add(sharedSpriteMat);
            }

            // Bobber.
            var bobberGo = new GameObject("FishingBobber");
            bobberGo.transform.SetParent(transform, false);
            bobberGo.transform.rotation = BillboardTilt;
            bobberRenderer = bobberGo.AddComponent<SpriteRenderer>();
            bobberRenderer.sprite = FishingPixelArt.CreateBobberSprite(ownedResources);
            bobberRenderer.sortingOrder = 5;
            if (sharedSpriteMat != null) bobberRenderer.sharedMaterial = sharedSpriteMat;

            // Animated water ripple flat on the water surface.
            rippleFrames = FishingPixelArt.CreateRippleSprites(ownedResources);
            var rippleGo = new GameObject("WaterRipple");
            rippleGo.transform.SetParent(transform, false);
            rippleGo.transform.rotation = FlatWaterRotation;
            rippleRenderer = rippleGo.AddComponent<SpriteRenderer>();
            rippleRenderer.sprite = rippleFrames[0];
            rippleRenderer.sortingOrder = 3;
            if (sharedSpriteMat != null) rippleRenderer.sharedMaterial = sharedSpriteMat;

            // Bite exclamation cue.
            var cueGo = new GameObject("BiteCue");
            cueGo.transform.SetParent(transform, false);
            cueGo.transform.rotation = BillboardTilt;
            cueRenderer = cueGo.AddComponent<SpriteRenderer>();
            cueRenderer.sprite = FishingPixelArt.CreateExclamationSprite(ownedResources);
            cueRenderer.sortingOrder = 10;
            if (sharedSpriteMat != null) cueRenderer.sharedMaterial = sharedSpriteMat;

            // Parabolic fishing line.
            var lineGo = new GameObject("FishingLine");
            lineGo.transform.SetParent(transform, false);
            lineRenderer = lineGo.AddComponent<LineRenderer>();
            lineRenderer.positionCount = LineSegments;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.028f;
            lineRenderer.endWidth = 0.020f;
            lineRenderer.numCapVertices = 2;
            lineRenderer.sortingOrder = 4;
            lineRenderer.startColor = new Color32(238, 224, 188, 235);
            lineRenderer.endColor = new Color32(204, 224, 219, 210);

            if (spriteShader != null)
            {
                lineMaterial = new Material(spriteShader);
                ownedResources.Add(lineMaterial);
                lineRenderer.sharedMaterial = lineMaterial;
            }

            HideAll();
        }

        public void HideAll()
        {
            if (!initialized) return;
            bobberRenderer.enabled = false;
            rippleRenderer.enabled = false;
            cueRenderer.enabled = false;
            lineRenderer.enabled = false;
        }

        /// <summary>
        /// Updates all world-space visuals without per-frame allocations.
        /// </summary>
        public void UpdateVisuals(
            FishingState state,
            Vector3 rodTipWorld,
            Vector3 bobberWorld,
            Vector3 cueAnchorWorld,
            float waterSurfaceY,
            bool biteCueActive,
            float time)
        {
            EnsureInitialized();

            bool showLineAndBobber = state == FishingState.Casting ||
                                     state == FishingState.WaitingForBite ||
                                     state == FishingState.HookedMinigame;

            bobberRenderer.enabled = showLineAndBobber;
            lineRenderer.enabled = showLineAndBobber;

            if (showLineAndBobber)
            {
                float bobOffset = 0f;
                if (state == FishingState.WaitingForBite)
                {
                    bobOffset = biteCueActive
                        ? -0.14f + Mathf.Sin(time * 28f) * 0.05f
                        : Mathf.Sin(time * 3.4f) * 0.045f;
                }
                else if (state == FishingState.HookedMinigame)
                {
                    bobOffset = Mathf.Sin(time * 16f) * 0.06f;
                }

                Vector3 displayBobber = bobberWorld;
                displayBobber.y += bobOffset;
                bobberRenderer.transform.position = displayBobber;

                // Parabolic slack line during WaitingForBite, taut line during Casting/HookedMinigame.
                float sag = state == FishingState.WaitingForBite ? 0.55f :
                            state == FishingState.HookedMinigame ? 0.12f : 0.35f;

                for (int i = 0; i < LineSegments; i++)
                {
                    float t = i / (float)(LineSegments - 1);
                    Vector3 pt = Vector3.Lerp(rodTipWorld, displayBobber, t);
                    pt.y -= 4f * t * (1f - t) * sag;
                    linePoints[i] = pt;
                }

                lineRenderer.SetPositions(linePoints);
            }

            bool showRipple = state == FishingState.WaitingForBite || state == FishingState.HookedMinigame;
            rippleRenderer.enabled = showRipple;
            if (showRipple)
            {
                float rippleRate = (biteCueActive || state == FishingState.HookedMinigame) ? 7.5f : 2.6f;
                int frame = Mathf.FloorToInt(time * rippleRate) % rippleFrames.Length;
                rippleRenderer.sprite = rippleFrames[frame];
                rippleRenderer.transform.position = new Vector3(bobberWorld.x, waterSurfaceY + 0.03f, bobberWorld.z);
                float scale = (biteCueActive || state == FishingState.HookedMinigame) ? 1.35f : 1.0f;
                rippleRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            }

            cueRenderer.enabled = biteCueActive;
            if (biteCueActive)
            {
                float bounce = Mathf.Abs(Mathf.Sin(time * 11f)) * 0.18f;
                cueRenderer.transform.position = cueAnchorWorld + new Vector3(0f, bounce, 0f);
            }
        }

        /// <summary>
        /// Convenience entry point for multiplayer proxy travelers to render remote fishing visuals.
        /// </summary>
        public void ApplyRemoteState(byte rawState, Vector3 rodTipWorld, Vector3 bobberWorld, float waterSurfaceY = -2.0f)
        {
            FishingState state = (FishingState)rawState;
            UpdateVisuals(
                state,
                rodTipWorld,
                bobberWorld,
                rodTipWorld + Vector3.up * 0.65f,
                waterSurfaceY,
                false,
                Time.time);
        }

        void OnDestroy()
        {
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

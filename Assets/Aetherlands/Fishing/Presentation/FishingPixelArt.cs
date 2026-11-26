using System.Collections.Generic;
using UnityEngine;

namespace Aetherlands.Fishing
{
    /// <summary>
    /// Procedural point-filtered pixel art generator matching the Aetherlands Art Bible palette.
    /// Produces crisp sprites for world fishing props, UI frames, and coastal fish species.
    /// All generated Texture2D and Sprite instances are tracked in the caller's ownedResources list.
    /// </summary>
    public static class FishingPixelArt
    {
        public static readonly Color32 Ink = new Color32(52, 52, 72, 255);
        public static readonly Color32 Navy = new Color32(52, 77, 99, 255);
        public static readonly Color32 DeepWater = new Color32(30, 82, 102, 255);
        public static readonly Color32 MidTeal = new Color32(52, 139, 137, 255);
        public static readonly Color32 MintFoam = new Color32(131, 199, 170, 255);
        public static readonly Color32 TimberDark = new Color32(107, 71, 52, 255);
        public static readonly Color32 TimberMid = new Color32(161, 108, 68, 255);
        public static readonly Color32 TimberLight = new Color32(198, 148, 98, 255);
        public static readonly Color32 Parchment = new Color32(238, 216, 160, 255);
        public static readonly Color32 Cream = new Color32(246, 232, 196, 255);
        public static readonly Color32 Coral = new Color32(191, 91, 67, 255);
        public static readonly Color32 Gold = new Color32(240, 187, 102, 255);
        public static readonly Color32 LanternGlow = new Color32(255, 214, 128, 255);
        public static readonly Color32 CatchGreenDark = new Color32(60, 130, 89, 255);
        public static readonly Color32 CatchGreen = new Color32(114, 166, 79, 255);
        public static readonly Color32 CatchGreenLight = new Color32(164, 212, 114, 255);

        sealed class PixelGrid
        {
            public readonly int Width;
            public readonly int Height;
            public readonly Color32[] Pixels;

            public PixelGrid(int width, int height)
            {
                Width = width;
                Height = height;
                Pixels = new Color32[width * height];
            }

            public void Set(int x, int y, Color32 color)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                Pixels[y * Width + x] = color;
            }

            public Color32 Get(int x, int y)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) return default;
                return Pixels[y * Width + x];
            }

            public void Rect(int x, int y, int w, int h, Color32 color)
            {
                for (int j = y; j < y + h; j++)
                {
                    for (int i = x; i < x + w; i++)
                    {
                        Set(i, j, color);
                    }
                }
            }

            public void Outline(Color32 outlineColor)
            {
                var copy = new Color32[Pixels.Length];
                System.Array.Copy(Pixels, copy, Pixels.Length);

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (copy[y * Width + x].a != 0) continue;
                        bool touchesOpaque =
                            (x > 0 && copy[y * Width + (x - 1)].a > 0) ||
                            (x < Width - 1 && copy[y * Width + (x + 1)].a > 0) ||
                            (y > 0 && copy[(y - 1) * Width + x].a > 0) ||
                            (y < Height - 1 && copy[(y + 1) * Width + x].a > 0);
                        if (touchesOpaque)
                        {
                            Pixels[y * Width + x] = outlineColor;
                        }
                    }
                }
            }
        }

        static Sprite BuildSprite(PixelGrid grid, float pixelsPerUnit, Vector2 pivot, Vector4 border, List<Object> ownedResources)
        {
            var tex = new Texture2D(grid.Width, grid.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels32(grid.Pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, grid.Width, grid.Height), pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            if (ownedResources != null)
            {
                ownedResources.Add(tex);
                ownedResources.Add(sprite);
            }

            return sprite;
        }

        public static Sprite CreateBobberSprite(List<Object> ownedResources)
        {
            var g = new PixelGrid(16, 16);
            // Stem + brass eyelet at top.
            g.Rect(7, 11, 2, 3, TimberMid);
            g.Set(7, 14, Gold);
            g.Set(8, 14, Gold);

            // Round cork body: coral top half, cream bottom half.
            for (int y = 3; y <= 11; y++)
            {
                for (int x = 4; x <= 11; x++)
                {
                    float dx = (x - 7.5f) / 3.8f;
                    float dy = (y - 7.0f) / 4.2f;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        Color32 c = y >= 7 ? Coral : Cream;
                        if (y == 10 && x >= 5 && x <= 7) c = LanternGlow;
                        if (y == 4 && x >= 6 && x <= 9) c = Parchment;
                        g.Set(x, y, c);
                    }
                }
            }

            // Gold belly band.
            g.Rect(4, 7, 8, 1, Gold);
            g.Outline(Ink);

            return BuildSprite(g, 32f, new Vector2(0.5f, 0.35f), Vector4.zero, ownedResources);
        }

        public static Sprite[] CreateRippleSprites(List<Object> ownedResources)
        {
            var frames = new Sprite[3];
            for (int f = 0; f < 3; f++)
            {
                var g = new PixelGrid(32, 20);
                float radiusX = 5.5f + f * 3.8f;
                float radiusY = 3.2f + f * 2.1f;
                byte alpha = (byte)(210 - f * 48);

                for (int y = 0; y < g.Height; y++)
                {
                    for (int x = 0; x < g.Width; x++)
                    {
                        float nx = (x - 15.5f) / radiusX;
                        float ny = (y - 9.5f) / radiusY;
                        float d = Mathf.Sqrt(nx * nx + ny * ny);
                        if (Mathf.Abs(d - 1f) < 0.16f)
                        {
                            g.Set(x, y, new Color32(236, 248, 242, alpha));
                        }
                        else if (f > 0 && Mathf.Abs(d - 0.56f) < 0.14f)
                        {
                            g.Set(x, y, new Color32(131, 199, 170, (byte)(alpha * 0.7f)));
                        }
                    }
                }

                frames[f] = BuildSprite(g, 32f, new Vector2(0.5f, 0.5f), Vector4.zero, ownedResources);
            }

            return frames;
        }

        public static Sprite CreateExclamationSprite(List<Object> ownedResources)
        {
            var g = new PixelGrid(16, 24);
            // Tapered exclamation stem.
            g.Rect(6, 9, 4, 12, Gold);
            g.Rect(7, 10, 2, 10, LanternGlow);
            g.Rect(6, 9, 4, 2, Coral);

            // Bottom dot.
            g.Rect(6, 3, 4, 4, Gold);
            g.Rect(7, 4, 2, 2, LanternGlow);
            g.Set(6, 3, Coral);
            g.Set(9, 3, Coral);

            g.Outline(Ink);
            return BuildSprite(g, 32f, new Vector2(0.5f, 0.0f), Vector4.zero, ownedResources);
        }

        public static Sprite CreateFramedPanelSprite(Color32 fillColor, List<Object> ownedResources)
        {
            const int size = 24;
            var g = new PixelGrid(size, size);
            g.Rect(0, 0, size, size, Ink);
            g.Rect(1, 1, size - 2, size - 2, TimberDark);
            g.Rect(2, 2, size - 4, size - 4, TimberMid);
            g.Rect(2, size - 4, size - 4, 1, TimberLight);
            g.Rect(2, 2, 1, size - 4, TimberLight);
            g.Rect(4, 4, size - 8, size - 8, Ink);
            g.Rect(5, 5, size - 10, size - 10, fillColor);

            // Corner brass rivets.
            g.Set(2, 2, Gold);
            g.Set(size - 3, 2, Gold);
            g.Set(2, size - 3, Gold);
            g.Set(size - 3, size - 3, Gold);

            return BuildSprite(g, 16f, new Vector2(0.5f, 0.5f), new Vector4(6, 6, 6, 6), ownedResources);
        }

        public static Sprite CreateWaterColumnSprite(List<Object> ownedResources)
        {
            const int w = 24;
            const int h = 128;
            var g = new PixelGrid(w, h);

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                Color32 baseCol = Color32.Lerp(DeepWater, MidTeal, t);
                if (y > h - 10)
                {
                    baseCol = Color32.Lerp(MidTeal, MintFoam, (y - (h - 10)) / 9f);
                }

                for (int x = 0; x < w; x++)
                {
                    Color32 c = baseCol;
                    // Subtle pixel-art water bands and vertical caustic glints.
                    if ((y % 12 == 0 || y % 12 == 1) && ((x + y / 4) % 7 < 3))
                    {
                        c = Color32.Lerp(c, MintFoam, 0.28f);
                    }
                    else if (x == 3 || x == 4)
                    {
                        c = Color32.Lerp(c, Cream, 0.14f);
                    }
                    else if (x == w - 2 || x == w - 1)
                    {
                        c = Color32.Lerp(c, Navy, 0.25f);
                    }

                    g.Set(x, y, c);
                }
            }

            // Seafoam crest line at the top.
            g.Rect(0, h - 2, w, 2, Cream);
            return BuildSprite(g, 16f, new Vector2(0.5f, 0.5f), Vector4.zero, ownedResources);
        }

        public static Sprite CreateCatchBarSprite(List<Object> ownedResources)
        {
            const int w = 20;
            const int h = 28;
            var g = new PixelGrid(w, h);

            g.Rect(0, 0, w, h, Ink);
            g.Rect(1, 1, w - 2, h - 2, CatchGreenDark);
            g.Rect(2, 2, w - 4, h - 4, CatchGreen);
            g.Rect(2, h - 4, w - 4, 2, CatchGreenLight);
            g.Rect(3, 3, 2, h - 6, CatchGreenLight);

            // Subtle notch indicators in the center of the catch bar.
            int midY = h / 2;
            g.Rect(2, midY, 2, 1, Cream);
            g.Rect(w - 4, midY, 2, 1, Cream);

            return BuildSprite(g, 16f, new Vector2(0.5f, 0.5f), new Vector4(4, 4, 4, 4), ownedResources);
        }

        public static Sprite CreateSolidSprite(Color32 color, List<Object> ownedResources)
        {
            var g = new PixelGrid(4, 4);
            g.Rect(0, 0, 4, 4, color);
            return BuildSprite(g, 16f, new Vector2(0.5f, 0.5f), Vector4.zero, ownedResources);
        }

        public static Sprite CreateFishIconSprite(FishSpeciesDefinition species, List<Object> ownedResources)
        {
            var g = new PixelGrid(24, 16);

            Color32 body = species != null ? (Color32)species.bodyColor : MidTeal;
            Color32 belly = species != null ? (Color32)species.bellyColor : Cream;
            Color32 fin = species != null ? (Color32)species.finColor : Navy;
            Color32 accent = species != null ? (Color32)species.accentColor : Gold;
            FishSilhouetteStyle style = species != null ? species.silhouette : FishSilhouetteStyle.Slender;

            float rx = style == FishSilhouetteStyle.Tuna ? 7.4f : style == FishSilhouetteStyle.Slender ? 7.2f : 6.4f;
            float ry = style == FishSilhouetteStyle.Slender ? 3.2f : style == FishSilhouetteStyle.Bream ? 5.1f : 4.4f;
            float cx = 12.8f;
            float cy = 7.8f;

            // Tail fin (left side).
            int tailSplit = style == FishSilhouetteStyle.Tuna ? 4 : 3;
            for (int x = 2; x <= 6; x++)
            {
                int spread = 1 + (6 - x);
                for (int y = 8 - spread; y <= 8 + spread; y++)
                {
                    if (x == 2 && Mathf.Abs(y - 8) < tailSplit - 1) continue;
                    g.Set(x, y, fin);
                }
            }

            // Dorsal and ventral fins.
            int dorsalTop = Mathf.Min(14, Mathf.RoundToInt(cy + ry) + 2);
            g.Rect(10, Mathf.RoundToInt(cy + ry) - 1, 5, Mathf.Max(2, dorsalTop - Mathf.RoundToInt(cy + ry) + 1), fin);
            g.Rect(11, Mathf.Max(1, Mathf.RoundToInt(cy - ry) - 1), 3, 2, fin);

            // Body ellipse with belly shading and accent stripe.
            for (int y = 2; y < 14; y++)
            {
                for (int x = 6; x < 22; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        Color32 c = dy < -0.18f ? belly : body;
                        if (Mathf.Abs(dy - 0.15f) < 0.22f && x >= 9 && x <= 17)
                        {
                            c = accent;
                        }
                        if (style == FishSilhouetteStyle.Snapper && x % 3 == 0 && y > cy)
                        {
                            c = accent;
                        }
                        g.Set(x, y, c);
                    }
                }
            }

            // Pectoral fin + gill + eye.
            g.Rect(12, 6, 3, 2, fin);
            g.Set(15, 7, Ink);
            g.Set(15, 8, Ink);
            g.Set(18, 9, Cream);
            g.Set(19, 9, Ink);

            g.Outline(Ink);
            return BuildSprite(g, 24f, new Vector2(0.5f, 0.5f), Vector4.zero, ownedResources);
        }
    }
}

using UnityEngine;

namespace Aetherlands.Fishing
{
    /// <summary>
    /// Detects valid water casting targets in front of the traveler using coastal bounds
    /// and downward physics raycasts to avoid landing the bobber on the bridge deck or cliffs.
    /// </summary>
    public static class CoastalWaterSpotDetector
    {
        const float WorldWaterHalfExtent = 54f;

        /// <summary>
        /// Checks whether a valid water casting spot exists in front of the traveler.
        /// </summary>
        public static bool TryFindWaterSpot(
            Vector3 origin,
            Vector3 facingDirection,
            float normalizedPower,
            FishingConfig config,
            out Vector3 waterPoint)
        {
            float waterY = config != null ? config.waterSurfaceY : -2.0f;
            float minDist = config != null ? config.minCastDistance : 2.4f;
            float maxDist = config != null ? config.maxCastDistance : 5.4f;
            float step = config != null ? Mathf.Max(0.2f, config.waterSearchStep) : 0.45f;

            Vector3 dir = new Vector3(facingDirection.x, 0f, facingDirection.z);
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.right;
            }
            else
            {
                dir.Normalize();
            }

            float targetDist = Mathf.Lerp(minDist, maxDist, Mathf.Clamp01(normalizedPower));

            // 1. Check the exact target distance along facing direction first.
            if (IsWaterPoint(origin, dir, targetDist, waterY, out waterPoint))
            {
                return true;
            }

            // 2. Search outward and inward along the facing ray within [minDist, maxDist].
            for (float offset = step; offset <= (maxDist - minDist); offset += step)
            {
                float farther = targetDist + offset;
                if (farther <= maxDist && IsWaterPoint(origin, dir, farther, waterY, out waterPoint))
                {
                    return true;
                }

                float nearer = targetDist - offset;
                if (nearer >= minDist && IsWaterPoint(origin, dir, nearer, waterY, out waterPoint))
                {
                    return true;
                }
            }

            // 3. Slight angular fan (+/- 22 degrees) for comfortable casting along curved shorelines.
            const float fanRad = 22f * Mathf.Deg2Rad;
            float cos = Mathf.Cos(fanRad);
            float sin = Mathf.Sin(fanRad);
            Vector3 leftDir = new Vector3(dir.x * cos - dir.z * sin, 0f, dir.x * sin + dir.z * cos);
            Vector3 rightDir = new Vector3(dir.x * cos + dir.z * sin, 0f, -dir.x * sin + dir.z * cos);

            if (IsWaterPoint(origin, leftDir, targetDist, waterY, out waterPoint)) return true;
            if (IsWaterPoint(origin, rightDir, targetDist, waterY, out waterPoint)) return true;

            waterPoint = new Vector3(origin.x + dir.x * targetDist, waterY, origin.z + dir.z * targetDist);
            return false;
        }

        static bool IsWaterPoint(Vector3 origin, Vector3 flatDir, float distance, float waterY, out Vector3 waterPoint)
        {
            float x = origin.x + flatDir.x * distance;
            float z = origin.z + flatDir.z * distance;
            waterPoint = new Vector3(x, waterY, z);

            if (Mathf.Abs(x) > WorldWaterHalfExtent || Mathf.Abs(z) > WorldWaterHalfExtent)
            {
                return false;
            }

            // Analytical exclusion of coastal deck & main islands so bobbers land in clear water
            // beyond cliff overhangs even where colliders sit slightly inside the visual rim.
            if (IsOverCoastalLand(x, z))
            {
                return false;
            }

            // Physics check against any scene colliders (deck, landings, props, custom docks).
            float rayStartY = Mathf.Max(origin.y + 2.5f, waterY + 4.5f);
            float rayLength = (rayStartY - waterY) - 0.15f;
            if (rayLength > 0.1f)
            {
                if (Physics.Raycast(new Vector3(x, rayStartY, z), Vector3.down, out RaycastHit hit, rayLength, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.point.y > waterY + 0.15f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Pure geometric check matching the CoastalLandscape bridge deck and north/south islands.
        /// </summary>
        public static bool IsOverCoastalLand(float x, float z)
        {
            // Bridge deck span (with railing margin).
            if (Mathf.Abs(x) <= 1.85f && z >= -8.35f && z <= 4.85f)
            {
                return true;
            }

            // North landing island ellipse: center (0, 8), visual rx=6.6, rz=5.6.
            if (InsideEllipse(x, z, 0f, 8f, 6.75f, 5.75f))
            {
                return true;
            }

            // South landing island ellipse: center (0, -12), visual rx=7.0, rz=5.5.
            if (InsideEllipse(x, z, 0f, -12f, 7.15f, 5.65f))
            {
                return true;
            }

            return false;
        }

        static bool InsideEllipse(float x, float z, float cx, float cz, float rx, float rz)
        {
            float nx = (x - cx) / rx;
            float nz = (z - cz) / rz;
            return nx * nx + nz * nz <= 1f;
        }
    }
}

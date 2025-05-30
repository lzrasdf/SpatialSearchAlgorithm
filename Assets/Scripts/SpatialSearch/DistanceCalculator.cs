using UnityEngine;

namespace SpatialSearchAlgorithm
{
    /// <summary>
    /// 距离计算控制类，用于管理是否使用开方计算
    /// </summary>
    public static class DistanceCalculator
    {
        public static bool UseSqrt = false;

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        public static float CalculateDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            float dz = a.z - b.z;
            float distanceSquared = dx * dx + dy * dy + dz * dz;
            
            return UseSqrt ? Mathf.Sqrt(distanceSquared) : distanceSquared;
        }

        /// <summary>
        /// 计算两点之间的距离（2D版本）
        /// </summary>
        public static float CalculateDistance2D(Vector2 a, Vector2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            float distanceSquared = dx * dx + dy * dy;
            
            return UseSqrt ? Mathf.Sqrt(distanceSquared) : distanceSquared;
        }

        /// <summary>
        /// 比较距离是否在范围内
        /// </summary>
        public static bool IsInRange(float distance, float radius)
        {
            return UseSqrt ? distance <= radius : distance <= radius * radius;
        }
    }
} 
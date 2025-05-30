using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpatialSearchAlgorithm
{
    /// <summary>
    /// 空间测试环境
    /// </summary>
    public class SpatialTestEnvironment
    {
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float MinRadius { get; private set; }
        public float MaxRadius { get; private set; }
        public float CircleSpeed { get; private set; }
        public List<Circle> Circles { get; private set; }
        private System.Random random;

        public SpatialTestEnvironment(
            float width = 1000f,
            float height = 1000f,
            float minRadius = 5f,
            float maxRadius = 20f,
            float circleSpeed = 100f)
        {
            Width = width;
            Height = height;
            MinRadius = minRadius;
            MaxRadius = maxRadius;
            CircleSpeed = circleSpeed;
            Circles = new List<Circle>();
            random = new System.Random();
        }

        /// <summary>
        /// 生成指定数量的不重叠的圆
        /// </summary>
        public List<Circle> GenerateCircles(int numCircles, int maxAttempts = 1000)
        {
            Circles.Clear();

            for (int i = 0; i < numCircles; i++)
            {
                int attempts = 0;
                while (attempts < maxAttempts)
                {
                    // 随机生成圆心和半径
                    float x = (float)(random.NextDouble() * Width);
                    float y = (float)(random.NextDouble() * Height);
                    float radius = (float)(random.NextDouble() * (MaxRadius - MinRadius) + MinRadius);

                    var newCircle = new Circle(x, y, radius, CircleSpeed);

                    // 检查是否在边界内
                    if (newCircle.X - newCircle.Radius < 0 ||
                        newCircle.X + newCircle.Radius > Width ||
                        newCircle.Y - newCircle.Radius < 0 ||
                        newCircle.Y + newCircle.Radius > Height)
                    {
                        attempts++;
                        continue;
                    }

                    // 检查是否与其他圆重叠
                    // bool overlaps = Circles.Any(existingCircle => newCircle.Overlaps(existingCircle));

                    // if (!overlaps)
                    // {
                        Circles.Add(newCircle);
                        break;
                    // }

                    attempts++;
                }

                if (attempts >= maxAttempts)
                {
                    Debug.LogWarning($"Could not place circle after {maxAttempts} attempts");
                }
            }

            return Circles;
        }

        /// <summary>
        /// 清空环境
        /// </summary>
        public void Clear()
        {
            Circles.Clear();
        }

        /// <summary>
        /// 获取当前环境中的所有圆
        /// </summary>
        public List<Circle> GetCircles()
        {
            return new List<Circle>(Circles);
        }
    }
} 
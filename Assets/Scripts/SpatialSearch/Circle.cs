using UnityEngine;

namespace SpatialSearchAlgorithm
{
    /// <summary>
    /// 表示一个圆形对象
    /// </summary>
    public class Circle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Radius { get; set; }
        private Vector2 _velocity;
        public Vector2 Velocity 
        { 
            get => _velocity;
            set => _velocity = value;
        }
        public float Speed { get; set; }

        public Circle(float x, float y, float radius, float speed = 100f)
        {
            X = x;
            Y = y;
            Radius = radius;
            Speed = speed;
            // 随机初始方向
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            _velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Speed;
        }

        /// <summary>
        /// 计算到另一个圆的距离
        /// </summary>
        public float DistanceTo(Circle other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 检查是否与另一个圆重叠
        /// </summary>
        public bool Overlaps(Circle other)
        {
            return DistanceTo(other) < (Radius + other.Radius);
        }

        /// <summary>
        /// 转换为Vector2（用于Unity中的位置表示）
        /// </summary>
        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }

        /// <summary>
        /// 更新位置
        /// </summary>
        public void UpdatePosition(float deltaTime)
        {
            X += Velocity.x * deltaTime;
            Y += Velocity.y * deltaTime;
        }

        /// <summary>
        /// 处理边界碰撞
        /// </summary>
        public void HandleBoundaryCollision(float width, float height)
        {
            // 检查左右边界
            if (X - Radius < 0)
            {
                X = Radius;
                _velocity.x = Mathf.Abs(_velocity.x);
            }
            else if (X + Radius > width)
            {
                X = width - Radius;
                _velocity.x = -Mathf.Abs(_velocity.x);
            }

            // 检查上下边界
            if (Y - Radius < 0)
            {
                Y = Radius;
                _velocity.y = Mathf.Abs(_velocity.y);
            }
            else if (Y + Radius > height)
            {
                Y = height - Radius;
                _velocity.y = -Mathf.Abs(_velocity.y);
            }
        }
    }
} 
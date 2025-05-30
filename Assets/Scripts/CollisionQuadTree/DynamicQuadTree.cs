using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CollisionQuadTree
{
    /// <summary>
    /// 动态增长的四叉树
    /// </summary>
    public class DynamicQuadTree<T>
    {
        private QuadTree<T> activeTree;
        private readonly int maxDepth;
        private readonly int maxItemCount;
        private readonly HashSet<Entity<T>> trackedEntities; // 跟踪的实体
        
        // 收缩检查相关参数
        private float lastContractionCheckTime = 0f;
        private readonly float contractionCheckInterval = 10f; // 定期检查一次是否需要收缩
        private readonly float contractionThreshold = 0.3f; // 当实体分布范围小于当前范围的30%时考虑收缩
        private readonly float contractionPadding = 200f; // 收缩后的边界padding
        private bool autoContractionEnabled = true; // 是否启用自动收缩

        public DynamicQuadTree(int maxDepth = 8, int maxItemCount = 16)
        {
            this.maxDepth = maxDepth;
            this.maxItemCount = maxItemCount;
            this.trackedEntities = new HashSet<Entity<T>>();
            
            // 初始化一个合理大小的四叉树
            var initialRect = new Rect(-10f, -10f, 20f, 20f); // 可以根据实际需求调整初始大小
            activeTree = new QuadTree<T>(maxDepth, maxItemCount, initialRect);
        }

        public void Reset()
        {
            // 清除所有实体的回调
            foreach (var entity in trackedEntities)
            {
                entity.SetOnRectChanged(null);
            }
            trackedEntities.Clear();
            
            var initialRect = new Rect(-10f, -10f, 20f, 20f); // 可以根据实际需求调整初始大小
            activeTree = new QuadTree<T>(maxDepth, maxItemCount, initialRect);
            lastContractionCheckTime = 0f;
            autoContractionEnabled = true; // 重置时恢复默认值
        }
        
        /// <summary>
        /// 智能添加实体，如果超出范围则扩展到最近的合适区域
        /// </summary>
        public bool Add(Entity<T> entity)
        {
            var entityRect = entity.Rect;

            if (!IsInsideRoot(entityRect))
            {
                // 检查并扩展四叉树
                CheckAndExpand(entityRect);
            }

            // 添加实体
            // 设置位置变更回调并跟踪实体
            if (trackedEntities.Add(entity))
            {
                entity.SetOnRectChanged(OnEntityRectChanged);
            }
            return activeTree.Add(entity);
        }
        
        /// <summary>
        /// 实体位置变更回调
        /// </summary>
        private void OnEntityRectChanged(Entity<T> entity)
        {
            // 检查是否需要扩展四叉树
            CheckAndExpand(entity.Rect);
        }
        
        /// <summary>
        /// 检查矩形是否超出范围，如果超出则扩展四叉树
        /// </summary>
        public void CheckAndExpand(Rect rect)
        {
            var rootRect = activeTree.Root.Rect;

            // 如果在当前范围内，不需要扩展
            if (IsInsideRoot(rect))
            {
                return;
            }

            // 计算新的四叉树范围
            // 使用所有实体的最大坐标范围再加上固定范围值
            float minX = Mathf.Min(rootRect.xMin, rect.xMin);
            float minY = Mathf.Min(rootRect.yMin, rect.yMin);
            float maxX = Mathf.Max(rootRect.xMax, rect.xMax);
            float maxY = Mathf.Max(rootRect.yMax, rect.yMax);
            
            // 添加固定范围值作为边界
            float padding = 50f; // 可以根据实际需求调整边界值
            minX -= padding;
            minY -= padding;
            maxX += padding;
            maxY += padding;
            
            // 创建新的矩形范围
            Rect newRect = new Rect(
                minX,
                minY,
                maxX - minX,
                maxY - minY
            );
            
            // 收集当前所有实体
            var entities = new HashSet<Entity<T>>();
            foreach (var entity in activeTree.AllEntities)
            {
                entities.Add(entity);
            }
            
            // 创建新的四叉树
            var newTree = new QuadTree<T>(maxDepth, maxItemCount, newRect);
            
            // 更新活动树
            activeTree = newTree;
            
            // 一次性添加所有已有实体
            foreach (var existingEntity in entities)
            {
                activeTree.Add(existingEntity);
            }
        }

        /// <summary>
        /// 检查是否可以收缩四叉树范围
        /// </summary>
        private void CheckAndContract()
        {
            if (trackedEntities.Count == 0)
            {
                // 如果没有实体，重置到初始大小
                var initialRect = new Rect(-10f, -10f, 20f, 20f);
                var newTree = new QuadTree<T>(maxDepth, maxItemCount, initialRect);
                activeTree = newTree;
                return;
            }

            // 计算所有实体的边界
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (var entity in trackedEntities)
            {
                var rect = entity.Rect;
                minX = Mathf.Min(minX, rect.xMin);
                minY = Mathf.Min(minY, rect.yMin);
                maxX = Mathf.Max(maxX, rect.xMax);
                maxY = Mathf.Max(maxY, rect.yMax);
            }

            // 添加padding
            minX -= contractionPadding;
            minY -= contractionPadding;
            maxX += contractionPadding;
            maxY += contractionPadding;

            // 计算实体分布范围与当前四叉树范围的比例
            var currentRect = activeTree.Root.Rect;
            float entityWidth = maxX - minX;
            float entityHeight = maxY - minY;
            float currentWidth = currentRect.width;
            float currentHeight = currentRect.height;

            float widthRatio = entityWidth / currentWidth;
            float heightRatio = entityHeight / currentHeight;

            // 如果实体分布范围明显小于当前范围，则考虑收缩
            if (widthRatio < contractionThreshold && heightRatio < contractionThreshold)
            {
                // 创建新的收缩后的矩形范围
                Rect newRect = new Rect(minX, minY, maxX - minX, maxY - minY);
                
                // 收集当前所有实体
                var entities = new HashSet<Entity<T>>();
                foreach (var entity in activeTree.AllEntities)
                {
                    entities.Add(entity);
                }
                
                // 创建新的四叉树
                var newTree = new QuadTree<T>(maxDepth, maxItemCount, newRect);
                
                // 更新活动树
                activeTree = newTree;
                
                // 一次性添加所有已有实体
                foreach (var existingEntity in entities)
                {
                    activeTree.Add(existingEntity);
                }
            }
        }

        private bool IsInsideRoot(Rect entityRect)
        {
            var rootRect = activeTree.Root.Rect;
            return IsInsideRect(entityRect, rootRect);
        }
        
        private bool IsInsideRect(Rect entityRect, Rect containerRect)
        {
            return containerRect.Contains(new Vector2(entityRect.xMin, entityRect.yMin)) &&
                   containerRect.Contains(new Vector2(entityRect.xMax, entityRect.yMax));
        }

        // 代理原有四叉树的方法
        public void Update()
        {
            // 更新四叉树
            activeTree.Update();
            
            // 处理被标记为移除的实体
            var removedEntities = new List<Entity<T>>();
            foreach (var entity in trackedEntities)
            {
                if (entity.Removed)
                {
                    removedEntities.Add(entity);
                }
            }
            
            // 清除被移除实体的回调
            foreach (var entity in removedEntities)
            {
                entity.SetOnRectChanged(null);
                trackedEntities.Remove(entity);
            }
            
            // 周期性检查是否需要收缩
            if (autoContractionEnabled)
            {
                float currentTime = Time.time;
                if (currentTime - lastContractionCheckTime >= contractionCheckInterval)
                {
                    CheckAndContract();
                    lastContractionCheckTime = currentTime;
                }
            }
        }
        
        /// <summary>
        /// 设置是否启用自动收缩
        /// </summary>
        /// <param name="enabled">是否启用自动收缩</param>
        public void SetAutoContraction(bool enabled)
        {
            autoContractionEnabled = enabled;
            if (!enabled)
            {
                lastContractionCheckTime = 0f; // 重置检查时间
            }
        }

        /// <summary>
        /// 获取当前是否启用自动收缩
        /// </summary>
        public bool IsAutoContractionEnabled => autoContractionEnabled;

        public HashSet<Entity<T>> Query(Rect queryRect, HashSet<Entity<T>> results = null) 
            => activeTree.Query(queryRect, results);
        
        public void MarkRemove(T item) => activeTree.MarkRemove(item);
        
        public Rect Bounds => activeTree.Root.Rect;
    }
}
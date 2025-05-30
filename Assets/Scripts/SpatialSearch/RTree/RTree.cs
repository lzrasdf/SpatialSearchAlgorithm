using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SpatialSearchAlgorithm;

/// <summary>
/// R树（R-Tree）空间索引结构
/// 一种用于空间数据的树形数据结构，通过将空间递归地划分为更小的区域，每个区域用一个最小外接矩形（MBR）表示
/// 适用于2D/3D空间中的快速范围查询和最近邻搜索
/// 
/// 优点：
/// 1. 动态性能好：支持动态插入和删除
/// 2. 查询效率高：可以快速定位到目标区域
/// 3. 空间利用率高：通过最小外接矩形减少空间重叠
/// 
/// 缺点：
/// 1. 实现复杂度高：需要处理节点分裂和合并
/// 2. 内存开销大：每个节点都需要存储边界框
/// </summary>
public class RTree : ISpatialSearch
{
    /// <summary>
    /// R树节点类，表示树中的一个节点
    /// </summary>
    private class RNode
    {
        /// <summary>
        /// 节点的边界框（最小外接矩形）
        /// </summary>
        public Bounds bounds;
        
        /// <summary>
        /// 叶子节点中存储的对象列表
        /// 每个对象包含：位置、半径和关联数据
        /// </summary>
        public List<(Vector3 position, float radius, object data)> objects;
        
        /// <summary>
        /// 子节点列表
        /// </summary>
        public List<RNode> children;
        
        /// <summary>
        /// 是否为叶子节点（没有子节点）
        /// </summary>
        public bool isLeaf;
        
        /// <summary>
        /// 节点在树中的深度
        /// </summary>
        public int depth;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bounds">节点的边界框</param>
        /// <param name="depth">节点深度</param>
        public RNode(Bounds bounds, int depth)
        {
            this.bounds = bounds;
            this.objects = new List<(Vector3, float, object)>();
            this.children = new List<RNode>();
            this.isLeaf = true;
            this.depth = depth;
        }

        /// <summary>
        /// 更新节点的边界框
        /// 如果是叶子节点，根据存储的对象更新边界
        /// 如果是内部节点，根据子节点的边界更新边界
        /// </summary>
        public void UpdateBounds()
        {
            if (isLeaf)
            {
                if (objects.Count == 0) return;
                
                bounds = new Bounds(objects[0].position, Vector3.zero);
                foreach (var obj in objects)
                {
                    bounds.Encapsulate(obj.position);
                }
            }
            else
            {
                if (children.Count == 0) return;
                
                bounds = children[0].bounds;
                foreach (var child in children)
                {
                    bounds.Encapsulate(child.bounds);
                }
            }
        }
    }

    /// <summary>
    /// 树的根节点
    /// </summary>
    private RNode root;
    
    /// <summary>
    /// 每个节点最多存储的对象数量
    /// </summary>
    private readonly int maxEntries;
    
    /// <summary>
    /// 每个节点最少存储的对象数量
    /// </summary>
    private readonly int minEntries;
    
    /// <summary>
    /// 树的最大深度
    /// </summary>
    private readonly int maxDepth;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="center">R树的中心点</param>
    /// <param name="size">R树的大小</param>
    /// <param name="maxEntries">每个节点最多存储的对象数量</param>
    /// <param name="minEntries">每个节点最少存储的对象数量</param>
    /// <param name="maxDepth">树的最大深度</param>
    public RTree(Vector2 center, Vector2 size, int maxEntries = 4, int minEntries = 2, int maxDepth = 8)
    {
        this.maxEntries = maxEntries;
        this.minEntries = minEntries;
        this.maxDepth = maxDepth;
        
        Bounds rootBounds = new Bounds(
            new Vector3(center.x, center.y, 0),
            new Vector3(size.x, size.y, 0)
        );
        root = new RNode(rootBounds, 0);
    }

    /// <summary>
    /// 向R树中插入一个新对象
    /// </summary>
    /// <param name="position">对象的位置</param>
    /// <param name="radius">对象的半径</param>
    /// <param name="data">要存储的数据</param>
    public void Insert(Vector3 position, float radius, object data)
    {
        Bounds objectBounds = new Bounds(position, new Vector3(radius * 2, radius * 2, 0));
        InsertIntoNode(root, position, radius, data, objectBounds);
    }

    /// <summary>
    /// 递归插入对象的辅助方法
    /// </summary>
    private void InsertIntoNode(RNode node, Vector3 position, float radius, object data, Bounds objectBounds)
    {
        if (node.isLeaf)
        {
            node.objects.Add((position, radius, data));
            node.UpdateBounds();
            
            if (node.objects.Count > maxEntries && node.depth < maxDepth)
            {
                SplitNode(node);
            }
        }
        else
        {
            RNode bestChild = FindBestChild(node, objectBounds);
            InsertIntoNode(bestChild, position, radius, data, objectBounds);
            node.UpdateBounds();
        }
    }

    /// <summary>
    /// 找到最适合插入对象的子节点
    /// 选择边界框扩展最小的子节点
    /// </summary>
    private RNode FindBestChild(RNode node, Bounds objectBounds)
    {
        float minIncrease = float.MaxValue;
        RNode bestChild = null;

        foreach (var child in node.children)
        {
            Bounds combinedBounds = child.bounds;
            combinedBounds.Encapsulate(objectBounds);
            float increase = combinedBounds.size.sqrMagnitude - child.bounds.size.sqrMagnitude;

            if (increase < minIncrease)
            {
                minIncrease = increase;
                bestChild = child;
            }
        }

        return bestChild;
    }

    /// <summary>
    /// 分裂节点
    /// 当节点中的对象数量超过最大限制时，将节点分裂为两个子节点
    /// </summary>
    private void SplitNode(RNode node)
    {
        node.isLeaf = false;
        var entries = node.objects.ToList();
        node.objects.Clear();

        // 使用改进的分裂策略
        var (leftEntries, rightEntries) = ChooseSplitAxis(entries);
        
        RNode leftNode = new RNode(CalculateBounds(leftEntries), node.depth + 1);
        RNode rightNode = new RNode(CalculateBounds(rightEntries), node.depth + 1);

        leftNode.objects.AddRange(leftEntries);
        rightNode.objects.AddRange(rightEntries);

        node.children.Add(leftNode);
        node.children.Add(rightNode);
    }

    /// <summary>
    /// 选择最佳分裂轴
    /// 通过计算每个轴上的方差，选择方差较大的轴进行分裂
    /// </summary>
    private (List<(Vector3, float, object)>, List<(Vector3, float, object)>) ChooseSplitAxis(List<(Vector3 position, float radius, object data)> entries)
    {
        // 计算每个轴上的方差
        float xVariance = CalculateVariance(entries.Select(e => e.position.x));
        float yVariance = CalculateVariance(entries.Select(e => e.position.y));

        // 选择方差较大的轴进行分裂
        var sortedEntries = xVariance > yVariance 
            ? entries.OrderBy(e => e.position.x).ToList()
            : entries.OrderBy(e => e.position.y).ToList();

        // 使用最小化重叠和边界框面积的标准来选择分裂点
        int bestSplitIndex = FindBestSplitIndex(sortedEntries);
        
        return (
            sortedEntries.Take(bestSplitIndex).ToList(),
            sortedEntries.Skip(bestSplitIndex).ToList()
        );
    }

    /// <summary>
    /// 找到最佳分裂点
    /// 通过最小化两个子节点的边界框重叠来选择分裂点
    /// </summary>
    private int FindBestSplitIndex(List<(Vector3 position, float radius, object data)> sortedEntries)
    {
        float minOverlap = float.MaxValue;
        int bestIndex = sortedEntries.Count / 2;

        for (int i = minEntries; i <= sortedEntries.Count - minEntries; i++)
        {
            var leftEntries = sortedEntries.Take(i).ToList();
            var rightEntries = sortedEntries.Skip(i).ToList();

            Bounds leftBounds = CalculateBounds(leftEntries);
            Bounds rightBounds = CalculateBounds(rightEntries);

            float overlap = CalculateOverlap(leftBounds, rightBounds);
            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// 计算两个边界框的重叠面积
    /// </summary>
    private float CalculateOverlap(Bounds bounds1, Bounds bounds2)
    {
        float overlapX = Mathf.Max(0, Mathf.Min(bounds1.max.x, bounds2.max.x) - Mathf.Max(bounds1.min.x, bounds2.min.x));
        float overlapY = Mathf.Max(0, Mathf.Min(bounds1.max.y, bounds2.max.y) - Mathf.Max(bounds1.min.y, bounds2.min.y));
        return overlapX * overlapY;
    }

    /// <summary>
    /// 计算一组数值的方差
    /// </summary>
    private float CalculateVariance(IEnumerable<float> values)
    {
        var list = values.ToList();
        float mean = list.Average();
        return list.Sum(x => (x - mean) * (x - mean)) / list.Count;
    }

    /// <summary>
    /// 计算一组对象的边界框
    /// </summary>
    private Bounds CalculateBounds(List<(Vector3 position, float radius, object data)> entries)
    {
        if (entries.Count == 0) return new Bounds();

        Bounds bounds = new Bounds(entries[0].position, Vector3.zero);
        foreach (var entry in entries)
        {
            bounds.Encapsulate(entry.position);
        }
        return bounds;
    }

    /// <summary>
    /// 查询指定位置和半径范围内的所有对象
    /// </summary>
    /// <param name="position">查询中心点</param>
    /// <param name="radius">查询半径</param>
    /// <returns>在查询范围内的所有对象数据列表</returns>
    public List<object> Query(Vector3 position, float radius)
    {
        List<object> results = new List<object>();
        Bounds queryBounds = new Bounds(position, new Vector3(radius * 2, radius * 2, 0));
        QueryNode(root, position, radius, queryBounds, results);
        return results;
    }

    /// <summary>
    /// 递归查询节点的辅助方法
    /// </summary>
    private void QueryNode(RNode node, Vector3 position, float radius, Bounds queryBounds, List<object> results)
    {
        if (!node.bounds.Intersects(queryBounds))
            return;

        if (node.isLeaf)
        {
            foreach (var obj in node.objects)
            {
                float distance = DistanceCalculator.CalculateDistance2D(
                    new Vector2(position.x, position.y),
                    new Vector2(obj.position.x, obj.position.y)
                );
                if (DistanceCalculator.IsInRange(distance, radius + obj.radius))
                {
                    results.Add(obj.data);
                }
            }
        }
        else
        {
            foreach (var child in node.children)
            {
                if (child.bounds.Intersects(queryBounds))
                {
                    QueryNode(child, position, radius, queryBounds, results);
                }
            }
        }
    }

    /// <summary>
    /// 从R树中删除指定数据的对象
    /// </summary>
    /// <param name="data">要删除的数据</param>
    public void Remove(object data)
    {
        RemoveFromNode(root, data);
    }

    /// <summary>
    /// 递归删除节点的辅助方法
    /// </summary>
    private bool RemoveFromNode(RNode node, object data)
    {
        if (node.isLeaf)
        {
            int index = node.objects.FindIndex(x => x.data == data);
            if (index != -1)
            {
                node.objects.RemoveAt(index);
                node.UpdateBounds();
                return true;
            }
            return false;
        }

        bool removed = false;
        foreach (var child in node.children)
        {
            if (RemoveFromNode(child, data))
            {
                removed = true;
            }
        }

        if (removed)
        {
            node.UpdateBounds();
            if (node.children.Count < minEntries)
            {
                RebalanceNode(node);
            }
        }

        return removed;
    }

    /// <summary>
    /// 重新平衡节点
    /// 当节点的子节点数量小于最小限制时，重新组织节点结构
    /// </summary>
    private void RebalanceNode(RNode node)
    {
        // 收集所有子节点中的对象
        var allObjects = new List<(Vector3 position, float radius, object data)>();
        foreach (var child in node.children)
        {
            allObjects.AddRange(child.objects);
        }

        // 清空子节点
        node.children.Clear();
        node.isLeaf = true;

        // 重新插入所有对象
        foreach (var obj in allObjects)
        {
            InsertIntoNode(node, obj.position, obj.radius, obj.data, new Bounds(obj.position, new Vector3(obj.radius * 2, obj.radius * 2, 0)));
        }
    }

    /// <summary>
    /// 清空R树中的所有对象
    /// </summary>
    public void Clear()
    {
        root = new RNode(root.bounds, 0);
    }

    /// <summary>
    /// 重建R树
    /// </summary>
    /// <param name="objects">要重建的对象列表</param>
    public void Rebuild(List<(Vector3 position, float radius, object data)> objects)
    {
        // 清空当前树
        Clear();

        // 如果没有对象，直接返回
        if (objects == null || objects.Count == 0)
            return;

        // 对对象进行排序，以便构建更平衡的树
        var sortedObjects = objects.OrderBy(x => x.position.x).ToList();
        
        // 批量插入对象
        foreach (var obj in sortedObjects)
        {
            Insert(obj.position, obj.radius, obj.data);
        }
    }

    /// <summary>
    /// 用于调试的可视化方法
    /// 在Unity编辑器中绘制R树的结构
    /// </summary>
    public void DebugDraw()
    {
        DrawNode(root);
    }

    /// <summary>
    /// 递归绘制节点的辅助方法
    /// </summary>
    private void DrawNode(RNode node)
    {
    }
} 
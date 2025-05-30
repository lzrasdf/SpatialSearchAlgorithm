using UnityEngine;
using System.Collections.Generic;
using SpatialSearchAlgorithm;  // 添加命名空间引用

/// <summary>
/// 四叉树（Quadtree）实现的空间搜索算法
/// 将2D空间递归地划分为四个象限，每个节点最多存储固定数量的对象
/// 当节点中的对象数量超过阈值时，节点会分裂为四个子节点
/// 适用于对象分布不均匀的场景，可以自适应地调整空间划分
/// 
/// 优点：
/// 1. 自适应划分：根据对象分布自动调整空间划分
/// 2. 查询效率：可以快速定位到目标区域
/// 3. 内存效率：空区域不会占用额外内存
/// 
/// 缺点：
/// 1. 实现复杂度：比网格划分更复杂
/// 2. 动态更新：对象移动时需要重新插入
/// </summary>
public class Quadtree : ISpatialSearch
{
    /// <summary>
    /// 四叉树节点类，表示空间中的一个区域
    /// </summary>
    private class QuadNode
    {
        /// <summary>
        /// 节点中心点
        /// </summary>
        public Vector2 center;
        
        /// <summary>
        /// 节点大小
        /// </summary>
        public Vector2 size;
        
        /// <summary>
        /// 存储在该节点中的对象列表
        /// 每个对象包含：位置、半径和关联数据
        /// </summary>
        public List<(Vector3 position, float radius, object data)> objects;
        
        /// <summary>
        /// 四个子节点，分别对应四个象限
        /// </summary>
        public QuadNode[] children;
        
        /// <summary>
        /// 是否为叶子节点（没有子节点）
        /// </summary>
        public bool isLeaf;

        public int depth;  // 添加深度属性

        public QuadNode(Vector2 center, Vector2 size, int depth = 0)
        {
            this.center = center;
            this.size = size;
            this.objects = new List<(Vector3, float, object)>();
            this.children = new QuadNode[4];
            this.isLeaf = true;
            this.depth = depth;
        }
    }

    /// <summary>
    /// 四叉树的根节点
    /// </summary>
    private QuadNode root;
    
    /// <summary>
    /// 每个节点最多存储的对象数量
    /// </summary>
    private readonly int maxObjectsPerNode;
    
    /// <summary>
    /// 最小节点大小，防止过度细分
    /// </summary>
    private readonly float minNodeSize;
    
    /// <summary>
    /// 合并阈值，当节点中的对象数量小于此值时考虑合并
    /// </summary>
    private readonly float mergeThreshold;

    private readonly int maxDepth;  // 添加最大深度参数

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="center">四叉树的中心点</param>
    /// <param name="size">四叉树覆盖的空间大小</param>
    /// <param name="maxObjectsPerNode">每个节点最多存储的对象数量</param>
    /// <param name="minNodeSize">最小节点大小</param>
    /// <param name="mergeThreshold">合并阈值</param>
    /// <param name="maxDepth">最大深度</param>
    public Quadtree(Vector2 center, Vector2 size, int maxObjectsPerNode = 16, float minNodeSize = 1f, float mergeThreshold = 8f, int maxDepth = 4)
    {
        this.root = new QuadNode(center, size);
        this.maxObjectsPerNode = maxObjectsPerNode;
        this.minNodeSize = minNodeSize;
        this.mergeThreshold = mergeThreshold;
        this.maxDepth = maxDepth;
    }

    /// <summary>
    /// 向四叉树中插入一个对象
    /// </summary>
    /// <param name="position">对象的位置</param>
    /// <param name="radius">对象的半径</param>
    /// <param name="data">要存储的对象数据</param>
    public void Insert(Vector3 position, float radius, object data)
    {
        if (!IsPointInBounds(position, radius))
        {
            // Debug.LogWarning($"Point {position} with radius {radius} is outside of quadtree bounds!");
            return;
        }
        InsertIntoNode(root, position, radius, data);
    }

    /// <summary>
    /// 检查点是否在四叉树的边界内
    /// </summary>
    private bool IsPointInBounds(Vector3 position, float radius)
    {
        Vector2 halfSize = root.size / 2f;
        return position.x - radius > root.center.x - halfSize.x &&
               position.x + radius < root.center.x + halfSize.x &&
               position.y - radius > root.center.y - halfSize.y &&
               position.y + radius < root.center.y + halfSize.y;
    }

    /// <summary>
    /// 将对象插入到指定节点中
    /// 如果节点已满，则分裂节点并重新分配对象
    /// </summary>
    private void InsertIntoNode(QuadNode node, Vector3 position, float radius, object data)
    {
        if (!node.isLeaf)
        {
            for (int i = 0; i < 4; i++)
            {
                if (IntersectsQuadrant(node, position, radius, i))
                {
                    InsertIntoNode(node.children[i], position, radius, data);
                }
            }
            return;
        }

        if (node.objects.Count < maxObjectsPerNode)
        {
            node.objects.Add((position, radius, data));
            return;
        }

        if (node.size.x <= minNodeSize || node.size.y <= minNodeSize || node.depth >= maxDepth)
        {
            node.objects.Add((position, radius, data));
            return;
        }

        Vector2 halfSize = node.size / 2f;
        Vector2 center = node.center;

        node.children[0] = new QuadNode(new Vector2(center.x - halfSize.x/2, center.y - halfSize.y/2), halfSize, node.depth + 1);
        node.children[1] = new QuadNode(new Vector2(center.x + halfSize.x/2, center.y - halfSize.y/2), halfSize, node.depth + 1);
        node.children[2] = new QuadNode(new Vector2(center.x - halfSize.x/2, center.y + halfSize.y/2), halfSize, node.depth + 1);
        node.children[3] = new QuadNode(new Vector2(center.x + halfSize.x/2, center.y + halfSize.y/2), halfSize, node.depth + 1);

        node.isLeaf = false;

        var allObjects = new List<(Vector3 position, float radius, object data)>(node.objects);
        allObjects.Add((position, radius, data));
        node.objects.Clear();

        foreach (var obj in allObjects)
        {
            for (int i = 0; i < 4; i++)
            {
                if (IntersectsQuadrant(node, obj.position, obj.radius, i))
                {
                    InsertIntoNode(node.children[i], obj.position, obj.radius, obj.data);
                }
            }
        }
    }

    /// <summary>
    /// 检查对象是否与指定象限相交
    /// </summary>
    private bool IntersectsQuadrant(QuadNode node, Vector3 position, float radius, int quadrant)
    {
        Vector2 halfSize = node.size / 2f;
        Vector2 center = node.center;

        float minX = (quadrant & 1) == 0 ? center.x - halfSize.x : center.x;
        float maxX = (quadrant & 1) == 0 ? center.x : center.x + halfSize.x;
        float minY = (quadrant & 2) == 0 ? center.y - halfSize.y : center.y;
        float maxY = (quadrant & 2) == 0 ? center.y : center.y + halfSize.y;

        float closestX = Mathf.Max(minX, Mathf.Min(position.x, maxX));
        float closestY = Mathf.Max(minY, Mathf.Min(position.y, maxY));

        if (closestX == position.x && closestY == position.y)
            return true;

        float dx = position.x - closestX;
        float dy = position.y - closestY;
        float distanceSquared = dx * dx + dy * dy;

        return distanceSquared <= (radius * radius);
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
        QueryNode(root, position, radius, results);
        return results;
    }

    /// <summary>
    /// 在指定节点中查询对象
    /// </summary>
    private void QueryNode(QuadNode node, Vector3 position, float radius, List<object> results)
    {
        if (!IntersectsCircle(node, position, radius))
            return;

        if (node.isLeaf)
        {
            foreach (var obj in node.objects)
            {
                float distance = DistanceCalculator.CalculateDistance2D(
                    new Vector2(obj.position.x, obj.position.y),
                    new Vector2(position.x, position.y)
                );
                if (DistanceCalculator.IsInRange(distance, radius + obj.radius))
                {
                    results.Add(obj.data);
                }
            }
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                if (node.children[i] != null && IntersectsCircle(node.children[i], position, radius))
                {
                    QueryNode(node.children[i], position, radius, results);
                }
            }
        }
    }

    /// <summary>
    /// 检查圆是否与节点相交
    /// </summary>
    private bool IntersectsCircle(QuadNode node, Vector3 position, float radius)
    {
        Vector2 halfSize = node.size / 2f;
        Vector2 center = node.center;

        float closestX = Mathf.Max(center.x - halfSize.x, Mathf.Min(position.x, center.x + halfSize.x));
        float closestY = Mathf.Max(center.y - halfSize.y, Mathf.Min(position.y, center.y + halfSize.y));

        float distance = DistanceCalculator.CalculateDistance2D(
            new Vector2(position.x, position.y),
            new Vector2(closestX, closestY)
        );
        return DistanceCalculator.IsInRange(distance, radius);
    }

    /// <summary>
    /// 从四叉树中移除指定对象
    /// </summary>
    /// <param name="data">要移除的对象数据</param>
    public void Remove(object data)
    {
        RemoveFromNode(root, data);
    }

    /// <summary>
    /// 从指定节点中移除对象
    /// 如果移除后节点可以合并，则进行合并
    /// </summary>
    private bool RemoveFromNode(QuadNode node, object data)
    {
        if (node.isLeaf)
        {
            int index = node.objects.FindIndex(x => x.data == data);
            if (index != -1)
            {
                node.objects.RemoveAt(index);
                return true;
            }
            return false;
        }

        bool removed = false;
        for (int i = 0; i < 4; i++)
        {
            if (node.children[i] != null)
            {
                if (RemoveFromNode(node.children[i], data))
                {
                    removed = true;
                }
            }
        }

        if (removed && CanMergeNodes(node))
        {
            MergeNodes(node);
        }
        return removed;
    }

    /// <summary>
    /// 检查节点是否可以合并
    /// 当所有子节点都是叶子节点且总对象数小于合并阈值时可以合并
    /// </summary>
    private bool CanMergeNodes(QuadNode node)
    {
        if (node.isLeaf) return false;
        
        int totalObjects = 0;
        foreach (var child in node.children)
        {
            if (!child.isLeaf) return false;
            totalObjects += child.objects.Count;
        }
        return totalObjects <= mergeThreshold;
    }

    /// <summary>
    /// 合并节点
    /// 将所有子节点的对象合并到当前节点，并删除子节点
    /// </summary>
    private void MergeNodes(QuadNode node)
    {
        node.objects.Clear();
        foreach (var child in node.children)
        {
            if (child != null)
            {
                node.objects.AddRange(child.objects);
                child.objects.Clear();
                child.children = null;
            }
        }
        node.isLeaf = true;
        node.children = new QuadNode[4];
    }

    /// <summary>
    /// 清空四叉树中的所有对象
    /// </summary>
    public void Clear()
    {
        root = new QuadNode(root.center, root.size);
    }

    /// <summary>
    /// 重建四叉树
    /// </summary>
    /// <param name="objects">要重建的对象列表</param>
    public void Rebuild(List<(Vector3 position, float radius, object data)> objects)
    {
        // 清空当前树
        Clear();

        // 如果没有对象，直接返回
        if (objects == null || objects.Count == 0)
            return;

        // 重新插入所有对象
        foreach (var obj in objects)
        {
            Insert(obj.position, obj.radius, obj.data);
        }
    }

    /// <summary>
    /// 在Unity编辑器中绘制四叉树结构，用于调试
    /// </summary>
    public void DebugDraw()
    {
        DebugDrawNode(root);
    }

    /// <summary>
    /// 递归绘制节点及其子节点
    /// </summary>
    private void DebugDrawNode(QuadNode node)
    {
        if (node == null) return;

        // 如果节点是叶子节点且没有对象，则不绘制
        if (node.isLeaf && node.objects.Count == 0) return;

        Vector2 halfSize = node.size / 2f;
        Vector3 center = new Vector3(node.center.x, node.center.y, 0);
        
        // 根据深度设置不同的颜色
        Color nodeColor = GetColorForDepth(node.depth);
        
        // 绘制节点边界
        Debug.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, 0), center + new Vector3(halfSize.x, -halfSize.y, 0), nodeColor);
        Debug.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, 0), center + new Vector3(halfSize.x, halfSize.y, 0), nodeColor);
        Debug.DrawLine(center + new Vector3(halfSize.x, halfSize.y, 0), center + new Vector3(-halfSize.x, halfSize.y, 0), nodeColor);
        Debug.DrawLine(center + new Vector3(-halfSize.x, halfSize.y, 0), center + new Vector3(-halfSize.x, -halfSize.y, 0), nodeColor);

        // 递归绘制子节点
        if (!node.isLeaf)
        {
            foreach (var child in node.children)
            {
                DebugDrawNode(child);
            }
        }
    }

    /// <summary>
    /// 根据节点深度返回对应的颜色
    /// </summary>
    private Color GetColorForDepth(int depth)
    {
        // 使用不同颜色表示不同深度
        switch (depth)
        {
            case 0: return Color.red;      // 根节点
            case 1: return Color.yellow;   // 第一层
            case 2: return Color.blue;     // 第二层
            case 3: return Color.cyan;     // 第三层
            default: return Color.magenta; // 更深层级
        }
    }
} 
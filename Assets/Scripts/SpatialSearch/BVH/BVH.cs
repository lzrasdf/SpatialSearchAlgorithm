using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SpatialSearchAlgorithm;

/// <summary>
/// 层次包围盒BVH (Bounding Volume Hierarchy) 空间索引结构
/// 通过将空间递归地划分为更小的区域，每个区域用一个包围盒表示，从而加速空间查询
/// 适用于2D/3D空间中的快速碰撞检测和空间查询
/// </summary>
public class BVH : ISpatialSearch
{
    /// <summary>
    /// BVH节点类，表示树中的一个节点
    /// </summary>
    private class BVHNode
    {
        public Bounds bounds;        // 节点的包围盒
        public List<(Vector3 position, float radius, object data)> objects;  // 叶子节点中存储的物体列表
        public BVHNode left;         // 左子节点
        public BVHNode right;        // 右子节点
        public bool isLeaf;          // 是否为叶子节点
        public int depth;           // 节点深度

        public BVHNode(Bounds bounds, int depth)
        {
            this.bounds = bounds;
            this.objects = new List<(Vector3, float, object)>();
            this.left = null;
            this.right = null;
            this.isLeaf = true;
            this.depth = depth;
        }

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
                if (left != null)
                {
                    bounds = left.bounds;
                    if (right != null)
                    {
                        bounds.Encapsulate(right.bounds);
                    }
                }
                else if (right != null)
                {
                    bounds = right.bounds;
                }
            }
        }
    }

    /// <summary>
    /// BVH中存储的物体类
    /// </summary>
    private class BVHObject
    {
        public Vector3 position;     // 物体位置
        public float radius;         // 物体半径
        public object data;          // 物体数据

        public BVHObject(Vector3 position, float radius, object data)
        {
            this.position = position;
            this.radius = radius;
            this.data = data;
        }
    }

    private BVHNode root;                    // 树的根节点
    private readonly int maxObjectsPerLeaf;  // 叶子节点中最大物体数量
    private readonly int maxDepth;           // 树的最大深度

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="maxObjectsPerLeaf">叶子节点中最大物体数量</param>
    /// <param name="maxDepth">树的最大深度</param>
    public BVH(Vector2 center, Vector2 size, int maxObjectsPerLeaf = 4, int maxDepth = 8)
    {
        this.maxObjectsPerLeaf = maxObjectsPerLeaf;
        this.maxDepth = maxDepth;
        
        Bounds rootBounds = new Bounds(
            new Vector3(center.x, center.y, 0),
            new Vector3(size.x, size.y, 0)
        );
        root = new BVHNode(rootBounds, 0);
    }

    /// <summary>
    /// 构建BVH树
    /// </summary>
    /// <param name="objects">要构建的物体列表</param>
    public void Build(List<Vector3> positions, List<float> radii, List<object> data)
    {
        // 创建BVHObject列表
        List<BVHObject> objects = new List<BVHObject>();
        for (int i = 0; i < positions.Count; i++)
        {
            objects.Add(new BVHObject(positions[i], radii[i], data[i]));
        }

        // 计算所有物体的包围盒
        Bounds bounds = new Bounds(objects[0].position, Vector3.zero);
        foreach (var obj in objects)
        {
            bounds.Encapsulate(obj.position);
        }

        // 创建根节点并开始构建树
        root = new BVHNode(bounds, 0);
        BuildNode(root, objects, 0);
    }

    /// <summary>
    /// 递归构建BVH节点
    /// </summary>
    /// <param name="node">当前节点</param>
    /// <param name="objects">当前节点包含的物体</param>
    /// <param name="depth">当前深度</param>
    private void BuildNode(BVHNode node, List<BVHObject> objects, int depth)
    {
        // 如果物体数量小于阈值或达到最大深度，创建叶子节点
        if (objects.Count <= maxObjectsPerLeaf || depth >= maxDepth)
        {
            node.isLeaf = true;
            node.objects = objects.Select(obj => (obj.position, obj.radius, obj.data)).ToList();
            return;
        }

        // 选择最佳分割轴
        int axis = GetBestSplitAxis(objects);
        
        // 按选定的轴对物体进行排序
        objects.Sort((a, b) => a.position[axis].CompareTo(b.position[axis]));
        
        // 在中间位置分割
        int mid = objects.Count / 2;
        
        // 创建左右子节点
        node.left = new BVHNode(new Bounds(), depth + 1);
        node.right = new BVHNode(new Bounds(), depth + 1);
        node.isLeaf = false;

        // 递归构建子树
        BuildNode(node.left, objects.GetRange(0, mid), depth + 1);
        BuildNode(node.right, objects.GetRange(mid, objects.Count - mid), depth + 1);

        // 更新边界
        node.left.UpdateBounds();
        node.right.UpdateBounds();
    }

    /// <summary>
    /// 计算最佳分割轴
    /// 选择物体分布最分散的轴作为分割轴
    /// </summary>
    private int GetBestSplitAxis(List<BVHObject> objects)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        // 计算所有物体在三个轴上的范围
        foreach (var obj in objects)
        {
            min = Vector3.Min(min, obj.position);
            max = Vector3.Max(max, obj.position);
        }

        // 选择范围最大的轴
        Vector3 size = max - min;
        if (size.x > size.y && size.x > size.z) return 0;
        if (size.y > size.z) return 1;
        return 2;
    }

    /// <summary>
    /// 空间查询
    /// 返回给定位置和半径范围内的所有物体
    /// </summary>
    public List<object> Query(Vector3 position, float radius)
    {
        List<object> results = new List<object>();
        // 创建查询范围的包围盒
        Bounds queryBounds = new Bounds(position, new Vector3(radius * 2, radius * 2, radius * 2));
        QueryNode(root, position, radius, queryBounds, results);
        return results;
    }

    /// <summary>
    /// 递归查询节点
    /// </summary>
    /// <param name="node">当前节点</param>
    /// <param name="position">查询位置</param>
    /// <param name="radius">查询半径</param>
    /// <param name="queryBounds">查询范围的包围盒</param>
    /// <param name="results">查询结果列表</param>
    private void QueryNode(BVHNode node, Vector3 position, float radius, Bounds queryBounds, List<object> results)
    {
        // 如果查询范围与节点包围盒不相交，直接返回
        if (!node.bounds.Intersects(queryBounds))
            return;

        // 如果是叶子节点，检查所有物体
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
        // 否则递归检查子节点
        else
        {
            if (node.left != null)
                QueryNode(node.left, position, radius, queryBounds, results);
            if (node.right != null)
                QueryNode(node.right, position, radius, queryBounds, results);
        }
    }

    public void Insert(Vector3 position, float radius, object data)
    {
        InsertIntoNode(root, position, radius, data);
    }

    private void InsertIntoNode(BVHNode node, Vector3 position, float radius, object data)
    {
        if (node.isLeaf)
        {
            node.objects.Add((position, radius, data));
            node.UpdateBounds();

            if (node.objects.Count > maxObjectsPerLeaf && node.depth < maxDepth)
            {
                SplitNode(node);
            }
        }
        else
        {
            // 选择最佳子节点
            float leftVolume = node.left != null ? node.left.bounds.size.x * node.left.bounds.size.y : float.MaxValue;
            float rightVolume = node.right != null ? node.right.bounds.size.x * node.right.bounds.size.y : float.MaxValue;

            if (leftVolume <= rightVolume)
            {
                if (node.left == null)
                {
                    node.left = new BVHNode(new Bounds(position, Vector3.zero), node.depth + 1);
                }
                InsertIntoNode(node.left, position, radius, data);
            }
            else
            {
                if (node.right == null)
                {
                    node.right = new BVHNode(new Bounds(position, Vector3.zero), node.depth + 1);
                }
                InsertIntoNode(node.right, position, radius, data);
            }
            node.UpdateBounds();
        }
    }

    private void SplitNode(BVHNode node)
    {
        // 计算所有对象的中心点
        Vector3 center = Vector3.zero;
        foreach (var obj in node.objects)
        {
            center += obj.position;
        }
        center /= node.objects.Count;

        // 根据中心点将对象分成两组
        var leftObjects = new List<(Vector3, float, object)>();
        var rightObjects = new List<(Vector3, float, object)>();

        foreach (var obj in node.objects)
        {
            if (obj.position.x < center.x)
            {
                leftObjects.Add(obj);
            }
            else
            {
                rightObjects.Add(obj);
            }
        }

        // 创建子节点
        node.left = new BVHNode(new Bounds(), node.depth + 1);
        node.right = new BVHNode(new Bounds(), node.depth + 1);
        node.isLeaf = false;

        // 将对象分配到子节点
        node.left.objects = leftObjects;
        node.right.objects = rightObjects;

        // 更新边界
        node.left.UpdateBounds();
        node.right.UpdateBounds();

        // 清空当前节点的对象列表
        node.objects.Clear();
    }

    public void Remove(object data)
    {
        RemoveFromNode(root, data);
    }

    private bool RemoveFromNode(BVHNode node, object data)
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
        if (node.left != null)
        {
            if (RemoveFromNode(node.left, data))
            {
                removed = true;
            }
        }
        if (node.right != null)
        {
            if (RemoveFromNode(node.right, data))
            {
                removed = true;
            }
        }

        if (removed)
        {
            node.UpdateBounds();
        }

        return removed;
    }

    public void Clear()
    {
        root = new BVHNode(root.bounds, 0);
    }

    /// <summary>
    /// 重建BVH树
    /// </summary>
    /// <param name="objects">要重建的对象列表</param>
    public void Rebuild(List<(Vector3 position, float radius, object data)> objects)
    {
        // 清空当前树
        Clear();

        // 如果没有对象，直接返回
        if (objects == null || objects.Count == 0)
            return;

        // 创建BVHObject列表
        List<BVHObject> bvhObjects = new List<BVHObject>();
        foreach (var obj in objects)
        {
            bvhObjects.Add(new BVHObject(obj.position, obj.radius, obj.data));
        }

        // 计算所有物体的包围盒
        Bounds bounds = new Bounds(bvhObjects[0].position, Vector3.zero);
        foreach (var obj in bvhObjects)
        {
            bounds.Encapsulate(obj.position);
        }

        // 创建根节点并开始构建树
        root = new BVHNode(bounds, 0);
        BuildNode(root, bvhObjects, 0);
    }

    public void DebugDraw()
    {
        DrawNode(root);
    }

    private void DrawNode(BVHNode node)
    {
    }
} 
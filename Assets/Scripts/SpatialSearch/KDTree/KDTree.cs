using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SpatialSearchAlgorithm;

/// <summary>
/// KD树实现，用于3D空间中的快速最近邻搜索
/// 实现了ISpatialSearch接口，支持插入、查询、删除和清空操作
/// </summary>
public class KDTree : ISpatialSearch
{
    /// <summary>
    /// KD树的节点类
    /// </summary>
    private class KDNode
    {
        public Vector3 position;    // 节点在3D空间中的位置
        public float radius;        // 节点的半径（用于范围查询）
        public object data;         // 节点存储的数据
        public KDNode left;         // 左子节点
        public KDNode right;        // 右子节点
        public int depth;           // 节点在树中的深度

        public KDNode(Vector3 position, float radius, object data, int depth)
        {
            this.position = position;
            this.radius = radius;
            this.data = data;
            this.depth = depth;
            this.left = null;
            this.right = null;
        }
    }

    private KDNode root;            // 树的根节点
    private int count;              // 树中节点的数量
    private readonly float aspectRatio;  // 空间的长宽比

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="size">空间的大小</param>
    public KDTree(Vector2 size)
    {
        this.aspectRatio = size.x / size.y;
    }

    /// <summary>
    /// 向KD树中插入一个新节点
    /// </summary>
    /// <param name="position">节点的位置</param>
    /// <param name="radius">节点的半径</param>
    /// <param name="data">要存储的数据</param>
    public void Insert(Vector3 position, float radius, object data)
    {
        root = InsertNode(root, position, radius, data, 0);
        count++;
    }

    /// <summary>
    /// 递归插入节点的辅助方法
    /// </summary>
    private KDNode InsertNode(KDNode node, Vector3 position, float radius, object data, int depth)
    {
        if (node == null)
        {
            return new KDNode(position, radius, data, depth);
        }

        // 根据深度选择分割轴（x, y, z循环）
        int axis = depth % 3;
        float comparison = axis == 0 ? position.x - node.position.x :
                          axis == 1 ? position.y - node.position.y :
                          position.z - node.position.z;

        // 根据比较结果决定插入左子树还是右子树
        if (comparison < 0)
        {
            node.left = InsertNode(node.left, position, radius, data, depth + 1);
        }
        else
        {
            node.right = InsertNode(node.right, position, radius, data, depth + 1);
        }

        return node;
    }

    /// <summary>
    /// 查询给定位置和半径范围内的所有节点
    /// </summary>
    /// <param name="position">查询中心点</param>
    /// <param name="radius">查询半径</param>
    /// <returns>范围内的所有数据对象列表</returns>
    public List<object> Query(Vector3 position, float radius)
    {
        List<object> results = new List<object>();
        QueryNode(root, position, radius, results);
        return results;
    }

    /// <summary>
    /// 递归查询节点的辅助方法
    /// </summary>
    private void QueryNode(KDNode node, Vector3 position, float radius, List<object> results)
    {
        if (node == null) return;

        // 检查当前节点是否在查询范围内（考虑两个球体的半径）
        float distance = DistanceCalculator.CalculateDistance(node.position, position);
        if (DistanceCalculator.IsInRange(distance, radius + node.radius))
        {
            results.Add(node.data);
        }

        // 根据当前节点的分割轴计算距离
        int axis = node.depth % 3;
        float axisDistance = axis == 0 ? position.x - node.position.x :
                           axis == 1 ? position.y - node.position.y :
                           position.z - node.position.z;

        // 递归检查左右子树（考虑半径的边界）
        if (axisDistance - (radius + node.radius) <= 0)
        {
            QueryNode(node.left, position, radius, results);
        }
        if (axisDistance + (radius + node.radius) >= 0)
        {
            QueryNode(node.right, position, radius, results);
        }
    }

    /// <summary>
    /// 从树中删除指定数据的节点
    /// </summary>
    /// <param name="data">要删除的数据</param>
    public void Remove(object data)
    {
        root = RemoveNode(root, data, 0);
        count--;
    }

    /// <summary>
    /// 递归删除节点的辅助方法
    /// </summary>
    private KDNode RemoveNode(KDNode node, object data, int depth)
    {
        if (node == null) return null;

        if (node.data == data)
        {
            // 找到要删除的节点，使用右子树中的最小值或左子树中的最小值替换
            if (node.right != null)
            {
                KDNode min = FindMin(node.right, depth % 3, depth + 1);
                node.position = min.position;
                node.radius = min.radius;
                node.data = min.data;
                node.right = RemoveNode(node.right, min.data, depth + 1);
            }
            else if (node.left != null)
            {
                KDNode min = FindMin(node.left, depth % 3, depth + 1);
                node.position = min.position;
                node.radius = min.radius;
                node.data = min.data;
                node.right = RemoveNode(node.left, min.data, depth + 1);
                node.left = null;
            }
            else
            {
                return null;
            }
        }
        else
        {
            // 递归搜索左右子树
            node.left = RemoveNode(node.left, data, depth + 1);
            node.right = RemoveNode(node.right, data, depth + 1);
        }

        return node;
    }

    /// <summary>
    /// 在指定轴上查找最小值节点
    /// </summary>
    private KDNode FindMin(KDNode node, int axis, int depth)
    {
        if (node == null) return null;

        int currentAxis = depth % 3;
        if (currentAxis == axis)
        {
            if (node.left == null)
                return node;
            return FindMin(node.left, axis, depth + 1);
        }

        // 比较当前节点和左右子树中的最小值
        KDNode left = FindMin(node.left, axis, depth + 1);
        KDNode right = FindMin(node.right, axis, depth + 1);

        KDNode min = node;
        if (left != null && GetAxisValue(left.position, axis) < GetAxisValue(min.position, axis))
            min = left;
        if (right != null && GetAxisValue(right.position, axis) < GetAxisValue(min.position, axis))
            min = right;

        return min;
    }

    /// <summary>
    /// 获取Vector3在指定轴上的值
    /// </summary>
    private float GetAxisValue(Vector3 position, int axis)
    {
        return axis == 0 ? position.x : axis == 1 ? position.y : position.z;
    }

    /// <summary>
    /// 清空KD树
    /// </summary>
    public void Clear()
    {
        root = null;
        count = 0;
    }
    
    /// <summary>
    /// 重建KD树
    /// </summary>
    /// <param name="objects">要重建的对象列表</param>
    public void Rebuild(List<(Vector3 position, float radius, object data)> objects)
    {
        // 清空当前树
        Clear();

        // 如果没有对象，直接返回
        if (objects == null || objects.Count == 0)
            return;

        // 直接构建平衡树，不需要预先排序
        root = BuildBalancedTree(objects, 0, objects.Count - 1, 0);
    }

    /// <summary>
    /// 递归构建平衡的KD树
    /// </summary>
    private KDNode BuildBalancedTree(List<(Vector3 position, float radius, object data)> objects, int start, int end, int depth)
    {
        if (start > end)
            return null;

        // 选择当前维度的中位数作为分割点
        int mid = (start + end) / 2;
        int axis = depth % 3;

        // 根据当前维度对对象进行排序
        if (axis == 0)
            objects.Sort(start, end - start + 1, Comparer<(Vector3, float, object)>.Create((a, b) => a.Item1.x.CompareTo(b.Item1.x)));
        else if (axis == 1)
            objects.Sort(start, end - start + 1, Comparer<(Vector3, float, object)>.Create((a, b) => a.Item1.y.CompareTo(b.Item1.y)));
        else
            objects.Sort(start, end - start + 1, Comparer<(Vector3, float, object)>.Create((a, b) => a.Item1.z.CompareTo(b.Item1.z)));

        // 创建新节点
        var node = new KDNode(objects[mid].position, objects[mid].radius, objects[mid].data, depth);
        
        // 递归构建左右子树
        node.left = BuildBalancedTree(objects, start, mid - 1, depth + 1);
        node.right = BuildBalancedTree(objects, mid + 1, end, depth + 1);

        count++;
        return node;
    }
    
    /// <summary>
    /// 用于调试的可视化方法
    /// </summary>
    public void DebugDraw(){}
} 
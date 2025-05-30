using UnityEngine;
using System.Collections.Generic;
using SpatialSearchAlgorithm;

/// <summary>
/// 暴力遍历（Brute Force）实现的空间搜索算法
/// 通过线性遍历所有对象来查找范围内的对象
/// 作为基准实现，用于对比其他优化算法的性能
/// 
/// 优点：
/// 1. 实现简单直观
/// 2. 不需要额外的数据结构
/// 3. 适合小规模数据集
/// 
/// 缺点：
/// 1. 时间复杂度为O(n)，性能较差
/// 2. 不适合大规模数据集
/// </summary>
public class BruteForce : ISpatialSearch
{
    /// <summary>
    /// 存储所有对象的列表
    /// </summary>
    private List<(Vector3 position, float radius, object data)> objects;

    /// <summary>
    /// 构造函数
    /// </summary>
    public BruteForce()
    {
        objects = new List<(Vector3, float, object)>();
    }

    /// <summary>
    /// 插入一个对象
    /// </summary>
    /// <param name="position">对象位置</param>
    /// <param name="radius">对象半径</param>
    /// <param name="data">对象数据</param>
    public void Insert(Vector3 position, float radius, object data)
    {
        objects.Add((position, radius, data));
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
        
        // 遍历所有对象
        foreach (var obj in objects)
        {
            // 计算距离
            float distance = DistanceCalculator.CalculateDistance2D(
                new Vector2(position.x, position.y),
                new Vector2(obj.position.x, obj.position.y)
            );
            
            // 检查是否在范围内
            if (DistanceCalculator.IsInRange(distance, radius + obj.radius))
            {
                results.Add(obj.data);
            }
        }
        
        return results;
    }

    /// <summary>
    /// 移除指定对象
    /// </summary>
    /// <param name="data">要移除的对象数据</param>
    public void Remove(object data)
    {
        objects.RemoveAll(obj => obj.data == data);
    }

    /// <summary>
    /// 清空所有对象
    /// </summary>
    public void Clear()
    {
        objects.Clear();
    }

    /// <summary>
    /// 调试绘制（空实现，因为暴力遍历没有可视化结构）
    /// </summary>
    public void DebugDraw()
    {
        // 暴力遍历没有可视化结构，所以这里不需要实现
    }

    /// <summary>
    /// 重建数据结构（空实现，因为暴力遍历不需要重建）
    /// </summary>
    public void Rebuild(List<(Vector3 position, float radius, object data)> objects)
    {
        this.objects = new List<(Vector3, float, object)>(objects);
    }
} 
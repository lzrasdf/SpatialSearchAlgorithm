using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using SpatialSearchAlgorithm;

/// <summary>
/// LSH（局部敏感哈希）空间索引结构，实现高维空间的近似邻域快速查询。
/// 适用于大规模、动态变化的数据集。
/// </summary>
public class LSH : ISpatialSearch
{
    /// <summary>
    /// 内部哈希表类，每个哈希表负责一组随机投影和分桶。
    /// </summary>
    private class HashTable
    {
        /// <summary>
        /// 哈希桶，键为哈希值，值为对象列表（包含位置、半径和数据）。
        /// </summary>
        public Dictionary<int, List<(Vector3 position, float radius, object data)>> buckets;
        
        /// <summary>
        /// 随机投影向量，用于将高维空间映射到一维。
        /// </summary>
        public Vector3[] randomProjections;
        
        /// <summary>
        /// 桶的宽度，决定哈希分桶的粒度。
        /// </summary>
        public float bucketWidth;

        /// <summary>
        /// 构造函数，初始化哈希表。
        /// </summary>
        /// <param name="numProjections">随机投影向量的数量</param>
        /// <param name="bucketWidth">桶宽度</param>
        public HashTable(int numProjections, float bucketWidth)
        {
            this.buckets = new Dictionary<int, List<(Vector3, float, object)>>();
            this.randomProjections = new Vector3[numProjections];
            this.bucketWidth = bucketWidth;

            // 生成随机投影向量，每个向量都是单位向量
            System.Random random = new System.Random();
            for (int i = 0; i < numProjections; i++)
            {
                // 生成随机向量
                float x = (float)(random.NextDouble() * 2 - 1); // [-1, 1]
                float y = (float)(random.NextDouble() * 2 - 1); // [-1, 1]
                float z = (float)(random.NextDouble() * 2 - 1); // [-1, 1]
                
                // 归一化为单位向量
                Vector3 v = new Vector3(x, y, z).normalized;
                randomProjections[i] = v;
            }
        }

        /// <summary>
        /// 计算给定位置的哈希值。
        /// </summary>
        /// <param name="position">空间位置</param>
        /// <returns>哈希桶编号</returns>
        public int GetHash(Vector3 position)
        {
            float hashValue = 0;
            for (int i = 0; i < randomProjections.Length; i++)
            {
                // 使用点积计算点在随机方向上的投影
                hashValue += Vector3.Dot(position, randomProjections[i]);
            }
            return Mathf.FloorToInt(hashValue / bucketWidth);
        }
    }

    /// <summary>
    /// 多个哈希表组成的LSH结构。
    /// </summary>
    private List<HashTable> hashTables;
    private readonly int numHashTables;   // 哈希表数量
    private readonly int numProjections;  // 每个哈希表的投影数量
    private readonly float bucketWidth;   // 桶宽度
    private readonly Vector2 worldSize;   // 空间大小
    private readonly Vector2 worldCenter; // 空间中心

    /// <summary>
    /// 构造函数，初始化LSH结构。
    /// </summary>
    /// <param name="center">空间中心</param>
    /// <param name="size">空间大小</param>
    /// <param name="numHashTables">哈希表数量</param>
    /// <param name="numProjections">每个哈希表的投影数量</param>
    /// <param name="bucketWidth">桶宽度</param>
    public LSH(Vector2 center, Vector2 size, int numHashTables = 4, int numProjections = 8, float bucketWidth = 50f)
    {
        this.worldCenter = center;
        this.worldSize = size;
        this.numHashTables = numHashTables;
        this.numProjections = numProjections;
        this.bucketWidth = bucketWidth;

        // 初始化多个哈希表，每个表有独立的随机投影
        hashTables = new List<HashTable>();
        for (int i = 0; i < numHashTables; i++)
        {
            hashTables.Add(new HashTable(numProjections, bucketWidth));
        }
    }

    /// <summary>
    /// 插入一个对象到LSH结构。
    /// </summary>
    /// <param name="position">对象位置</param>
    /// <param name="radius">对象半径</param>
    /// <param name="data">对象数据</param>
    public void Insert(Vector3 position, float radius, object data)
    {
        foreach (var hashTable in hashTables)
        {
            int hash = hashTable.GetHash(position);
            if (!hashTable.buckets.ContainsKey(hash))
            {
                hashTable.buckets[hash] = new List<(Vector3, float, object)>();
            }
            hashTable.buckets[hash].Add((position, radius, data));
        }
    }

    /// <summary>
    /// 查询给定位置和半径范围内的所有对象。
    /// </summary>
    /// <param name="position">查询中心点</param>
    /// <param name="radius">查询半径</param>
    /// <returns>范围内的所有数据对象列表</returns>
    public List<object> Query(Vector3 position, float radius)
    {
        HashSet<object> results = new HashSet<object>(); // 用于去重

        foreach (var hashTable in hashTables)
        {
            int centerHash = hashTable.GetHash(position);
            
            // 计算需要检查的桶范围
            int bucketRange = Mathf.CeilToInt(radius / hashTable.bucketWidth);
            
            // 检查范围内的所有桶
            for (int offset = -bucketRange; offset <= bucketRange; offset++)
            {
                int currentHash = centerHash + offset;
                if (hashTable.buckets.TryGetValue(currentHash, out var bucket))
                {
                    foreach (var obj in bucket)
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
            }
        }

        return results.ToList();
    }

    /// <summary>
    /// 从LSH结构中移除指定数据的对象。
    /// </summary>
    /// <param name="data">要移除的数据</param>
    public void Remove(object data)
    {
        foreach (var hashTable in hashTables)
        {
            foreach (var bucket in hashTable.buckets.Values)
            {
                bucket.RemoveAll(x => x.data == data);
            }
        }
    }

    /// <summary>
    /// 清空LSH结构，移除所有对象。
    /// </summary>
    public void Clear()
    {
        foreach (var hashTable in hashTables)
        {
            hashTable.buckets.Clear();
        }
    }

    /// <summary>
    /// 重建LSH结构（批量插入）。
    /// </summary>
    /// <param name="objects">要重建的对象列表</param>
    public void Rebuild(List<(Vector3 position, float radius, object data)> objects)
    {
        // 清空当前结构
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
    /// 在Unity场景中可视化LSH桶的分布。
    /// </summary>
    public void DebugDraw()
    {
    }
} 
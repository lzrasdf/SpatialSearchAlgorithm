using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

namespace SpatialSearchAlgorithm
{
    public class SpatialSearchPerformanceTest : MonoBehaviour
    {
        [Header("Test Settings")]
        public int[] testObjectCounts = new int[] { 1000, 5000 };
        public int queryIterations = 100;
        public float queryRadius = 50f;
        
        [Header("Environment Settings")]
        public float width = 1000f;
        public float height = 1000f;
        public float minRadius = 5f;
        public float maxRadius = 20f;
        
        private SpatialTestEnvironment environment;
        private SpatialSearchManager searchManager;
        private List<Circle> circles;
        private bool isRunning = false;
        
        void Start()
        {
            
        }

        void OnDestroy()
        {
            if (searchManager != null)
            {
                Destroy(searchManager);
            }
        }
        
        public void RunPerformanceTest()
        {
            if (!isRunning)
            {
                isRunning = true;
                environment = new SpatialTestEnvironment(width, height, minRadius, maxRadius);
                searchManager ??= gameObject.AddComponent<SpatialSearchManager>();
                
                foreach (int objectCount in testObjectCounts)
                {
                    // 测试暴力遍历（作为基准）
                    TestStructure(SpatialSearchType.BruteForce, objectCount);
                    
                    // 测试四叉树
                    TestStructure(SpatialSearchType.Quadtree, objectCount);
                    
                    // 测试KD树
                    TestStructure(SpatialSearchType.KDTree, objectCount);

                    // 测试空间网格
                    TestStructure(SpatialSearchType.SpatialGrid, objectCount);
                    
                    // 测试R树
                    TestStructure(SpatialSearchType.RTree, objectCount);
                    
                    // 测试BVH
                    TestStructure(SpatialSearchType.BVH, objectCount);
                    
                    // 测试LSH
                    TestStructure(SpatialSearchType.LSH, objectCount);
                }
                
                // 测试完成后销毁自身
                isRunning = false;
            }
        }
        
        private void TestStructure(SpatialSearchType searchType, int objectCount)
        {
            // 初始化环境
            circles = environment.GenerateCircles(objectCount);
            
            // 初始化搜索结构
            searchManager.Initialize(searchType, new Vector2(width/2, height/2), new Vector2(width, height));
            
            // 执行测试并获取结果
            var results = ExecutePerformanceTest(searchType, objectCount);
            
            // 输出结果
            UnityEngine.Debug.Log($"{searchType} Performance Test Results:");
            UnityEngine.Debug.Log($"Object Count: {objectCount}");
            UnityEngine.Debug.Log($"Insert Time: {results.insertTimeMs:F2}ms");
            UnityEngine.Debug.Log($"Query Time: {results.queryTimeMs:F2}ms");
            UnityEngine.Debug.Log($"Average Query Time: {results.queryTimeMs / queryIterations:F2}ms");
            UnityEngine.Debug.Log("----------------------------------------");
            
            // 清理当前测试的数据
            searchManager.Clear();
        }

        private (double insertTimeMs, double queryTimeMs) ExecutePerformanceTest(SpatialSearchType searchType, int objectCount)
        {
            // 测量插入性能
            DateTime insertStartTime = DateTime.Now;
            foreach (var circle in circles)
            {
                searchManager.Insert(new Vector3(circle.X, circle.Y, 0), circle.Radius, circle);
            }
            DateTime insertEndTime = DateTime.Now;
            double insertTimeMs = (insertEndTime - insertStartTime).TotalMilliseconds;
            
            // 测量查询性能
            DateTime queryStartTime = DateTime.Now;
            for (int i = 0; i < queryIterations; i++)
            {
                Vector3 queryPoint = new Vector3(
                    UnityEngine.Random.Range(0, width),
                    UnityEngine.Random.Range(0, height),
                    0
                );
                searchManager.Query(queryPoint, queryRadius);
            }
            DateTime queryEndTime = DateTime.Now;
            double queryTimeMs = (queryEndTime - queryStartTime).TotalMilliseconds;
            
            return (insertTimeMs, queryTimeMs);
        }
    }
} 
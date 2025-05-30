using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SpatialSearchAlgorithm;
using UnityEngine.Profiling;

public class SpatialTestVisualizer : MonoBehaviour
{
    [Header("Environment Settings")]
    public float width = 1000f;
    public float height = 1000f;
    public float minRadius = 5f;
    public float maxRadius = 20f;
    public int numCircles = 100;
    [Range(0f, 500f)]
    public float circleSpeed = 100f;

    [Header("Spatial Search Settings")]
    [SerializeField] private SpatialSearchType searchType = SpatialSearchType.BruteForce;
    [SerializeField] private float queryRadius = 50f;
    [SerializeField] private Color queryColor = Color.red;
    [SerializeField] private bool realTimeQuery = true;

    [Header("Quadtree Settings")]
    [SerializeField] private int maxObjectsPerNode = 16;
    [SerializeField] private float minNodeSize = 1f;
    [SerializeField] private float mergeThreshold = 8f;
    [SerializeField] private int maxDepth = 4;

    [Header("Visualization Settings")]
    public Color circleColor = Color.blue;
    public Color boundaryColor = Color.red;

    private SpatialTestEnvironment environment;
    private List<Circle> circles;
    private Camera mainCamera;
    private float scale = 1f;
    private List<GameObject> sphereObjects = new List<GameObject>();
    private bool isPaused = false;

    private SpatialSearchManager searchManager;
    private List<Circle> queriedCircles = new List<Circle>();
    private Vector3 lastQueryPosition;
    private Dictionary<Circle, Vector3> lastPositions = new Dictionary<Circle, Vector3>();
    private bool isInitialized = false;
    private bool canMoveSpheres = true;  // 添加控制球体移动的变量
    
    // 性能监控
    private float lastUpdateTime;
    private float lastQueryTime;
    private float updateTimeSum;
    private float queryTimeSum;
    private int updateCount;
    private int queryCount;
    private const int SAMPLE_COUNT = 60; // 采样数量，用于计算平均值
    
    public GameObject performanceTestObj;
    private SpatialSearchPerformanceTest performanceTest;

    private bool createActualSpheres = true; // 控制是否创建实际球体

    void Start()
    {
        InitializeEnvironment();
        InitializeSearch();
        performanceTest = performanceTestObj.GetComponent<SpatialSearchPerformanceTest>();
    }

    void InitializeEnvironment()
    {
        // 初始化环境
        environment = new SpatialTestEnvironment(width, height, minRadius, maxRadius, circleSpeed);
        circles = environment.GenerateCircles(numCircles);

        // 获取主相机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
            return;
        }

        // 调整相机位置和大小以适应环境
        mainCamera.orthographic = true;
        mainCamera.transform.position = new Vector3(width / 2, height / 2, -10);
        mainCamera.orthographicSize = height / 2;

        // 创建球体
        CreateSpheres();
    }

    void InitializeSearch()
    {
        searchManager = GetComponent<SpatialSearchManager>();
        if (searchManager == null)
        {
            searchManager = gameObject.AddComponent<SpatialSearchManager>();
        }

        // 初始化搜索管理器
        searchManager.Initialize(searchType, new Vector2(width/2, height/2), new Vector2(width, height), 
            maxObjectsPerNode, minNodeSize, mergeThreshold, maxDepth);

        // 将所有Circle添加到空间搜索中
        foreach (var circle in circles)
        {
            Vector3 position = new Vector3(circle.X, circle.Y, 0);
            searchManager.Insert(position, circle.Radius, circle);
            lastPositions[circle] = position;
        }

        isInitialized = true;
    }

    void Update()
    {
        if (isPaused) return;

        if (!isInitialized) return;

        // 更新球体位置
        if (canMoveSpheres)  // 只有在允许移动时才更新位置
        {
        foreach (var circle in circles)
        {
            // 更新位置
            circle.UpdatePosition(Time.deltaTime);
            // 处理边界碰撞
            circle.HandleBoundaryCollision(width, height);

                // 更新球体位置
                int index = circles.IndexOf(circle);
                if (index >= 0 && index < sphereObjects.Count)
                {
                    sphereObjects[index].transform.position = new Vector3(circle.X, circle.Y, 0);
                }
            }
        }

        // 更新空间搜索
        float startTime = Time.realtimeSinceStartup;
        UpdateSpatialSearch();
        lastUpdateTime = (Time.realtimeSinceStartup - startTime) * 1000; // 转换为毫秒
        updateTimeSum += lastUpdateTime;
        updateCount++;
        if (updateCount > SAMPLE_COUNT)
        {
            updateTimeSum -= updateTimeSum / updateCount;
            updateCount--;
        }

        // 处理查询
        if (realTimeQuery)
        {
            startTime = Time.realtimeSinceStartup;
            UpdateQuery();
            lastQueryTime = (Time.realtimeSinceStartup - startTime) * 1000; // 转换为毫秒
            queryTimeSum += lastQueryTime;
            queryCount++;
            if (queryCount > SAMPLE_COUNT)
            {
                queryTimeSum -= queryTimeSum / queryCount;
                queryCount--;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            startTime = Time.realtimeSinceStartup;
            PerformQueryTest();
            lastQueryTime = (Time.realtimeSinceStartup - startTime) * 1000; // 转换为毫秒
            queryTimeSum += lastQueryTime;
            queryCount++;
            if (queryCount > SAMPLE_COUNT)
            {
                queryTimeSum -= queryTimeSum / queryCount;
                queryCount--;
            }
        }

        if (searchManager.CurrentSearch is Quadtree quadtree)
        {
            quadtree.DebugDraw();
        }
        if (searchManager.CurrentSearch is SpatialGrid grid)
        {
            grid.DebugDraw();
        }
        if (searchManager.CurrentSearch is RTree rtree)
        {
            rtree.DebugDraw();
        }
        if (searchManager.CurrentSearch is BVH bvh)
        {
            bvh.DebugDraw();
        }
        if (searchManager.CurrentSearch is LSH lsh)
        {
            lsh.DebugDraw();
        }
    }

    void UpdateSpatialSearch()
    {
        foreach (var circle in circles)
        {
            Vector3 currentPosition = new Vector3(circle.X, circle.Y, 0);
            if (lastPositions.TryGetValue(circle, out Vector3 lastPosition))
            {
                if (currentPosition != lastPosition)
                {
                    // 移除旧位置的对象
                    searchManager.Remove(circle);
                    // 插入新位置
                    searchManager.Insert(currentPosition, circle.Radius, circle);
                    // 更新位置记录
                    lastPositions[circle] = currentPosition;
                }
            }
            else
            {
                // 如果是新添加的物体
                searchManager.Insert(currentPosition, circle.Radius, circle);
                lastPositions[circle] = currentPosition;
            }
        }
    }

    void UpdateQuery()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;

        // 移除位置变化检查，每次都更新查询结果
        ResetQueriedObjects();
        PerformQueryAtPosition(worldPos);
        lastQueryPosition = worldPos;
    }

    void PerformQueryTest()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;
        PerformQueryAtPosition(worldPos);
    }

    void PerformQueryAtPosition(Vector3 position)
    {
        // 执行查询
        object[] results = searchManager.Query(position, queryRadius);

        // 高亮显示查询结果
        foreach (object obj in results)
        {
            Circle circle = obj as Circle;
            if (circle != null)
            {
                // 找到对应的球体对象
                int index = circles.IndexOf(circle);
                if (index >= 0 && index < sphereObjects.Count)
                {
                    sphereObjects[index].GetComponent<Renderer>().material.color = queryColor;
                    queriedCircles.Add(circle);
                }
            }
        }
    }

    void ResetQueriedObjects()
    {
        foreach (var circle in queriedCircles)
        {
            int index = circles.IndexOf(circle);
            if (index >= 0 && index < sphereObjects.Count)
            {
                sphereObjects[index].GetComponent<Renderer>().material.color = circleColor;
            }
        }
        queriedCircles.Clear();
    }

    void CreateSpheres()
    {
        // 清除现有的球体
        foreach (var sphere in sphereObjects)
        {
            if (sphere != null)
            {
                Destroy(sphere);
            }
        }
        sphereObjects.Clear();

        // 创建新的球体
        foreach (var circle in circles)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = new Vector3(circle.X, circle.Y, 0);
            sphere.transform.localScale = new Vector3(circle.Radius * 2, circle.Radius * 2, circle.Radius * 2);
            sphere.GetComponent<Renderer>().material.color = circleColor;

            sphereObjects.Add(sphere);
        }
    }

    void OnGUI()
    {
        // 添加一些控制按钮
        if (GUI.Button(new Rect(10, 10, 100, 30), "Regenerate"))
        {
            // 重新初始化环境以使用新的参数
            InitializeEnvironment();
            InitializeSearch();
        }

        // 显示性能指标
        GUI.Label(new Rect(10, 90, 300, 100),
            $"索引维护时间: {lastUpdateTime:F4}ms\n" +
            $"查询时间: {lastQueryTime:F4}ms\n");

        if (GUI.Button(new Rect(120, 10, 100, 30), "Clear"))
        {
            environment.Clear();
            circles.Clear();
            foreach (var sphere in sphereObjects)
            {
                if (sphere != null)
                {
                    Destroy(sphere);
                }
            }
            sphereObjects.Clear();
            searchManager.Clear();
            isInitialized = false;
        }

        // 添加暂停/继续按钮
        if (GUI.Button(new Rect(230, 10, 100, 30), isPaused ? "Continue" : "Pause"))
        {
            isPaused = !isPaused;
        }

        // 添加搜索类型切换按钮
        string searchTypeText = searchType switch
        {
            SpatialSearchType.Quadtree => "Quadtree",
            SpatialSearchType.KDTree => "KDTree",
            SpatialSearchType.SpatialGrid => "Grid",
            SpatialSearchType.RTree => "RTree",
            SpatialSearchType.BVH => "BVH",
            SpatialSearchType.LSH => "LSH",
            SpatialSearchType.BruteForce => "BruteForce",
            _ => "Unknown"
        };

        if (GUI.Button(new Rect(340, 10, 100, 30), $"Switch: {searchTypeText}"))
        {
            switch (searchType)
            {
                case SpatialSearchType.BruteForce:
                    searchType = SpatialSearchType.SpatialGrid;
                    break;
                case SpatialSearchType.SpatialGrid:
                    searchType = SpatialSearchType.Quadtree;
                    break;
                case SpatialSearchType.Quadtree:
                    searchType = SpatialSearchType.KDTree;
                    break;
                case SpatialSearchType.KDTree:
                    searchType = SpatialSearchType.LSH;
                    break;
                case SpatialSearchType.LSH:
                    searchType = SpatialSearchType.BVH;
                    break;
                case SpatialSearchType.BVH:
                    searchType = SpatialSearchType.RTree;
                    break;
                case SpatialSearchType.RTree:
                    searchType = SpatialSearchType.BruteForce;
                    break;
                
            }
            InitializeSearch();
        }

        // 添加实时查询切换按钮
        if (GUI.Button(new Rect(450, 10, 100, 30), realTimeQuery ? "Single Query" : "Real-time"))
        {
            realTimeQuery = !realTimeQuery;
            if (!realTimeQuery)
            {
                ResetQueriedObjects();
            }
        }

        // 添加性能测试按钮
        // if (GUI.Button(new Rect(560, 10, 100, 30), "Performance Test"))
        // {
        //     // 创建新的游戏对象来运行性能测试
        //     performanceTest.RunPerformanceTest();
        // }

        // 添加重建树按钮
        if (GUI.Button(new Rect(10, 50, 100, 30), "Rebuild"))
        {
            // 保存当前所有圆的位置信息
            List<(Vector3 position, float radius, object data)> objects = new List<(Vector3, float, object)>();
            foreach (var circle in circles)
            {
                objects.Add((new Vector3(circle.X, circle.Y, 0), circle.Radius, circle));
            }

            // 调用重建方法
            searchManager.CurrentSearch.Rebuild(objects);
        }

        // 添加控制球体移动的按钮
        if (GUI.Button(new Rect(120, 50, 100, 30), canMoveSpheres ? "Stop Moving" : "Start Moving"))
        {
            canMoveSpheres = !canMoveSpheres;
        }

        // 添加距离计算方式切换按钮
        // if (GUI.Button(new Rect(670, 90, 100, 30), DistanceCalculator.UseSqrt ? "Use Sqrt" : "No Sqrt"))
        // {
        //     DistanceCalculator.UseSqrt = !DistanceCalculator.UseSqrt;
        // }

        // 添加是否创建实际球体的开关按钮
        // if (GUI.Button(new Rect(670, 120, 100, 30), createActualSpheres ? "Hide Spheres" : "Show Spheres"))
        // {
        //     createActualSpheres = !createActualSpheres;
        //     UpdateSphereVisibility();
        // }
    }

    // 更新球体可见性
    private void UpdateSphereVisibility()
    {
        foreach (var sphere in sphereObjects)
        {
            if (sphere != null)
            {
                sphere.SetActive(createActualSpheres);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (circles == null) return;

        // 绘制边界
        Gizmos.color = boundaryColor;
        // 左边界
        Gizmos.DrawLine(new Vector3(0, 0, 0), new Vector3(0, height, 0));
        // 右边界
        Gizmos.DrawLine(new Vector3(width, 0, 0), new Vector3(width, height, 0));
        // 上边界
        Gizmos.DrawLine(new Vector3(0, height, 0), new Vector3(width, height, 0));
        // 下边界
        Gizmos.DrawLine(new Vector3(0, 0, 0), new Vector3(width, 0, 0));

        // 绘制查询范围
        if (isInitialized)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastQueryPosition, queryRadius);
        }
    }

    void OnDestroy()
    {
        // 清理球体对象
        foreach (var sphere in sphereObjects)
        {
            if (sphere != null)
            {
                Destroy(sphere);
            }
        }
        sphereObjects.Clear();
    }
} 
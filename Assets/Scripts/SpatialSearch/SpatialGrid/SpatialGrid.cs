using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 空间网格（Spatial Grid）实现的空间搜索算法
/// 将2D空间划分为均匀的网格，每个网格单元存储其中的对象
/// 适用于需要快速查询某个区域内的所有对象的场景
/// 
/// 优点：
/// 1. 设计思路简单，容易理解和实现
/// 2. 时间复杂度从 O(n) 降低到 O(k)，其中 k 是查询区域内的对象数量
/// 缺点：
/// 1. 需要预先确定网格大小，网格太小会导致内存浪费，太大则查询效率降低
/// 2. 如果对象分布极度不均匀，时间复杂度甚至可能退化到 O(n)
/// 使用建议：
/// 适合分布均匀的场景。
/// </summary>
public class SpatialGrid : ISpatialSearch
{
    /// <summary>
    /// 网格单元类，用于存储网格中的对象信息
    /// </summary>
    private class GridCell
    {
        /// <summary>
        /// 存储在该网格单元中的对象列表
        /// 每个对象包含：位置、半径和关联数据
        /// </summary>
        public List<(Vector3 position, float radius, object data)> objects;
        
        /// <summary>
        /// 网格单元在网格中的位置（x,y坐标）
        /// </summary>
        public Vector2Int gridPosition;

        public GridCell(Vector2Int gridPosition)
        {
            this.gridPosition = gridPosition;
            this.objects = new List<(Vector3, float, object)>();
        }
    }

    /// <summary>
    /// 存储所有网格单元的字典，键为网格位置，值为网格单元
    /// </summary>
    private Dictionary<Vector2Int, GridCell> grid;
    
    /// <summary>
    /// 每个网格单元的大小
    /// </summary>
    private readonly float cellSize;
    
    /// <summary>
    /// 整个网格覆盖的世界空间大小
    /// </summary>
    private readonly Vector2 worldSize;
    
    /// <summary>
    /// 网格的中心点位置
    /// </summary>
    private readonly Vector2 worldCenter;
    
    /// <summary>
    /// 网格的宽度（以网格单元数计）
    /// </summary>
    private readonly int gridWidth;
    
    /// <summary>
    /// 网格的高度（以网格单元数计）
    /// </summary>
    private readonly int gridHeight;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="center">网格的中心点位置</param>
    /// <param name="size">网格覆盖的世界空间大小</param>
    /// <param name="cellSize">每个网格单元的大小</param>
    public SpatialGrid(Vector2 center, Vector2 size, float cellSize = 100f)
    {
        this.worldCenter = center;
        this.worldSize = size;
        this.cellSize = cellSize;
        this.gridWidth = Mathf.CeilToInt(size.x / cellSize);
        this.gridHeight = Mathf.CeilToInt(size.y / cellSize);
        this.grid = new Dictionary<Vector2Int, GridCell>();
    }

    /// <summary>
    /// 将世界坐标转换为网格坐标
    /// </summary>
    /// <param name="worldPosition">世界空间中的位置</param>
    /// <returns>对应的网格坐标</returns>
    private Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector2 relativePos = new Vector2(
            worldPosition.x - (worldCenter.x - worldSize.x / 2),
            worldPosition.y - (worldCenter.y - worldSize.y / 2)
        );
        
        int x = Mathf.FloorToInt(relativePos.x / cellSize);
        int y = Mathf.FloorToInt(relativePos.y / cellSize);
        
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 检查网格坐标是否有效（在网格范围内）
    /// </summary>
    /// <param name="gridPos">要检查的网格坐标</param>
    /// <returns>如果坐标在有效范围内返回true，否则返回false</returns>
    private bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }

    /// <summary>
    /// 获取指定网格位置的单元格，如果不存在则创建
    /// </summary>
    /// <param name="gridPos">网格坐标</param>
    /// <returns>对应的网格单元</returns>
    private GridCell GetOrCreateCell(Vector2Int gridPos)
    {
        if (!grid.TryGetValue(gridPos, out GridCell cell))
        {
            cell = new GridCell(gridPos);
            grid[gridPos] = cell;
        }
        return cell;
    }

    /// <summary>
    /// 向网格中插入一个对象
    /// </summary>
    /// <param name="position">对象的位置</param>
    /// <param name="radius">对象的半径</param>
    /// <param name="data">要存储的对象数据</param>
    public void Insert(Vector3 position, float radius, object data)
    {
        Vector2Int centerCell = WorldToGrid(position);
        int radiusInCells = Mathf.CeilToInt(radius / cellSize);

        // 遍历可能受影响的网格单元
        for (int x = -radiusInCells; x <= radiusInCells; x++)
        {
            for (int y = -radiusInCells; y <= radiusInCells; y++)
            {
                Vector2Int cellPos = new Vector2Int(centerCell.x + x, centerCell.y + y);
                if (IsValidGridPosition(cellPos))
                {
                    GetOrCreateCell(cellPos).objects.Add((position, radius, data));
                }
            }
        }
    }

    /// <summary>
    /// 查询指定位置和半径范围内的所有对象
    /// </summary>
    /// <param name="position">查询中心点</param>
    /// <param name="radius">查询半径</param>
    /// <returns>在查询范围内的所有对象数据列表</returns>
    public List<object> Query(Vector3 position, float radius)
    {
        HashSet<object> results = new HashSet<object>();
        Vector2Int centerCell = WorldToGrid(position);
        int radiusInCells = Mathf.CeilToInt(radius / cellSize);

        // 遍历可能包含目标对象的网格单元
        for (int x = -radiusInCells; x <= radiusInCells; x++)
        {
            for (int y = -radiusInCells; y <= radiusInCells; y++)
            {
                Vector2Int cellPos = new Vector2Int(centerCell.x + x, centerCell.y + y);
                if (IsValidGridPosition(cellPos) && grid.TryGetValue(cellPos, out GridCell cell))
                {
                    foreach (var obj in cell.objects)
                    {
                        if (Vector2.Distance(
                            new Vector2(position.x, position.y),
                            new Vector2(obj.position.x, obj.position.y)) <= radius + obj.radius)
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
    /// 从网格中移除指定对象
    /// </summary>
    /// <param name="data">要移除的对象数据</param>
    public void Remove(object data)
    {
        // 先收集需要移除的单元格
        var emptyCells = new List<Vector2Int>();
        
        // 遍历所有网格单元
        foreach (var cell in grid.Values)
        {
            cell.objects.RemoveAll(obj => obj.data == data);
            
            // 如果单元格为空，记录它
            if (cell.objects.Count == 0)
            {
                emptyCells.Add(cell.gridPosition);
            }
        }
        
        // 移除空单元格
        foreach (var pos in emptyCells)
        {
            grid.Remove(pos);
        }
    }

    /// <summary>
    /// 清空网格中的所有对象
    /// </summary>
    public void Clear()
    {
        grid.Clear();
    }

    /// <summary>
    /// 重建空间网格
    /// </summary>
    /// <param name="objects">要重建的对象列表</param>
    public void Rebuild(List<(Vector3 position, float radius, object data)> objects)
    {
        // 清空当前网格
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
    /// 在Unity编辑器中绘制网格和对象，用于调试
    /// </summary>
    public void DebugDraw()
    {
        // 绘制网格线
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = new Vector3(
                worldCenter.x - worldSize.x / 2 + x * cellSize,
                worldCenter.y - worldSize.y / 2,
                0
            );
            Vector3 end = new Vector3(
                worldCenter.x - worldSize.x / 2 + x * cellSize,
                worldCenter.y + worldSize.y / 2,
                0
            );
            Debug.DrawLine(start, end, Color.gray);
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = new Vector3(
                worldCenter.x - worldSize.x / 2,
                worldCenter.y - worldSize.y / 2 + y * cellSize,
                0
            );
            Vector3 end = new Vector3(
                worldCenter.x + worldSize.x / 2,
                worldCenter.y - worldSize.y / 2 + y * cellSize,
                0
            );
            Debug.DrawLine(start, end, Color.gray);
        }

        // 绘制有对象的单元格
        foreach (var cell in grid.Values)
        {
            if (cell.objects.Count > 0)
            {
                Vector3 cellCenter = new Vector3(
                    worldCenter.x - worldSize.x / 2 + (cell.gridPosition.x + 0.5f) * cellSize,
                    worldCenter.y - worldSize.y / 2 + (cell.gridPosition.y + 0.5f) * cellSize,
                    0
                );
                // 绘制单元格的四个边
                float halfSize = cellSize * 0.5f;
                Vector3 topLeft = cellCenter + new Vector3(-halfSize, halfSize, 0);
                Vector3 topRight = cellCenter + new Vector3(halfSize, halfSize, 0);
                Vector3 bottomLeft = cellCenter + new Vector3(-halfSize, -halfSize, 0);
                Vector3 bottomRight = cellCenter + new Vector3(halfSize, -halfSize, 0);

                Debug.DrawLine(topLeft, topRight, Color.yellow);
                Debug.DrawLine(topRight, bottomRight, Color.yellow);
                Debug.DrawLine(bottomRight, bottomLeft, Color.yellow);
                Debug.DrawLine(bottomLeft, topLeft, Color.yellow);
            }
        }
    }
} 
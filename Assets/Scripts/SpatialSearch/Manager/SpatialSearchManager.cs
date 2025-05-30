using UnityEngine;

public enum SpatialSearchType
{
    BruteForce,
    Quadtree,
    KDTree,
    SpatialGrid,
    RTree,
    BVH,
    LSH,
}

public class SpatialSearchManager : MonoBehaviour
{
    private ISpatialSearch currentSearch;
    public ISpatialSearch CurrentSearch => currentSearch;
    private SpatialSearchType currentType;

    public void Initialize(SpatialSearchType searchType, Vector2 center, Vector2 size, 
        int maxObjectsPerNode = 16, float minNodeSize = 1f, float mergeThreshold = 8f, int maxDepth = 4)
    {
        currentType = searchType;
        switch (searchType)
        {
            case SpatialSearchType.Quadtree:
                currentSearch = new Quadtree(center, size, maxObjectsPerNode, minNodeSize, mergeThreshold, maxDepth);
                break;
            case SpatialSearchType.KDTree:
                currentSearch = new KDTree(size);
                break;
            case SpatialSearchType.SpatialGrid:
                currentSearch = new SpatialGrid(center, size);
                break;
            case SpatialSearchType.RTree:
                currentSearch = new RTree(center, size);
                break;
            case SpatialSearchType.BVH:
                currentSearch = new BVH(center, size);
                break;
            case SpatialSearchType.LSH:
                currentSearch = new LSH(center, size);
                break;
            case SpatialSearchType.BruteForce:
                currentSearch = new BruteForce();
                break;
        }
    }

    public void Insert(Vector3 position, float radius, object data)
    {
        currentSearch?.Insert(position, radius, data);
    }

    public void Remove(object data)
    {
        currentSearch?.Remove(data);
    }

    public object[] Query(Vector3 position, float radius)
    {
        return currentSearch?.Query(position, radius).ToArray();
    }

    public void Clear()
    {
        currentSearch?.Clear();
    }

    public SpatialSearchType GetCurrentType()
    {
        return currentType;
    }
} 
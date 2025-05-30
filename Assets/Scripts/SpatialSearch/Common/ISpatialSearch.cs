using UnityEngine;
using System.Collections.Generic;

public interface ISpatialSearch
{
    void Insert(Vector3 position, float radius, object data);
    void Remove(object data);
    List<object> Query(Vector3 position, float radius);
    void Clear();
    void DebugDraw();
    void Rebuild(List<(Vector3 position, float radius, object data)> objects);
} 
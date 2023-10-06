using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deformer : MonoBehaviour
{
    /// <summary>
    /// Hedef mesh
    /// </summary>
    [SerializeField] private DeformableMesh _deformableMesh;

    /// <summary>
    /// Kaynak obje
    /// </summary>
    [SerializeField] private Transform _raycastSource;

    public bool deform;
    private void Update()
    {
        if (deform)
            DeformMesh();
    }
    private void DeformMesh()
    {
        Ray ray = new Ray(new Vector3(_raycastSource.position.x, _raycastSource.position.y, _raycastSource.position.z), Vector3.down);

        if (!Physics.Raycast(ray, out var hit))
            return;

        _deformableMesh.Deform(hit.point);
    }

}
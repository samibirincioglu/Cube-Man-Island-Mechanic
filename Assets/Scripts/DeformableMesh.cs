using UnityEngine;
public abstract class DeformableMesh : MonoBehaviour
{
    /// <summary>
    /// Bu alan� kullan�lan meshe g�re ayarla
    /// </summary>
    [SerializeField] protected float _radiusOfDeformation = 0.8f;
    /// <summary>
    /// G�� uygulanan vertexlerin ne kadar derine gidece�i
    /// </summary>
    [SerializeField] protected float _powerOfDeformation = 1f;
    public abstract void Deform(Vector3 positionToDeform);
}
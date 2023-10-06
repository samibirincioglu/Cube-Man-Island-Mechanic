using UnityEngine;
public abstract class DeformableMesh : MonoBehaviour
{
    /// <summary>
    /// Bu alaný kullanýlan meshe göre ayarla
    /// </summary>
    [SerializeField] protected float _radiusOfDeformation = 0.8f;
    /// <summary>
    /// Güç uygulanan vertexlerin ne kadar derine gideceði
    /// </summary>
    [SerializeField] protected float _powerOfDeformation = 1f;
    public abstract void Deform(Vector3 positionToDeform);
}
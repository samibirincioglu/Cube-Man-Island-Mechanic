using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using System;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class DeformableMeshObject : DeformableMesh
{
    private Mesh _mesh;
    private Vector3[] _vertices;

    /// <summary>
    /// Meshin tamam�n�n deforme edildi�ini anlamak, deforme olan say�ya g�re obje ��karmak, respawn triggeri olu�turmak vs. gibi durumlar i�in kullan�labilecek
    /// saya�
    /// </summary>
    public int deformedVertCount = 0;
    private void Awake()
    {
        var meshFilter = GetComponent<MeshFilter>();

        _mesh = meshFilter.mesh;
        _vertices = _mesh.vertices;
    }

    [BurstCompile]
    public struct DeformJob : IJobParallelFor
    {
        public NativeArray<Vector3> _verts;
        public Concurrent _deformedVertCount;

        public Vector3 _positionToDeform;
        public float _radiusOfDeformation;
        public float _powerOfDeformation;
        public void Execute(int index)
        {
            //DeformableMeshte belirledi�imiz menzildeyse deforme et
            var dist = (_verts[index] - _positionToDeform).sqrMagnitude;
            if (dist < _radiusOfDeformation)
            {
                _verts[index] -= Vector3.up * _powerOfDeformation;

                //saya�� artt�r
                _deformedVertCount.Increment();
            }
        }
    }

    public override void Deform(Vector3 positionToDeform)
    {
        //pozisyonu lokale �evir
        Vector3 _positionToDeform = transform.InverseTransformPoint(positionToDeform);

        //job lara sadece nativearray �eklinde liste g�nderilebiliyor
        //vertex datas�n� native arraye d�n��t�r�p o �ekillde yolla
        NativeArray<Vector3> _verts = new NativeArray<Vector3>(_vertices, Allocator.TempJob);
        var counter = new NativeCounter(Allocator.Persistent);

        //init job
        DeformJob job = new DeformJob
        {
            _verts = _verts,
            _positionToDeform = _positionToDeform,
            _radiusOfDeformation = _radiusOfDeformation,
            _powerOfDeformation = _powerOfDeformation,
            _deformedVertCount = counter
        };

        JobHandle jobHandle = job.Schedule(_vertices.Length, 500);
        jobHandle.Complete();

        //deforme edilmi� vertexleri orjinal meshe ata
        if (jobHandle.IsCompleted)
        {
            _vertices = job._verts.ToArray();

            if (counter.Count > 0)
                _mesh.vertices = _vertices;

            deformedVertCount += counter.Count;
        }

        //native arrayler i�lem bitti�inde dispose edilmeli
        _verts.Dispose();
        counter.Dispose();
    }
}

//Unity'nin native counterlari ve conteinerlari  icin linkten detayli bilgi edinebilirsiniz
//https://docs.unity3d.com/2020.1/Documentation/Manual/JobSystemNativeContainer.html
//
// Mark this struct as a NativeContainer, usually this would be a generic struct for containers, but a counter does not need to be generic
// TODO - why does a counter not need to be generic? - explain the argument for this reasoning please.
[StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    unsafe public struct NativeCounter
    {
        // The actual pointer to the allocated count needs to have restrictions relaxed so jobs can be schedled with this container
        [NativeDisableUnsafePtrRestriction]
        public int* m_Counter;

    #if ENABLE_UNITY_COLLECTIONS_CHECKS
        public AtomicSafetyHandle m_Safety;
        // The dispose sentinel tracks memory leaks. It is a managed type so it is cleared to null when scheduling a job
        // The job cannot dispose the container, and no one else can dispose it until the job has run, so it is ok to not pass it along
        // This attribute is required, without it this NativeContainer cannot be passed to a job; since that would give the job access to a managed object
        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
    #endif

        // Keep track of where the memory for this was allocated
        Allocator m_AllocatorLabel;

        public NativeCounter(Allocator label)
        {
            // This check is redundant since we always use an int that is blittable.
            // It is here as an example of how to check for type correctness for generic types.
    #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsBlittable<int>())
                throw new ArgumentException(string.Format("{0} used in NativeQueue<{0}> must be blittable", typeof(int)));
    #endif
            m_AllocatorLabel = label;

            // Allocate native memory for a single integer
            m_Counter = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(), 4, label);

            // Create a dispose sentinel to track memory leaks. This also creates the AtomicSafetyHandle
    #if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, Allocator.Persistent);
    #endif
            // Initialize the count to 0 to avoid uninitialized data
            Count = 0;
        }

        public void Increment()
        {
            // Verify that the caller has write permission on this data. 
            // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
    #if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
    #endif
            (*m_Counter)++;
        }

        public int Count
        {
            get
            {
                // Verify that the caller has read permission on this data. 
                // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
    #if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
    #endif
                return *m_Counter;
            }
            set
            {
                // Verify that the caller has write permission on this data. This is the race condition protection, without these checks the AtomicSafetyHandle is useless
    #if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
    #endif
                *m_Counter = value;
            }
        }

        public bool IsCreated
        {
            get { return m_Counter != null; }
        }

        public void Dispose()
        {
            // Let the dispose sentinel know that the data has been freed so it does not report any memory leaks
    #if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
    #endif

            UnsafeUtility.Free(m_Counter, m_AllocatorLabel);
            m_Counter = null;
        }
    }

// paralel job i�inde saya� tutmak i�in bu concurrentin implemente edilmesi gerekiyor


// This attribute is what makes it possible to use NativeCounter.Concurrent in a ParallelFor job
[NativeContainer]
[NativeContainerIsAtomicWriteOnly]
unsafe public struct Concurrent
{
    // Copy of the pointer from the full NativeCounter
    [NativeDisableUnsafePtrRestriction]
    int* m_Counter;

    // Copy of the AtomicSafetyHandle from the full NativeCounter. The dispose sentinel is not copied since this inner struct does not own the memory and is not responsible for freeing it.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    AtomicSafetyHandle m_Safety;
#endif

    // This is what makes it possible to assign to NativeCounter.Concurrent from NativeCounter
    public static implicit operator Concurrent(NativeCounter cnt)
    {
        Concurrent concurrent;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(cnt.m_Safety);
        concurrent.m_Safety = cnt.m_Safety;
        AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif

        concurrent.m_Counter = cnt.m_Counter;
        return concurrent;
    }

    public void Increment()
    {
        // Increment still needs to check for write permissions
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        // The actual increment is implemented with an atomic, since it can be incremented by multiple threads at the same time
        Interlocked.Increment(ref *m_Counter);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPS
{
    public enum ComputeType
    {
        None,
        Setup,
        SetupImmediately,
        DisableAll,
    }

    public enum UpdateMethodType
    {
        None,
        Target,
        Explode,
        Gravity,
    }

    public  interface IParticleTarget
    {
        Mesh Mesh { get; }
        int VertexCount { get; }
        Vector3[] Vertices { get; }
        Vector2[] UV { get; }
        Texture2D Texture { get; }
        Matrix4x4 WorldMatrix { get; }
        float MinScale { get; }
        float MaxScale { get; }
        uint[] SubGroupIndices { get; }

        void Initialize();
        void SetStartIndex(int startIdx);
    }

    public interface IParticleTargetGroup
    {
        Texture2DArray TextureArray { get; }
        Matrix4x4[] MatrixData { get; }
        InitData[] AllInitData { get; }
        uint[] Indices { get; }
        void Initialize(TransformParticleSystem system);
        void UpdateMatrices();
    }

    public struct TransformParticle
    {
        public int isActive;
        public int targetId;
        public Vector2 uv;

        public Vector3 targetPosition;

        public float speed;
        public Vector3 position;

        public int useTexture;
        public float scale;

        public Vector4 velocity;

        public Vector3 horizontal;
    }

    public struct InitData
    {
        public int isActive;
        public Vector3 targetPosition;

        public int targetId;
        public float scale;

        public Vector4 velocity;

        public Vector2 uv;
        public Vector3 horizontal;
    }
}


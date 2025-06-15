using UnityEngine;
using System.Collections.Generic;

namespace SampleProject.Editor.RoadGenerator
{
    [System.Serializable]
    public class RoadGenerationSettings
    {
        [Header("Grid Settings")]
        public int GridWidth = 3;
        public int GridHeight = 3;
        public float BlockSize = 50f;

        [Header("Road Settings")]
        public float RoadWidth = 8f;
        public float RoadHeight = 0.1f;
        public float IntersectionSize = 12f;

        [Header("Curb Settings")]
        public bool GenerateCurbs = true;
        public float CurbWidth = 0.5f;
        public float CurbHeight = 0.2f;

        [Header("Street Furniture")]
        public bool GeneratePoles = true;
        public float PoleHeight = 4f;
        public float PoleSpacing = 15f;
        public bool GenerateTrafficLights = true;

        [Header("Materials")]
        public Material RoadMaterial;
        public Material CurbMaterial;
        public Material PoleMaterial;

        [Header("Output Settings")]
        public string OutputPath = "Assets/Generated/Roads/";
        public string MeshName = "GeneratedRoad";
    }

    public class RoadNetwork
    {
        public List<RoadSegment> Roads { get; set; } = new List<RoadSegment>();
        public List<RoadIntersection> Intersections { get; set; } = new List<RoadIntersection>();
        public List<CurbSegment> Curbs { get; set; } = new List<CurbSegment>();
        public List<PoleObject> Poles { get; set; } = new List<PoleObject>();
        public List<TrafficLightObject> TrafficLights { get; set; } = new List<TrafficLightObject>();
    }

    public class RoadSegment
    {
        public Mesh Mesh { get; set; }
        public Vector3 StartPosition { get; set; }
        public Vector3 EndPosition { get; set; }
        public float Width { get; set; }
        public RoadDirection Direction { get; set; }
        public Bounds Bounds { get; set; }
    }

    public class RoadIntersection
    {
        public Mesh Mesh { get; set; }
        public Vector3 Position { get; set; }
        public float Size { get; set; }
        public Bounds Bounds { get; set; }
        public List<RoadConnection> Connections { get; set; } = new List<RoadConnection>();
    }

    public class CurbSegment
    {
        public Mesh Mesh { get; set; }
        public Vector3 StartPosition { get; set; }
        public Vector3 EndPosition { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public CurbSide Side { get; set; }
        public Bounds Bounds { get; set; }
    }

    public class PoleObject
    {
        public Mesh Mesh { get; set; }
        public Vector3 Position { get; set; }
        public float Height { get; set; }
        public PoleType Type { get; set; }
    }

    public class TrafficLightObject
    {
        public Mesh Mesh { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public TrafficLightType Type { get; set; }
    }

    public class RoadConnection
    {
        public RoadDirection Direction { get; set; }
        public Vector3 ConnectionPoint { get; set; }
        public float Width { get; set; }
    }

    public enum RoadDirection
    {
        North,
        South,
        East,
        West
    }

    public enum CurbSide
    {
        Left,
        Right,
        Front,
        Back
    }

    public enum PoleType
    {
        Standard,
        Corner,
        Intersection
    }

    public enum TrafficLightType
    {
        FourWay,
        TwoWay,
        Pedestrian
    }

    public struct GridCoordinate
    {
        public int X;
        public int Y;

        public GridCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector3 ToWorldPosition(float blockSize)
        {
            return new Vector3(X * blockSize, 0, Y * blockSize);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }

    public struct RoadPoint
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;

        public RoadPoint(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Position = position;
            Normal = normal;
            UV = uv;
        }
    }
} 
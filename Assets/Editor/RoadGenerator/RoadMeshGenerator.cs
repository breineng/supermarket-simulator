using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SampleProject.Editor.RoadGenerator
{
    public class RoadMeshGenerator
    {
        private const float UV_TILING_SCALE = 4f; // For road texture tiling

        public RoadNetwork GenerateRoadNetwork(RoadGenerationSettings settings)
        {
            var network = new RoadNetwork();

            // Generate grid layout
            var gridLayout = GenerateGridLayout(settings);

            // Generate road segments (horizontal and vertical roads)
            GenerateRoadSegments(network, gridLayout, settings);

            // Generate intersections
            GenerateIntersections(network, gridLayout, settings);

            // Generate curbs if enabled
            if (settings.GenerateCurbs)
            {
                GenerateCurbs(network, gridLayout, settings);
            }

            // Generate poles if enabled
            if (settings.GeneratePoles)
            {
                GeneratePoles(network, gridLayout, settings);
            }

            // Generate traffic lights if enabled
            if (settings.GenerateTrafficLights)
            {
                GenerateTrafficLights(network, gridLayout, settings);
            }

            return network;
        }

        private GridLayout GenerateGridLayout(RoadGenerationSettings settings)
        {
            var layout = new GridLayout();
            layout.Width = settings.GridWidth;
            layout.Height = settings.GridHeight;
            layout.BlockSize = settings.BlockSize;
            layout.RoadWidth = settings.RoadWidth;
            layout.IntersectionSize = settings.IntersectionSize;

            return layout;
        }

        private void GenerateRoadSegments(RoadNetwork network, GridLayout layout, RoadGenerationSettings settings)
        {
            // Generate horizontal road segments
            for (int y = 0; y <= layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    var road = CreateHorizontalRoadSegment(x, y, layout, settings);
                    network.Roads.Add(road);
                }
            }

            // Generate vertical road segments
            for (int x = 0; x <= layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    var road = CreateVerticalRoadSegment(x, y, layout, settings);
                    network.Roads.Add(road);
                }
            }
        }

        private RoadSegment CreateHorizontalRoadSegment(int x, int y, GridLayout layout, RoadGenerationSettings settings)
        {
            var segment = new RoadSegment();
            segment.Direction = RoadDirection.East;
            segment.Width = settings.RoadWidth;

            // Calculate positions
            float worldY = y * layout.BlockSize;
            float startX = x * layout.BlockSize + layout.IntersectionSize / 2;
            float endX = (x + 1) * layout.BlockSize - layout.IntersectionSize / 2;

            segment.StartPosition = new Vector3(startX, 0, worldY);
            segment.EndPosition = new Vector3(endX, 0, worldY);

            // Generate mesh
            segment.Mesh = CreateRoadSegmentMesh(segment.StartPosition, segment.EndPosition, settings.RoadWidth, settings.RoadHeight, RoadDirection.East);
            segment.Bounds = segment.Mesh.bounds;

            return segment;
        }

        private RoadSegment CreateVerticalRoadSegment(int x, int y, GridLayout layout, RoadGenerationSettings settings)
        {
            var segment = new RoadSegment();
            segment.Direction = RoadDirection.North;
            segment.Width = settings.RoadWidth;

            // Calculate positions
            float worldX = x * layout.BlockSize;
            float startZ = y * layout.BlockSize + layout.IntersectionSize / 2;
            float endZ = (y + 1) * layout.BlockSize - layout.IntersectionSize / 2;

            segment.StartPosition = new Vector3(worldX, 0, startZ);
            segment.EndPosition = new Vector3(worldX, 0, endZ);

            // Generate mesh
            segment.Mesh = CreateRoadSegmentMesh(segment.StartPosition, segment.EndPosition, settings.RoadWidth, settings.RoadHeight, RoadDirection.North);
            segment.Bounds = segment.Mesh.bounds;

            return segment;
        }

        private Mesh CreateRoadSegmentMesh(Vector3 start, Vector3 end, float width, float height, RoadDirection direction)
        {
            var mesh = new Mesh();
            mesh.name = $"RoadSegment_{direction}";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            Vector3 forward = (end - start).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float length = Vector3.Distance(start, end);

            // Create road surface vertices
            float halfWidth = width / 2f;

            // Bottom vertices (road surface)
            vertices.Add(start - right * halfWidth); // 0
            vertices.Add(start + right * halfWidth); // 1
            vertices.Add(end - right * halfWidth);   // 2
            vertices.Add(end + right * halfWidth);   // 3

            // Top vertices (if height > 0)
            if (height > 0)
            {
                Vector3 heightOffset = Vector3.up * height;
                vertices.Add(start - right * halfWidth + heightOffset); // 4
                vertices.Add(start + right * halfWidth + heightOffset); // 5
                vertices.Add(end - right * halfWidth + heightOffset);   // 6
                vertices.Add(end + right * halfWidth + heightOffset);   // 7
            }

            // UV coordinates for tiling
            float uvLength = length / UV_TILING_SCALE;
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, uvLength));
            uvs.Add(new Vector2(1, uvLength));

            if (height > 0)
            {
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, uvLength));
                uvs.Add(new Vector2(1, uvLength));
            }

            // Normals
            Vector3 upNormal = Vector3.up;
            for (int i = 0; i < vertices.Count; i++)
            {
                normals.Add(upNormal);
            }

            // Triangles for road surface
            if (height > 0)
            {
                // Top surface
                triangles.AddRange(new int[] { 4, 6, 5, 5, 6, 7 });
            }
            else
            {
                // Bottom surface
                triangles.AddRange(new int[] { 0, 2, 1, 1, 2, 3 });
            }

            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateBounds();
            return mesh;
        }

        private void GenerateIntersections(RoadNetwork network, GridLayout layout, RoadGenerationSettings settings)
        {
            for (int x = 0; x <= layout.Width; x++)
            {
                for (int y = 0; y <= layout.Height; y++)
                {
                    var intersection = CreateIntersection(x, y, layout, settings);
                    network.Intersections.Add(intersection);
                }
            }
        }

        private RoadIntersection CreateIntersection(int x, int y, GridLayout layout, RoadGenerationSettings settings)
        {
            var intersection = new RoadIntersection();
            intersection.Size = settings.IntersectionSize;
            intersection.Position = new Vector3(x * layout.BlockSize, 0, y * layout.BlockSize);

            // Generate mesh
            intersection.Mesh = CreateIntersectionMesh(intersection.Position, settings.IntersectionSize, settings.RoadHeight);
            intersection.Bounds = intersection.Mesh.bounds;

            // Add connections based on grid position
            if (x > 0) intersection.Connections.Add(new RoadConnection { Direction = RoadDirection.West, ConnectionPoint = intersection.Position + Vector3.left * settings.IntersectionSize / 2, Width = settings.RoadWidth });
            if (x < layout.Width) intersection.Connections.Add(new RoadConnection { Direction = RoadDirection.East, ConnectionPoint = intersection.Position + Vector3.right * settings.IntersectionSize / 2, Width = settings.RoadWidth });
            if (y > 0) intersection.Connections.Add(new RoadConnection { Direction = RoadDirection.South, ConnectionPoint = intersection.Position + Vector3.back * settings.IntersectionSize / 2, Width = settings.RoadWidth });
            if (y < layout.Height) intersection.Connections.Add(new RoadConnection { Direction = RoadDirection.North, ConnectionPoint = intersection.Position + Vector3.forward * settings.IntersectionSize / 2, Width = settings.RoadWidth });

            return intersection;
        }

        private Mesh CreateIntersectionMesh(Vector3 center, float size, float height)
        {
            var mesh = new Mesh();
            mesh.name = "RoadIntersection";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            float halfSize = size / 2f;

            // Create intersection vertices
            vertices.Add(center + new Vector3(-halfSize, 0, -halfSize)); // 0
            vertices.Add(center + new Vector3(halfSize, 0, -halfSize));  // 1
            vertices.Add(center + new Vector3(-halfSize, 0, halfSize));  // 2
            vertices.Add(center + new Vector3(halfSize, 0, halfSize));   // 3

            if (height > 0)
            {
                Vector3 heightOffset = Vector3.up * height;
                vertices.Add(center + new Vector3(-halfSize, 0, -halfSize) + heightOffset); // 4
                vertices.Add(center + new Vector3(halfSize, 0, -halfSize) + heightOffset);  // 5
                vertices.Add(center + new Vector3(-halfSize, 0, halfSize) + heightOffset);  // 6
                vertices.Add(center + new Vector3(halfSize, 0, halfSize) + heightOffset);   // 7
            }

            // UV coordinates
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));

            if (height > 0)
            {
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
            }

            // Normals
            for (int i = 0; i < vertices.Count; i++)
            {
                normals.Add(Vector3.up);
            }

            // Triangles
            if (height > 0)
            {
                // Top surface
                triangles.AddRange(new int[] { 4, 6, 5, 5, 6, 7 });
            }
            else
            {
                // Bottom surface
                triangles.AddRange(new int[] { 0, 2, 1, 1, 2, 3 });
            }

            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateBounds();
            return mesh;
        }

        private void GenerateCurbs(RoadNetwork network, GridLayout layout, RoadGenerationSettings settings)
        {
            // Generate curbs along all road segments
            foreach (var road in network.Roads)
            {
                // Create curbs on both sides of the road
                var leftCurb = CreateCurbSegment(road, CurbSide.Left, settings);
                var rightCurb = CreateCurbSegment(road, CurbSide.Right, settings);

                network.Curbs.Add(leftCurb);
                network.Curbs.Add(rightCurb);
            }
        }

        private CurbSegment CreateCurbSegment(RoadSegment road, CurbSide side, RoadGenerationSettings settings)
        {
            var curb = new CurbSegment();
            curb.Side = side;
            curb.Width = settings.CurbWidth;
            curb.Height = settings.CurbHeight;

            Vector3 forward = (road.EndPosition - road.StartPosition).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            float offset = (road.Width / 2f) + (settings.CurbWidth / 2f);
            Vector3 sideOffset = (side == CurbSide.Left) ? -right * offset : right * offset;

            curb.StartPosition = road.StartPosition + sideOffset;
            curb.EndPosition = road.EndPosition + sideOffset;

            // Generate curb mesh
            curb.Mesh = CreateCurbMesh(curb.StartPosition, curb.EndPosition, settings.CurbWidth, settings.CurbHeight, forward);
            curb.Bounds = curb.Mesh.bounds;

            return curb;
        }

        private Mesh CreateCurbMesh(Vector3 start, Vector3 end, float width, float height, Vector3 forward)
        {
            var mesh = new Mesh();
            mesh.name = "CurbSegment";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float halfWidth = width / 2f;
            float length = Vector3.Distance(start, end);

            // Create curb vertices (box shape)
            // Bottom face
            vertices.Add(start - right * halfWidth); // 0
            vertices.Add(start + right * halfWidth); // 1
            vertices.Add(end - right * halfWidth);   // 2
            vertices.Add(end + right * halfWidth);   // 3

            // Top face
            Vector3 heightOffset = Vector3.up * height;
            vertices.Add(start - right * halfWidth + heightOffset); // 4
            vertices.Add(start + right * halfWidth + heightOffset); // 5
            vertices.Add(end - right * halfWidth + heightOffset);   // 6
            vertices.Add(end + right * halfWidth + heightOffset);   // 7

            // UV mapping
            float uvLength = length / UV_TILING_SCALE;
            // Bottom face UVs
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, uvLength));
            uvs.Add(new Vector2(1, uvLength));
            // Top face UVs
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, uvLength));
            uvs.Add(new Vector2(1, uvLength));

            // Normals (simplified)
            for (int i = 0; i < 8; i++)
            {
                normals.Add(Vector3.up);
            }

            // Top face triangles
            triangles.AddRange(new int[] { 4, 6, 5, 5, 6, 7 });

            // Side faces
            triangles.AddRange(new int[] { 0, 4, 1, 1, 4, 5 }); // Front face
            triangles.AddRange(new int[] { 2, 3, 6, 3, 7, 6 }); // Back face
            triangles.AddRange(new int[] { 0, 2, 4, 4, 2, 6 }); // Left face
            triangles.AddRange(new int[] { 1, 5, 3, 3, 5, 7 }); // Right face

            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void GeneratePoles(RoadNetwork network, GridLayout layout, RoadGenerationSettings settings)
        {
            // Generate poles along roads
            foreach (var road in network.Roads)
            {
                var poles = CreatePolesAlongRoad(road, settings);
                network.Poles.AddRange(poles);
            }

            // Generate corner poles at intersections
            foreach (var intersection in network.Intersections)
            {
                var cornerPoles = CreateCornerPoles(intersection, settings);
                network.Poles.AddRange(cornerPoles);
            }
        }

        private List<PoleObject> CreatePolesAlongRoad(RoadSegment road, RoadGenerationSettings settings)
        {
            var poles = new List<PoleObject>();

            Vector3 forward = (road.EndPosition - road.StartPosition).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float roadLength = Vector3.Distance(road.StartPosition, road.EndPosition);

            float poleOffset = (road.Width / 2f) + settings.CurbWidth + 0.5f;

            int poleCount = Mathf.FloorToInt(roadLength / settings.PoleSpacing);
            for (int i = 0; i <= poleCount; i++)
            {
                float t = (float)i / poleCount;
                Vector3 roadPoint = Vector3.Lerp(road.StartPosition, road.EndPosition, t);

                // Left side pole
                var leftPole = new PoleObject
                {
                    Position = roadPoint - right * poleOffset,
                    Height = settings.PoleHeight,
                    Type = PoleType.Standard,
                    Mesh = CreatePoleMesh(settings.PoleHeight)
                };
                poles.Add(leftPole);

                // Right side pole
                var rightPole = new PoleObject
                {
                    Position = roadPoint + right * poleOffset,
                    Height = settings.PoleHeight,
                    Type = PoleType.Standard,
                    Mesh = CreatePoleMesh(settings.PoleHeight)
                };
                poles.Add(rightPole);
            }

            return poles;
        }

        private List<PoleObject> CreateCornerPoles(RoadIntersection intersection, RoadGenerationSettings settings)
        {
            var poles = new List<PoleObject>();

            float poleOffset = (settings.IntersectionSize / 2f) + settings.CurbWidth + 0.5f;

            // Four corner poles
            Vector3[] corners = {
                intersection.Position + new Vector3(-poleOffset, 0, -poleOffset),
                intersection.Position + new Vector3(poleOffset, 0, -poleOffset),
                intersection.Position + new Vector3(-poleOffset, 0, poleOffset),
                intersection.Position + new Vector3(poleOffset, 0, poleOffset)
            };

            foreach (var corner in corners)
            {
                var pole = new PoleObject
                {
                    Position = corner,
                    Height = settings.PoleHeight,
                    Type = PoleType.Corner,
                    Mesh = CreatePoleMesh(settings.PoleHeight)
                };
                poles.Add(pole);
            }

            return poles;
        }

        private Mesh CreatePoleMesh(float height)
        {
            var mesh = new Mesh();
            mesh.name = "Pole";

            int segments = 8;
            float radius = 0.1f;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            // Create cylinder vertices
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                // Bottom circle
                vertices.Add(new Vector3(x, 0, z));
                normals.Add(new Vector3(x, 0, z).normalized);
                uvs.Add(new Vector2((float)i / segments, 0));

                // Top circle
                vertices.Add(new Vector3(x, height, z));
                normals.Add(new Vector3(x, 0, z).normalized);
                uvs.Add(new Vector2((float)i / segments, 1));
            }

            // Create triangles for cylinder sides
            for (int i = 0; i < segments; i++)
            {
                int bottom1 = i * 2;
                int top1 = i * 2 + 1;
                int bottom2 = (i + 1) * 2;
                int top2 = (i + 1) * 2 + 1;

                // Two triangles per segment
                triangles.AddRange(new int[] { bottom1, top1, bottom2 });
                triangles.AddRange(new int[] { bottom2, top1, top2 });
            }

            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateBounds();
            return mesh;
        }

        private void GenerateTrafficLights(RoadNetwork network, GridLayout layout, RoadGenerationSettings settings)
        {
            // Generate traffic lights at major intersections
            foreach (var intersection in network.Intersections)
            {
                if (intersection.Connections.Count >= 4) // 4-way intersection
                {
                    var trafficLight = CreateTrafficLight(intersection, TrafficLightType.FourWay, settings);
                    network.TrafficLights.Add(trafficLight);
                }
                else if (intersection.Connections.Count >= 2) // 2-way intersection
                {
                    var trafficLight = CreateTrafficLight(intersection, TrafficLightType.TwoWay, settings);
                    network.TrafficLights.Add(trafficLight);
                }
            }
        }

        private TrafficLightObject CreateTrafficLight(RoadIntersection intersection, TrafficLightType type, RoadGenerationSettings settings)
        {
            var trafficLight = new TrafficLightObject
            {
                Position = intersection.Position + Vector3.up * settings.PoleHeight,
                Rotation = Quaternion.identity,
                Type = type,
                Mesh = CreateTrafficLightMesh(type)
            };

            return trafficLight;
        }

        private Mesh CreateTrafficLightMesh(TrafficLightType type)
        {
            var mesh = new Mesh();
            mesh.name = $"TrafficLight_{type}";

            // Simple box for now - can be enhanced with proper traffic light geometry
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            float width = 0.3f;
            float height = 0.8f;
            float depth = 0.2f;

            // Create box vertices
            Vector3[] boxVertices = {
                new Vector3(-width/2, -height/2, -depth/2), // 0
                new Vector3(width/2, -height/2, -depth/2),  // 1
                new Vector3(width/2, height/2, -depth/2),   // 2
                new Vector3(-width/2, height/2, -depth/2),  // 3
                new Vector3(-width/2, -height/2, depth/2),  // 4
                new Vector3(width/2, -height/2, depth/2),   // 5
                new Vector3(width/2, height/2, depth/2),    // 6
                new Vector3(-width/2, height/2, depth/2)    // 7
            };

            vertices.AddRange(boxVertices);

            // Simple normals
            for (int i = 0; i < 8; i++)
            {
                normals.Add(Vector3.forward);
            }

            // Simple UVs
            for (int i = 0; i < 8; i++)
            {
                uvs.Add(new Vector2((i % 2), (i / 2) % 2));
            }

            // Box triangles
            int[] boxTriangles = {
                0, 2, 1, 0, 3, 2, // Front face
                1, 6, 5, 1, 2, 6, // Right face
                5, 7, 4, 5, 6, 7, // Back face
                4, 3, 0, 4, 7, 3, // Left face
                3, 6, 2, 3, 7, 6, // Top face
                0, 5, 4, 0, 1, 5  // Bottom face
            };

            triangles.AddRange(boxTriangles);

            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    public class GridLayout
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public float BlockSize { get; set; }
        public float RoadWidth { get; set; }
        public float IntersectionSize { get; set; }
    }
} 
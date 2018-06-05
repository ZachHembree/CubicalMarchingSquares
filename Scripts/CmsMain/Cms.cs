﻿using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace CmsMain
{
    public static partial class Volume
    {
        public static string importVoxTime { get; private set; }
        public static string dimensions { get; private set; }
        public static string octantSize { get; private set; }
        public static string startVertices { get; private set; }

        private static Vector3 scale, delta, step, maximums;
        private static readonly int[] cubeDirs = new int[] { 1, 1, 0, 0 }, planeDirs = new int[] { 2, 3, 0, 1 };
        private static readonly int[][] edgeDirs = new int[][] { new int[] { 3, 2, 3, 2 }, new int[] { 1, 0, 1, 0 } };

        private static List<Edge>[][][] GetEdges(IList<Octant>[][] octants, out List<Vector3> vertices) // interior edges shouldn't be needed
        {
            int length = octants.Length, width = octants[1].Length;
            List<Edge>[][][] edges = new List<Edge>[3][][]
            { new List<Edge>[length][], new List<Edge>[length][], new List<Edge>[length][] };
            vertices = new List<Vector3>((length * width * 6) + (length * width / 2));

            for (int x = 0; x < length; x++)
            {
                edges[0][x] = new List<Edge>[width];
                edges[1][x] = new List<Edge>[width];
                edges[2][x] = new List<Edge>[width];

                edges[0][x][width - 1] = new List<Edge>(0);
                edges[1][x][width - 1] = new List<Edge>(0);
                edges[2][x][width - 1] = new List<Edge>(0);
            }

            for (int y = 0; y < width - 1; y++)
            {
                edges[0][length - 1][y] = new List<Edge>(0);
                edges[1][length - 1][y] = new List<Edge>(0);
                edges[2][length - 1][y] = new List<Edge>(0);
            }

            for (int x = 0; x < length - 1; x++)
                for (int y = 0; y < width - 1; y++)
                {
                    edges[0][x][y] = GetEdgeColumnX(vertices, octants[x][y], octants[x + 1][y]);
                    edges[1][x][y] = GetEdgeColumnY(vertices, octants[x][y], octants[x][y + 1]);
                    edges[2][x][y] = GetEdgeColumnZ(vertices, octants[x][y]);
                }

            startVertices = ("Starting Vertices: " + vertices.Count + "\n");

            return edges;
        }

        private static List<Edge> GetEdgeColumnX(List<Vector3> vertices, IList<Octant> x1, IList<Octant> x2)
        {
            int n1 = 0, n2 = 0, s1, s2;
            List<Edge> edges = new List<Edge>(x1.Count + x2.Count);

            while (n1 < x1.Count || n2 < x2.Count)
            {
                s1 = n1 < x1.Count ? x1[n1].range : int.MaxValue;
                s2 = n2 < x2.Count ? x2[n2].range : int.MaxValue;

                if (s1 < s2)
                {
                    edges.Add(new Edge(s1, vertices.Count, 1));
                    vertices.Add(new Vector3((x1[n1].x + delta.x), x1[n1].y, x1[n1].z));
                    n1++; 
                }
                else if (s1 > s2)
                {
                    edges.Add(new Edge(s2, vertices.Count, 2));
                    vertices.Add(new Vector3((x2[n2].x - delta.x), x2[n2].y, x2[n2].z));
                    n2++;
                }
                else
                {
                    edges.Add(new Edge(s1, -1, 3));
                    n1++; n2++;
                }
            }

            return edges;
        }

        private static List<Edge> GetEdgeColumnY(List<Vector3> vertices, IList<Octant> y1, IList<Octant> y2)
        {
            int n1 = 0, n2 = 0, s1, s2;
            List<Edge> edges = new List<Edge>(y1.Count + y2.Count);

            while (n1 < y1.Count || n2 < y2.Count)
            {
                s1 = n1 < y1.Count ? y1[n1].range : int.MaxValue;
                s2 = n2 < y2.Count ? y2[n2].range : int.MaxValue;

                if (s1 < s2)
                {
                    edges.Add(new Edge(s1, vertices.Count, 1));
                    vertices.Add(new Vector3(y1[n1].x, (y1[n1].y + delta.y), y1[n1].z));
                    n1++; 
                }
                else if (s1 > s2)
                {
                    edges.Add(new Edge(s2, vertices.Count, 2));
                    vertices.Add(new Vector3(y2[n2].x, (y2[n2].y - delta.y), y2[n2].z));
                    n2++;
                }
                else
                {
                    edges.Add(new Edge(s1, -1, 3));
                    n1++; n2++;
                }
            }

            return edges;
        }

        private static List<Edge> GetEdgeColumnZ(List<Vector3> vertices, IList<Octant> z)
        {
            int start;
            List<Edge> edges = new List<Edge>(z.Count);

            for (int n = 0; n < z.Count; n++)
            {
                start = z[n].range;

                if (n == 0 || (z[n - 1].range != start - 1))
                {
                    edges.Add(new Edge(start - 1, vertices.Count, 2));
                    vertices.Add(new Vector3(z[n].x, z[n].y, (z[n].z - delta.z)));
                }

                if ((n + 1 == z.Count) || (z[n + 1].range != start + 1))
                {
                    edges.Add(new Edge(start, vertices.Count, 1));
                    vertices.Add(new Vector3(z[n].x, z[n].y, z[n].z + delta.z));
                }
                else
                    edges.Add(new Edge(start, -1, 3));
            }

            return edges;
        }

        private static List<Segment> GetSegments(List<Edge>[][][] edges)
        {
            int length = edges[0].Length - 1, width = edges[0][0].Length - 1;
            List<Segment> segments =  new List<Segment>(length * width * 3);

            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    GetSegmentColumnX(segments, edges[1][x][y], edges[2][x][y], edges[2][x][y + 1]);
                    GetSegmentColumnY(segments, edges[0][x][y], edges[2][x][y], edges[2][x + 1][y]);
                    GetSegmentColumnZ(segments, edges[0][x][y], edges[0][x][y + 1], edges[1][x][y], edges[1][x + 1][y]);
                }
            }

            return segments;
        }

        private static void GetSegmentColumnX(List<Segment> segments, List<Edge> y, List<Edge> z1, List<Edge> z2)
        {
            int start, last = int.MinValue, n1 = 0, n2 = 0;
            Edge[] sides = new Edge[4];

            for (int z = 0; z < y.Count; z++)
            {
                start = y[z].z;

                if (last != start - 1)
                {
                    start--;

                    sides[0] = y[z];
                    sides[1] = GetEdge(z2, start, ref n1);
                    sides[2] = Edge.zero;
                    sides[3] = GetEdge(z1, start, ref n2);

                    AddSegments(segments, sides);
                    start++;
                }

                sides[0] = GetNextEdge(y, z + 1, start + 1);
                sides[1] = GetEdge(z2, start, ref n1);
                sides[2] = y[z];
                sides[3] = GetEdge(z1, start, ref n2);

                AddSegments(segments, sides);
                last = start;
            }
        }

        private static void GetSegmentColumnY(List<Segment> segments, List<Edge> x, List<Edge> z1, List<Edge> z2)
        {
            int start, last = int.MinValue, n1 = 0, n2 = 0;
            Edge[] sides = new Edge[4];

            for (int z = 0; z < x.Count; z++)
            {
                start = x[z].z;

                if (last != start - 1)
                {
                    start--;

                    sides[0] = GetEdge(z2, start, ref n1);
                    sides[1] = x[z];
                    sides[2] = GetEdge(z1, start, ref n2);
                    sides[3] = Edge.zero;

                    AddSegments(segments, sides);
                    start++;
                }

                sides[0] = GetEdge(z2, start, ref n1);
                sides[1] = GetNextEdge(x, z + 1, start + 1);
                sides[2] = GetEdge(z1, start, ref n2);
                sides[3] = x[z];

                AddSegments(segments, sides);
                last = start;
            }
        }

        private static void GetSegmentColumnZ(List<Segment> segments, List<Edge> x1, List<Edge> x2, List<Edge> y1, List<Edge> y2)
        {
            int start = int.MinValue, n1 = 0, n2 = 0, n3 = 0, n4 = 0;
            Edge s1, s2;
            Edge[] sides = new Edge[4];

            while (start != int.MaxValue)
            {
                s1 = n1 < x1.Count ? x1[n1] : Edge.zero;
                s2 = n2 < x2.Count ? x2[n2] : Edge.zero;

                if (s1.z < s2.z) { n1++; start = s1.z; s2 = Edge.zero; }
                else if (s1.z > s2.z) { n2++; start = s2.z; s1 = Edge.zero; }
                else { n1++; n2++; start = s1.z; }

                if (start != int.MaxValue)
                {
                    sides[0] = s2;
                    sides[1] = GetEdge(y2, start, ref n3);
                    sides[2] = s1;
                    sides[3] = GetEdge(y1, start, ref n4);

                    AddSegments(segments, sides);
                }
            }
        }

        private static Edge GetEdge(List<Edge> edges, int z, ref int i)
        {
            for (int n = i; n < edges.Count; n++)
            {
                if (edges[n].z == z)
                {
                    i = n;
                    return edges[n];
                }
                else if (edges[n].z > z)
                {
                    i = n;
                    return Edge.zero;
                }
            }

            return Edge.zero;
        }

        private static Edge GetNextEdge(List<Edge> edges, int i, int z)
        {
            if (i < edges.Count && edges[i].z == z)
                return edges[i];

            return Edge.zero;
        }

        private static void AddSegments(List<Segment> segments, Edge[] sides)
        {
            if (sides != null)
            {
                int conf = sides[0].conf + (sides[2].conf * 4);

                if (conf != 0 && conf != 15)
                {
                    DirEdge[] edges = new DirEdge[4];
                    int count = 0;

                    for (int n = 0; n < 4; n++)
                        if (sides[n].index != -1)
                        {
                            edges[count] = new DirEdge(n, sides[n]);
                            count++;
                        }

                    if (conf == 6)
                    {
                        DirEdge c = edges[0];
                        edges[0] = edges[2];
                        edges[2] = c;
                    }

                    for (int n = 0; n < 4; n += 2)
                        if (edges[n] != null)
                        {
                            if (sides[0].conf == 0 || sides[0].conf == 2)
                                segments.Add(new Segment(edges[n + 1], edges[n]));
                            else
                                segments.Add(new Segment(edges[n], edges[n + 1]));
                        }
                }
            }
        }

        private static List<int> GetMesh(List<Vector3> vertices, List<Segment> segments)
        {
            List<int> indices = new List<int>(12), triangles = new List<int>(vertices.Count * 9);

            foreach (Segment seg in segments)
            {
                if (!seg.IsUsed(0))
                    GetSurface(seg, seg.end, 0, indices, vertices, triangles);

                if (!seg.IsUsed(1))
                    GetSurface(seg, seg.start, 1, indices, vertices, triangles);
            }

            return triangles;
        }

        private static void GetSurface(Segment segment, DirEdge next, int dir, List<int> indices, List<Vector3> vertices, List<int> triangles)
        {
            Edge start = next.edge;
            indices.Clear();
            indices.Add(next.edge.index);

            while (segment.TryGetOppositeEdge(dir, ref next) && start != next.edge)
            {
                indices.Add(next.edge.index);
                segment = next.edge.segments[edgeDirs[dir][next.dir]];
                dir = cubeDirs[next.dir];
            }

            if (next == null || next.edge.index != indices[0] || indices.Count < 3)
                throw new Exception("[Cube Error] Could not form an enclosed surface. (" + indices.Count + ")");

            GetPolys(vertices, triangles, indices);
        }

        private static void GetPolys(List<Vector3> vertices, List<int> triangles, List<int> indices)
        {
            if (indices.Count > 4)
            {
                int pos = vertices.Count;

                for (int n = 0; n < indices.Count - 1; n++)
                {
                    triangles.Add(pos);
                    triangles.Add(indices[n]);
                    triangles.Add(indices[n + 1]);
                }

                triangles.Add(pos);
                triangles.Add(indices[indices.Count - 1]);
                triangles.Add(indices[0]);

                vertices.Add(GetAvgVertex(vertices, indices));
            }
            else
            {
                for (int n = 1; n < indices.Count - 1; n++)
                {
                    triangles.Add(indices[0]);
                    triangles.Add(indices[n]);
                    triangles.Add(indices[n + 1]);
                }
            }
        }

        private static Vector3 GetAvgVertex(List<Vector3> vertices, List<int> indices)
        {
            Vector3 vec = Vector3.zero;

            for (int a = 0; a < indices.Count; a++)
                vec += vertices[indices[a]];

            return vec / indices.Count;
        }
    }
}

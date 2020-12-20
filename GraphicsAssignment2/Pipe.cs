using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;

namespace GraphicsAssignment1
{
    class Pipe
    {
        public Matrix4 pipeMatrix;
        public float torusRadius;
        public float subPipePercentage;
        public float pipeRollRadians;

        private const int VertexBufferLocation = 0;
        private const int VertexColourLocation = 1;
        private const int VertexNormalLocation = 2;

        private float[] Vertices;
        private int[] Triangles;
        private float[] Normals;
        private float[] Colours;

        private int VertexBufferObject;
        private int VertexColourObject;
        private int NormalsBufferObject;
        private int TriangleBufferObject;

        public struct PipeParams
        {
            public int pipeSegments;
            public float pipeRadius;
            public int torusSegments;
            public float torusRadius;
            public float subPipePercentage;
        }

        // Runs the initial code to create a torus, but not draw it yet
        public Pipe(int pipeSegments, float pipeRadius, int torusSegments, float torusRadius, float subPipePercentage, float pipeRollRadians, bool invertNormals)
        {
            this.torusRadius = torusRadius;
            this.subPipePercentage = subPipePercentage;
            this.pipeRollRadians = pipeRollRadians;
            CreatePipe(pipeSegments, pipeRadius, torusSegments, torusRadius, subPipePercentage, invertNormals);

            CreateBuffersFromModelData();
        }

        public Pipe(PipeParams pipeParams, float pipeRollRadians, bool invertNormals)
        {
            this.torusRadius = pipeParams.torusRadius;
            this.subPipePercentage = pipeParams.subPipePercentage;
            this.pipeRollRadians = pipeRollRadians;

            CreatePipe(pipeParams.pipeSegments, pipeParams.pipeRadius, pipeParams.torusSegments, pipeParams.torusRadius, pipeParams.subPipePercentage, invertNormals);
            CreateBuffersFromModelData();
        }

        // for creating an empty initial pipe to attach to 
        public Pipe()
        {
            this.torusRadius = 0f;
            this.subPipePercentage = 0f;
            this.pipeRollRadians = 0f;
        }


        // Used to get a point on a torus with some given u/v values (u = angle along the torus, v = angle along pipe)
        public static Vector3 GetPointOnTorus(float u, float v, float torusRadius, float pipeRadius)
        {
            Vector3 point = new Vector3
            {
                X = (torusRadius + pipeRadius * MathF.Cos(v)) * MathF.Cos(u),
                Y = (torusRadius + pipeRadius * MathF.Cos(v)) * MathF.Sin(u),
                Z = pipeRadius * MathF.Sin(v)
            };

            return point;
        }

        // Generates a torus given the dimensions, and stores it within the Vertices, Triangles, and Normals arrays of this object.
        // Created by procedurally joining all vertices of a torus with correct triangles, using the equation of a torus from the above function.
        public void CreatePipe(int pipeSegments, float pipeRadius, int torusSegments, float torusRadius, float subPipePercentage, bool invert)
        {
            int renderTorusSegments = (int)MathF.Round((torusSegments) * subPipePercentage);

            Vector3[] vertices = new Vector3[renderTorusSegments * pipeSegments * 4];

            int[] triangles = new int[renderTorusSegments * pipeSegments * 6];

            Vector3[] normals = new Vector3[renderTorusSegments * pipeSegments * 4];

            float u = 0;
            float v = 0;

            //used to generate the array of vertices as required 
            for (int i = 0; i < renderTorusSegments; i++) //for each torusSegment, we want to save all vertices in the loop (2 each time for a pipeSegments amount of times)
            {
                for (int j = 0; j < (pipeSegments + 1); j++)
                {
                    Vector3 point1 = GetPointOnTorus(u + (2f * MathF.PI / (torusSegments)), v, torusRadius, pipeRadius);
                    Vector3 point2 = GetPointOnTorus(u, v, torusRadius, pipeRadius);

                    Vector3 pipeCentre1 =
                        (GetPointOnTorus(u + (2f * MathF.PI / (torusSegments)), 0, torusRadius, pipeRadius)
                        - GetPointOnTorus(u + (2f * MathF.PI / (torusSegments)), 0, torusRadius, pipeRadius).Normalized() * pipeRadius);
                    Vector3 pipeCentre2 = (GetPointOnTorus(u, 0, torusRadius, pipeRadius) - GetPointOnTorus(u, 0, torusRadius, pipeRadius).Normalized() * pipeRadius);

                    vertices[i * (pipeSegments * 2) + j * 2] = point1;
                    vertices[i * (pipeSegments * 2) + j * 2 + 1] = point2;


                    // generates inverted normals for more realistic lighting on the inside of the torus if this is required.
                    if (invert)
                    {
                        normals[i * (pipeSegments * 2) + j * 2] = -(point1 - pipeCentre1).Normalized();
                        normals[i * (pipeSegments * 2) + j * 2 + 1] = -(point2 - pipeCentre2).Normalized();
                    }
                    else
                    {
                        normals[i * (pipeSegments * 2) + j * 2] = (point1 - pipeCentre1).Normalized();
                        normals[i * (pipeSegments * 2) + j * 2 + 1] = (point2 - pipeCentre2).Normalized();
                    }
                    v += (2f * MathF.PI) / (pipeSegments - 1);
                }
                u += (2f * MathF.PI) / (torusSegments);
            }

            // connecting all vertices in a procedural way given the amount of segments that we want to generate
            int vertIndex = 0;
            for (int j = 0; j < renderTorusSegments; j++)
            {
                for (int i = 0; i < pipeSegments; i++)
                {
                    if (invert)
                    {
                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 0] = vertIndex + 3;    //0  //reverse these for outside view
                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 1] = vertIndex + 2;    //2
                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 2] = vertIndex + 0;    //3

                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 3] = vertIndex + 0;    //3  //reverse these for outside view
                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 4] = vertIndex + 1;    //1
                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 5] = vertIndex + 3;    //0 

                    }
                    else
                    {
                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 0] = vertIndex + 0;    //0  //reverse these for outside view
                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 1] = vertIndex + 2;    //2
                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 2] = vertIndex + 3;    //3

                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 3] = vertIndex + 3;    //3  //reverse these for outside view
                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 4] = vertIndex + 1;    //1
                        triangles[6 * (pipeSegments - 1) * j + 6 * i + 5] = vertIndex + 0;    //0 

                    }
                    vertIndex += 2;
                }
            }

            Triangles = triangles;
            
            // turning our vector arrays (vertices, normals) into float arrays
            Vertices = new float[vertices.Length * 3];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vertices[3 * i] = vertices[i].X;
                Vertices[3 * i + 1] = vertices[i].Y;
                Vertices[3 * i + 2] = vertices[i].Z;
            }

            Normals = new float[normals.Length * 3];
            for (int i = 0; i < normals.Length; i++)
            {
                Normals[3 * i] = normals[i].X;
                Normals[3 * i + 1] = normals[i].Y;
                Normals[3 * i + 2] = normals[i].Z;
            }

            Colours = new float[normals.Length * 4];
            for (int i = 0; i < normals.Length; i++)
            {
                Colours[4 * i] = normals[i].X;
                Colours[4 * i + 1] = normals[i].Y;
                Colours[4 * i + 2] = normals[i].Z;
                Colours[4 * i + 3] = 0.2f;
            }

        }

        // After running the above function, we must store its data in buffers for use in OpenGL
        public void CreateBuffersFromModelData()
        {
            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            VertexColourObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexColourObject);
            GL.BufferData(BufferTarget.ArrayBuffer, Colours.Length * sizeof(float), Colours, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            NormalsBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, NormalsBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, Normals.Length * sizeof(float), Normals, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            TriangleBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, TriangleBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, Triangles.Length * sizeof(int), Triangles, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /* Draw the pipe by binding the VBOs and drawing triangles */
        public void DrawPipe(int drawmode)
        {
            /* Bind pipe vertices. */
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.EnableVertexAttribArray(VertexBufferLocation);
            GL.VertexAttribPointer(VertexBufferLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            /* Bind pipe colours. */
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexColourObject);
            GL.EnableVertexAttribArray(VertexColourLocation);
            GL.VertexAttribPointer(VertexColourLocation, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            /* Bind pipe normals. */
            GL.BindBuffer(BufferTarget.ArrayBuffer, NormalsBufferObject);
            GL.EnableVertexAttribArray(VertexNormalLocation);
            GL.VertexAttribPointer(VertexNormalLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, TriangleBufferObject);

            // Switch between filled and wireframe modes
            if (drawmode == 1)
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            else if (drawmode == 2)
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line); 

            // Draw the pipe in triangles
            GL.DrawElements(BeginMode.Triangles, Triangles.Length, DrawElementsType.UnsignedInt, 0); 
        }
    }
}

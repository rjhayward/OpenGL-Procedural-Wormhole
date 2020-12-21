using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Generic;

namespace GraphicsAssignment2
{
    class Skybox
    {
        private int VertexArrayObject;
        private int VertexBufferObject;
        public int textureID;

        private const int VertexBufferLocation = 0;

        //
        string[] faces =
        {
            "../../../GreenSkybox_right.png",
            "../../../GreenSkybox_left.png",
            "../../../GreenSkybox_down.png",
            "../../../GreenSkybox_up.png",
            "../../../GreenSkybox_back.png",
            "../../../GreenSkybox_front.png"
        };

        float[] Vertices =
        {
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f
        };

        public Skybox()
        {
            textureID = Load(faces);

            CreateBuffersFromModelData();
        }

        //load skybox (image sharp)
        int Load(string[] faces)
        {
            int textureID;
            GL.GenTextures(1, out textureID);
            GL.BindTexture(TextureTarget.TextureCubeMap, textureID);

            //int width, height, nrChannels;
            System.Console.Out.WriteLine("loading cubemap");
            for (int i = 0; i < faces.Length; i++)
            {
                Image<Rgba32> image = Image.Load<Rgba32>(faces[i]);
                image.Mutate(x => x.Flip(FlipMode.Vertical));

                Rgba32[] tempPixels = new Rgba32[image.Height* image.Width];

                for (int row = 0; row < image.Height; row++)
                {
                    Rgba32[] tempPixelsRow = image.GetPixelRowSpan(row).ToArray();

                    for (int col = 0; col < image.Width; col++)
                    {
                        tempPixels[image.Height * row + col] = tempPixelsRow[col];
                    }
                }

                List<byte> pixels = new List<byte>();

                foreach (Rgba32 p in tempPixels)
                {
                    pixels.Add(p.R);
                    pixels.Add(p.G);
                    pixels.Add(p.B);
                    pixels.Add(p.A);
                }

                if (pixels.Count > 0)
                {
                    System.Console.Out.WriteLine(pixels.Count / 4096);

                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels.ToArray());

                    System.Console.Out.WriteLine("loaded cubemap");
                }
                else
                {
                    System.Console.Out.WriteLine("error loading cubemap");
                }



                ////Get an array of the pixels, in ImageSharp's internal format.
                //using (var memoryStream = new MemoryStream())
                //{
                //    var imageEncoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(PngFormat.Instance);
                //    image.Save(memoryStream, imageEncoder);
                //    byte[] pixels = memoryStream.ToArray();



                //GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);




            }

            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)All.ClampToEdge);

            return textureID;
        }

        // After running the above function, we must store its data in buffers for use in OpenGL
        void CreateBuffersFromModelData()
        {

            VertexArrayObject = GL.GenVertexArray();
            VertexBufferObject = GL.GenBuffer();

            GL.BindVertexArray(VertexArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices, BufferUsageHint.StaticDraw);
            
            GL.EnableVertexAttribArray(VertexBufferLocation);
            GL.VertexAttribPointer(VertexBufferLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);


        }

        /* Draw the pipe by binding the VBOs and drawing triangles */
        public void Draw()
        {
            GL.BindVertexArray(VertexArrayObject);
            GL.ActiveTexture(TextureUnit.Texture0);

            GL.BindTexture(TextureTarget.TextureCubeMap, textureID);

            GL.DrawArrays(PrimitiveType.Triangles, VertexBufferLocation, 36);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float), Vertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(VertexBufferLocation);
            GL.VertexAttribPointer(VertexBufferLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.BindVertexArray(0);

            /////* Bind pipe vertices. */
            //GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
            //GL.EnableVertexAttribArray(VertexBufferLocation);
            //GL.VertexAttribPointer(VertexBufferLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

        }
    }
}

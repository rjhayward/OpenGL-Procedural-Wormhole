using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace GraphicsAssignment2
{
    class Game : GameWindow
    {
        Shader shader;
        Shader skyboxShader;
        Skybox skybox;

        int shaderProgram;
        int VertexArrayObject;
        int modelID, projectionID, viewID, intensityID;

        public Stack<Matrix4> model;
        Matrix4 view;

        bool toPlayMusic = true;
        int drawMode;
        float intensity, horizontalAngle, verticalAngle;

        // cryptographically random numbers
        private byte[] randomBytes;

        // Queue of pipes
        private Queue<Pipe> pipeQueue;

        KeyboardState keyboardState, lastKeyboardState;

        public System.Media.SoundPlayer player = new System.Media.SoundPlayer("../../../soundtrack.wav");

        public static GraphicsMode gfxMode = new GraphicsMode(new ColorFormat(8, 8, 8, 0),
              24, // Depth bits
              8,  // Stencil bits
              4   // FSAA samples for anti-aliasing
            );
        
        public Game(int width, int height, string title) :
            base(width, height, gfxMode, title)
        {
            model = new Stack<Matrix4>();
            drawMode = 1;
            intensity = 1f;
            randomBytes = new byte[1];
            pipeQueue = new Queue<Pipe>();
            horizontalAngle = 0;
            verticalAngle = 0;
        }

        Pipe currentPipe;
        protected override void OnLoad(EventArgs e)
        {
            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);

            shader = new Shader("../../../shader.vert", "../../../shader.frag");
            skyboxShader = new Shader("../../../skyboxShader.vert", "../../../skyboxShader.frag");
            skybox = new Skybox();

            GL.ClearColor(0.4f, 0.5f, 0.4f, 0.0f); 
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            
            Pipe initPipe = new Pipe() { pipeMatrix = Matrix4.Identity };

            pipeQueue.Enqueue(initPipe);

            currentPipe = initPipe;

            base.OnLoad(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // Get current state
            keyboardState = Keyboard.GetState();

            if (IsKeyDown(Key.Escape))
            {
                Exit();
            }
            
            base.OnUpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.DepthMask(true);

            //render tunnels
            shaderProgram = shader.Use();
            {
                modelID = GL.GetUniformLocation(shaderProgram, "model");
                projectionID = GL.GetUniformLocation(shaderProgram, "projection");
                viewID = GL.GetUniformLocation(shaderProgram, "view");
                intensityID = GL.GetUniformLocation(shaderProgram, "intensity");

                SetModelMatrix();
                SetProjectionMatrix(90f);
                SetViewMatrix();
            }

            GL.DepthMask(false);
            GL.DepthFunc(DepthFunction.Lequal);

            // render skybox
            shaderProgram = skyboxShader.Use();
            {
                projectionID = GL.GetUniformLocation(shaderProgram, "projection");
                viewID = GL.GetUniformLocation(shaderProgram, "view");

                SetProjectionMatrix(90f);

                view = new Matrix4(new Matrix3(view)); // remove translation from view matrix for skybox

                GL.UniformMatrix4(viewID, false, ref view);

                skybox.Draw();
            }

            GL.DepthFunc(DepthFunction.Less);


            if (toPlayMusic)
            {
                // play background music after all skybox is loaded
                player.PlayLooping();
                toPlayMusic = false;
            }

            lastKeyboardState = keyboardState;

            Context.SwapBuffers();

            base.OnRenderFrame(e);
        }


        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);

            base.OnResize(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.DeleteVertexArrays(1, ref VertexArrayObject);

            shader.Dispose();
            base.OnUnload(e);
        }

        public bool IsKeyDown(Key key)
        {
            return (keyboardState[key] && (keyboardState[key] != lastKeyboardState[key]));
        }

        void SetProjectionMatrix(float fov)
        {
            Matrix4 projection;
            projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI * (fov / 180f), Width / (float)Height, 0.2f, 1024.0f);

            GL.UniformMatrix4(projectionID, false, ref projection);

        }

        void SetViewMatrix()
        {
            /* Controls for changing light intensity*/
            if (IsKeyDown(Key.F1)) intensity = 0.25f;
            if (IsKeyDown(Key.F2)) intensity = 0.5f;
            if (IsKeyDown(Key.F3)) intensity = 1.0f;
            if (IsKeyDown(Key.F4)) intensity = 2.0f;
            if (IsKeyDown(Key.F5)) intensity = 3f;

            /* Controls for changing from wireframe to fill and back */
            if (IsKeyDown(Key.Number1)) drawMode = 1;
            if (IsKeyDown(Key.Number2)) drawMode = 2;

            // get mouse info and use it to rotate
            MouseState cursorState = Mouse.GetCursorState();
            horizontalAngle -= 0.005f * (Width / 2 - cursorState.X);
            verticalAngle += 0.005f * (Height / 2 - cursorState.Y);
            Mouse.SetPosition(Width / 2, Height / 2);

            Vector4 eye = new Vector4(pipeQueue.ElementAt(0).torusRadius, 0, 0, 1);

            Vector4 target = new Vector4(0, 0, 0, 1);

            if (pipeQueue.Count > 100)
            {
                eye *= pipeQueue.ElementAt(50).pipeMatrix;

                target = new Vector4(pipeQueue.ElementAt(100).torusRadius, 0, 0, 1);

                target *= pipeQueue.ElementAt(100).pipeMatrix;
            }

            // apply transformations to camera
            Matrix4.CreateRotationY(-horizontalAngle + MathF.PI, out Matrix4 XRotation); //rotating in clockwise direction around x-axis

            Matrix4.CreateRotationX(-verticalAngle - 1 * MathF.PI / 4, out Matrix4 YRotation); //rotating in clockwise direction around x-axis

            Matrix4 Rotation = Matrix4.Mult(XRotation, YRotation);

            view = Matrix4.LookAt(new Vector3(eye), new Vector3(target), new Vector3(0f, 1f, 0f));

            view = Matrix4.Mult(view, Rotation);

            // send these uniforms to the shader
            GL.UniformMatrix4(viewID, false, ref view);
            GL.Uniform1(intensityID, intensity);
        }


        void AttachNewPipe(Pipe originalPipe, ref Pipe newPipe)
        {
            Matrix4 pipeRoll = Matrix4.CreateRotationY(MathF.Floor(newPipe.pipeRollRadians / 2 * MathF.PI * (newPipe.pipeSegments-1)) * 2 * MathF.PI / (newPipe.pipeSegments-1));

            Matrix4 translationInOGPipeSpace = Matrix4.CreateTranslation(new Vector3(newPipe.torusRadius, 0f, 0f)) * originalPipe.pipeMatrix;

            Matrix4 translationInNewPipeSpace = Matrix4.CreateTranslation(new Vector3(-newPipe.torusRadius, 0f, 0f));

            Matrix4 torusRotation = Matrix4.CreateRotationZ(-originalPipe.subPipePercentage * 2 * MathF.PI);

            Matrix4 modelMatrix = torusRotation * translationInNewPipeSpace * pipeRoll * translationInOGPipeSpace;

            newPipe.pipeMatrix = modelMatrix;
        }

        void SetModelMatrix()
        {
            Pipe.PipeParams newPipeParams = new Pipe.PipeParams { pipeSegments = 180, pipeRadius = 18f, torusSegments = 360, torusRadius = 12, subPipePercentage = 1/45f };

            // fill queue with torus segments
            if (pipeQueue.Count < 1000)
            {

                for (int i = 0; i < 1000; i++)
                {
                    RandomNumberGenerator.Create().GetNonZeroBytes(randomBytes);
                    Pipe newPipe = new Pipe(newPipeParams, randomBytes[0], true);
                    pipeQueue.Enqueue(newPipe);
                    AttachNewPipe(currentPipe, ref newPipe);

                    currentPipe = newPipe;
                }
            }

            RandomNumberGenerator.Create().GetNonZeroBytes(randomBytes);

            // queue size
            if (pipeQueue.Count >= 1000)
            {
                pipeQueue.Dequeue();
            }

            Pipe newPipe1 = new Pipe(newPipeParams, randomBytes[0], true);
            pipeQueue.Enqueue(newPipe1);
            AttachNewPipe(currentPipe, ref newPipe1);

            currentPipe = newPipe1;

            // draw all pipes
            if (pipeQueue.Count > 0)
            {
                Pipe pipeToDraw;

                for (int i = 0; i < pipeQueue.Count; i++)
                {
                    pipeToDraw = pipeQueue.ElementAt(i);

                    GL.UniformMatrix4(modelID, false, ref pipeToDraw.pipeMatrix);

                    // checking it is not the empty init pipe
                    if (pipeToDraw.subPipePercentage != 0.0f) pipeToDraw.DrawPipe(drawMode);
                }
            }
        }
    }
}

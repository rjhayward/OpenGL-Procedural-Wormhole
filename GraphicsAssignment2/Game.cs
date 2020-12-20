using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Cryptography;

namespace GraphicsAssignment1
{
    class Game : GameWindow
    {
        Shader shader;
        int shaderProgram;
        int VertexArrayObject;

        public Stack<Matrix4> model;
        Matrix4 view;

        int modelID, projectionID, viewID, intensityID, timeElapsedID, partyModeID;

        int drawMode, partyMode;

        float intensity, timeElapsed, horizontalAngle, verticalAngle, lastScroll;

        Vector3 scaleModifier;

        // cryptographically random numbers
        byte[] randomBytes;

        // Queue of pipes
        Queue<Pipe> pipeQueue;


        KeyboardState keyboardState, lastKeyboardState;


        public Game(int width, int height, string title) :
            base(width, height, GraphicsMode.Default, title)
        {
            model = new Stack<Matrix4>();
            drawMode = 1;
            partyMode = 1;
            intensity = 1f;
            timeElapsed = 0f;
            scaleModifier = new Vector3(1f, 1f, 1f);
            randomBytes = new byte[1];
            pipeQueue = new Queue<Pipe>();


        }

        Pipe currentPipe;
        protected override void OnLoad(EventArgs e)
        {
            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);

            shader = new Shader("../../../shader.vert", "../../../shader.frag");
            GL.ClearColor(0.2f, 0.3f, 0.3f, 0.0f);
            //GL.Enable(EnableCap.ProgramPointSize);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            

            lastScroll = Mouse.GetState().WheelPrecise;

            /* Define uniforms to send to vertex shader */  
            scaleModifier = new Vector3(-1f, -1f, -1f);

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


            timeElapsed += 0.1f;
            GL.Uniform1(timeElapsedID, timeElapsed);

            base.OnUpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shaderProgram = shader.Use();
            modelID = GL.GetUniformLocation(shaderProgram, "model");
            projectionID = GL.GetUniformLocation(shaderProgram, "projection");
            viewID = GL.GetUniformLocation(shaderProgram, "view");
            intensityID = GL.GetUniformLocation(shaderProgram, "intensity");
            timeElapsedID = GL.GetUniformLocation(shaderProgram, "timeElapsed");
            partyModeID = GL.GetUniformLocation(shaderProgram, "partyMode");
           
            SetModelMatrix();
            SetProjectionMatrix(90f);
            SetViewMatrix();

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
            if (IsKeyDown(Key.F1)) intensity = 0.5f;
            if (IsKeyDown(Key.F2)) intensity = 1.0f;
            if (IsKeyDown(Key.F3)) intensity = 1.5f;
            if (IsKeyDown(Key.F4)) intensity = 2.0f;
            if (IsKeyDown(Key.F5)) intensity = 2.5f;

            /* Controls for changing partymode intensity*/
            if (IsKeyDown(Key.Q)) partyMode = 1;
            if (IsKeyDown(Key.W)) partyMode = 2;
            if (IsKeyDown(Key.E)) partyMode = 3;
            if (IsKeyDown(Key.R)) partyMode = 4;

            /* Controls for changing from wireframe to fill and back */
            if (IsKeyDown(Key.Number1)) drawMode = 1;
            if (IsKeyDown(Key.Number2)) drawMode = 2;

            // these controls can also be done with scroll wheel; scaling the view
            if (IsKeyDown(Key.Up))
            {
                scaleModifier.X -= 0.02f;
                scaleModifier.Y -= 0.02f;
                scaleModifier.Z -= 0.02f;
            }
            if (IsKeyDown(Key.Down))
            {
                scaleModifier.X += 0.02f;
                scaleModifier.Y += 0.02f;
                scaleModifier.Z += 0.02f;
            }
            
            // get scroll wheel info and use it to transform
            float changeInScroll = lastScroll - Mouse.GetState().WheelPrecise;
            lastScroll = Mouse.GetState().WheelPrecise;
            if (changeInScroll < 0)
            {
                scaleModifier.X -= 0.2f;
                scaleModifier.Y -= 0.2f;
                scaleModifier.Z -= 0.2f;
            }
            if (changeInScroll > 0)
            {
                scaleModifier.X += 0.2f;
                scaleModifier.Y += 0.2f;
                scaleModifier.Z += 0.2f;
            }

            // general sigmoid function: 1/(1+e^-x)
            float Sigmoid(float x)
            {
                return 1 / (MathF.Pow(1 + MathF.E, -x));
            }

            // took the sigmoid function of the scaled modifier from the scroll wheel, so it is minimum 0 and max 1 with vast inputs
            Vector3 viewModSigmoid = new Vector3
            {
                X = Sigmoid(scaleModifier.X),
                Y = Sigmoid(scaleModifier.Y),
                Z = Sigmoid(scaleModifier.Z)
            };

            // get mouse info and use it to rotate
            MouseState cursorState = Mouse.GetCursorState();
            horizontalAngle -= 0.005f * (Width / 2 - cursorState.X);
            verticalAngle += 0.005f * (Height / 2 - cursorState.Y);
            Mouse.SetPosition(Width / 2, Height / 2);

            view = Matrix4.LookAt(new Vector3(5f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 0f));

            Matrix4 ScaleMatrix = Matrix4.CreateScale(viewModSigmoid);

            Matrix4 ZRotation;
            Matrix4.CreateRotationX(horizontalAngle, out ZRotation); //rotating in clockwise direction around x-axis

            Matrix4 YRotation;
            Matrix4.CreateRotationZ(verticalAngle, out YRotation); //rotating in clockwise direction around x-axis

            Matrix4 Rotation = Matrix4.Mult(ZRotation, YRotation);

            // apply transformations to camera
            view = Matrix4.Mult(Rotation, view);
            view = Matrix4.Mult(ScaleMatrix, view);

            // send these uniforms to the shader
            GL.UniformMatrix4(viewID, false, ref view);
            GL.Uniform1(intensityID, intensity);
            GL.Uniform1(partyModeID, partyMode);
        }


        void AttachNewPipe(Pipe originalPipe, ref Pipe newPipe)
        {
            Matrix4 pipeRoll = Matrix4.CreateRotationY(newPipe.pipeRollRadians);

            Matrix4 translationInOGPipeSpace = Matrix4.CreateTranslation(new Vector3(newPipe.torusRadius, 0f, 0f)) * originalPipe.pipeMatrix;

            Matrix4 translationInNewPipeSpace = Matrix4.CreateTranslation(new Vector3(-newPipe.torusRadius, 0f, 0f));

            Matrix4 torusRotation = Matrix4.CreateRotationZ(-originalPipe.subPipePercentage * 2 * MathF.PI);

            Matrix4 modelMatrix = torusRotation * translationInNewPipeSpace * pipeRoll * translationInOGPipeSpace;

            newPipe.pipeMatrix = modelMatrix;
        }

        void SetModelMatrix()
        {
            Pipe.PipeParams newPipeParams = new Pipe.PipeParams { pipeSegments = 50, pipeRadius = 20f, torusSegments = 15, torusRadius = 40, subPipePercentage = 0.4f };
            Pipe firstPipe = new Pipe() { pipeMatrix = Matrix4.Identity };

            //pipeQueue.Enqueue(new Pipe() { pipeMatrix = Matrix4.Identity });

            if (IsKeyDown(Key.Space))
            {
                RandomNumberGenerator.Create().GetNonZeroBytes(randomBytes);

                // queue size
                if (pipeQueue.Count > 5)
                {
                    pipeQueue.Dequeue();
                }

                Pipe newPipe = new Pipe(newPipeParams, randomBytes[0] / MathF.E, false);
                pipeQueue.Enqueue(newPipe);
                AttachNewPipe(currentPipe, ref newPipe);

                currentPipe = newPipe;
            }

            // draw all pipes
            if (pipeQueue.Count > 0)
            {
                Pipe pipeToDraw;

                for (int i = 0; i < pipeQueue.Count; i++)
                {
                    pipeToDraw = pipeQueue.ElementAt(i);

                    // checking it is not the empty init pipe
                    GL.UniformMatrix4(modelID, false, ref pipeToDraw.pipeMatrix);
                    if (pipeToDraw.subPipePercentage != 0.0f) pipeToDraw.DrawPipe(drawMode);
                }
            }
        }
    }
}



//angle_x_inc = 0.01f;

//model.Push(Matrix4.Identity);

//float torusRadius = 5f;
//float subPipePercentage = 0.4f;
//int numberOfPipes = 3;
//Vector4 TorusNormalVector = new Vector4(0f, 0f, 1f, 1f);
//Vector4 PipeForwardVector = new Vector4(0f, 1f, 0f, 1f);
//Vector4 PipeRotatePivot = new Vector4(torusRadius, 0f, 0f, 1f);
//Vector4 TorusNormalPivot = new Vector4(0f, 0f, 0f, 1f);
//for (int i = 0; i < numberOfPipes; i++)
//{

//    //Matrix4 Rotation = Matrix4.CreateRotationZ(2 * MathF.PI * (i / (float)numberOfPipes));
//    //rotating around z-axis 


//    //Rotation = Matrix4.Mult(Rotation, Matrix4.CreateFromAxisAngle(new Vector3(0f, 1f, 0f), MathF.PI/2));

//    //model.Push(Matrix4.Mult(model.Peek(), Translation));
//    //model.Push(Matrix4.Mult(model.Peek(), Rotation));

//    //Matrix4 modelTop = model.Pop();
//    //Matrix4 modelMatrix = modelTop;
//    //modelTop = model.Pop();

//    Matrix4 Translation = Matrix4.CreateTranslation(new Vector3(PipeRotatePivot) - new Vector3(TorusNormalPivot));
//    Matrix4 Translation2 = Matrix4.CreateTranslation(-(new Vector3(PipeRotatePivot) - new Vector3(TorusNormalPivot)));
//    Matrix4 RotationAroundTorusNormal = Matrix4.CreateFromAxisAngle(new Vector3(TorusNormalVector), 2 * MathF.PI * subPipePercentage * i);
//    Matrix4 RotationAroundPipeForward = Matrix4.CreateFromAxisAngle(new Vector3(PipeForwardVector), 0* MathF.PI/4);
//    //Matrix4 RotationAroundTorusNormal = Matrix4.Identity;

//    Matrix4 modelMatrix = Translation2 * RotationAroundTorusNormal * RotationAroundPipeForward  * Translation;
//    // send this uniform to the shader
//    PipeForwardVector *= modelMatrix;
//    TorusNormalVector *= modelMatrix;

//    PipeRotatePivot *= modelMatrix;
//    TorusNormalPivot *= modelMatrix;

//    Pipe donut = new Pipe(50, 2f, 100, torusRadius, subPipePercentage, false);

//    GL.UniformMatrix4(modelID, false, ref modelMatrix);
//    donut.DrawPipe(drawMode);


//}





//PipeData initPipeData = new PipeData { pipeMatrix = Matrix4.Identity, subPipePercentage = 0.0f };

//PipeData pipe1 = AttachNewPipe(initPipeData, newPipe, MathF.PI/8);
//PipeData pipe2 = AttachNewPipe(pipe1, newPipe, MathF.PI/8);
//PipeData pipe3 = AttachNewPipe(pipe2, newPipe, MathF.PI/8);




//modelTop = model.Peek();
//{
//    GL.UniformMatrix4(modelID, false, ref modelTop);
//    Pipe donut2 = new Pipe(50, 2f, 50, 4f, 0.05f, false);
//    donut2.DrawPipe(drawMode);
//}
//model.Pop();

//angle_x += angle_x_inc;
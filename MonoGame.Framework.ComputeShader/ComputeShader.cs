using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.ComputeShader.Internals;
using System;
using System.IO;
using System.Reflection;

namespace MonoGame.Framework.ComputeShader
{
    /// <summary>
    /// Compute shader implementation for OpenGL that plays nicely with MonoGame.
    /// Note that it is limited by MonoGames single-threaded concept so you will have to call it
    /// as part of the regular pipeline. Calls from a seperate thread are not supported.
    /// </summary>
    /// <example>
    /// var cs = new ComputeShader(..)
    ///
    /// // in your draw call:
    /// // do regular drawing
    ///
    /// // time for compute shader
    /// cs.Begin(rendertarget);
    /// cs.Execute(x, y, z):
    /// cs.End();
    ///
    /// // do some more regular drawing with the rendertarget
    /// </example>
    public class ComputeShader
    {
        private readonly uint _shader;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly uint _program;
        private readonly FieldInfo _glTextureInfo, _shaderProgramInfo;
        private FieldInfo? _programInfo;

        public ComputeShader(
            GraphicsDevice graphicsDevice,
            string shaderFilePath)
        {
            _graphicsDevice = graphicsDevice;
            _shader = LoadComputeShader(shaderFilePath);

            _glTextureInfo = typeof(RenderTarget2D).GetField("glTexture", BindingFlags.Instance | BindingFlags.NonPublic);
            _shaderProgramInfo = typeof(GraphicsDevice).GetField("_shaderProgram", BindingFlags.Instance | BindingFlags.NonPublic);

            _program = GL.CreateProgram();
            GL.AttachShader(_program, _shader);
            CheckGLError();
            GL.LinkProgram(_program);
            CheckGLError();
        }

        /// <summary>
        /// Loads the compute shader from file into memory, compiles and and returns the id of it on success.
        /// Else throws an exception.
        /// </summary>
        /// <param name="name"></param>
        private uint LoadComputeShader(string name)
        {
            var shader = GL.CreateShader(ShaderType.Compute);
            CheckGLError();
            GL.ShaderSource((int)shader, File.ReadAllText(name));
            CheckGLError();
            GL.CompileShader(shader);
            CheckGLError();
            var r = GL.GetShaderiv(shader, ShaderParameter.CompileStatus);
            CheckGLError();
            if (r != 1)
            {
                var status = GL.GetShaderInfoLog(shader);
                throw new InvalidDataException($"Failed to compile shader {name}. Reason: {status}");
            }
            return shader;
        }

        public void SetParameter(string name, Vector3 vec)
        {
            var p = GL.GetUniformLocation(_program, name);
            GL.SetUniform3f(p, vec.X, vec.Y, vec.Z);
            CheckGLError();
        }

        public void SetParameter(string name, int i)
        {
            var p = GL.GetUniformLocation(_program, name);
            GL.SetUniform1i(p, i);
            CheckGLError();
        }

        public void SetParameter(string name, float f)
        {
            var p = GL.GetUniformLocation(_program, name);
            GL.SetUniform1f(p, f);
            CheckGLError();
        }

        /// <summary>
        /// Must be called before execution.
        /// Sets up the rendertarget as the target of the compute shader.
        /// </summary>
        /// <param name="renderTarget"></param>
        public void Begin(RenderTarget2D renderTarget)
        {
            GL.UseProgram(_program);
            var tex = (uint)(int)_glTextureInfo.GetValue(renderTarget);
            // can't use SetRenderTarget as it uses glBindImage
            GL.BindImageTexture(0, tex, 0, false, 0, BufferAccess.WriteOnly, PixelInternalFormat.Rgba16);
        }

        /// <summary>
        /// Run the actual compute shader with the provided workgroups.
        /// </summary>
        public void Execute(uint threadGroupX, uint threadGroupY, uint threadGroupZ)
        {
            GL.DispatchCompute(threadGroupX, threadGroupY, threadGroupZ);
            // unbind image and wait for shader
            const uint GL_SHADER_IMAGE_ACCESS_BARRIER_BIT = 0x00000020;
            GL.MemoryBarrier(GL_SHADER_IMAGE_ACCESS_BARRIER_BIT);
            GL.BindImageTexture(0, 0, 0, false, 0, BufferAccess.ReadWrite, PixelInternalFormat.Rgba16);
        }

        /// <summary>
        /// Must be called after execute to return MonoGame to its original pipeline.
        /// </summary>
        public void End()
        {
            CheckGLError();

            // MonoGame has internal caches that we can't update
            // thus we must update the program or else MonoGame crashes
            // (it expects its program to still be active)
            var program = GetAtiveMonoGameProgram();
            GL.UseProgram(program);
            CheckGLError();
        }

        private uint GetAtiveMonoGameProgram()
        {
            var value = _shaderProgramInfo.GetValue(_graphicsDevice);
            if (value != null)
            {
                _programInfo ??= value.GetType().GetField("Program", BindingFlags.Instance | BindingFlags.Public);

                var activeProgram = (uint)(int)_programInfo.GetValue(value);
                return activeProgram;
            }
            return 0;
        }

        private void CheckGLError()
        {
            var error = GL.GetError();
            if (error != 0)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}

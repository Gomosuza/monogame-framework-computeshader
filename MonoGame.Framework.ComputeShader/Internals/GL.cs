using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MonoGame.Framework.ComputeShader.Internals
{
    /// <summary>
    /// GL entrypoints wrapped to idiomatic C#.
    /// Most would be accessible from MonoGames OpenGL class but theirs is internal..
    /// </summary>
    internal static class GL
    {
        private const CallingConvention CallConv = CallingConvention.Winapi;
        private static IntPtr NativeLibrary = GetNativeLibrary("libSDL2-2");

        /// <summary>
        /// Copied from: https://github.com/cra0zy/MonoGame/blob/dc44fc2ca8de754c9039dab6f7a71558c177aa69/MonoGame.Framework/SDL/SDL2.cs#L16
        /// </summary>
        /// <param name="library"></param>
        /// <returns></returns
        private static IntPtr GetNativeLibrary(string library)
        {
            var ret = IntPtr.Zero;

            // Load bundled library
            var assemblyLocation = Path.GetDirectoryName(typeof(GL).Assembly.Location);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.Is64BitProcess)
                ret = FuncLoader.LoadLibrary(Path.Combine(assemblyLocation, $"x64/{library}.dll"));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !Environment.Is64BitProcess)
                ret = FuncLoader.LoadLibrary(Path.Combine(assemblyLocation, $"x86/{library}.dll"));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Environment.Is64BitProcess)
                ret = FuncLoader.LoadLibrary(Path.Combine(assemblyLocation, $"x64/{library}.0.so.0"));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !Environment.Is64BitProcess)
                ret = FuncLoader.LoadLibrary(Path.Combine(assemblyLocation, $"x86/{library}.0.so.0"));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                ret = FuncLoader.LoadLibrary(Path.Combine(assemblyLocation, $"{library}.0.0.dylib"));

            // Load system library
            if (ret == IntPtr.Zero)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    ret = FuncLoader.LoadLibrary($"{library}.dll");
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    ret = FuncLoader.LoadLibrary($"{library}.0.so.0");
                else
                    ret = FuncLoader.LoadLibrary($"{library}.0.0.dylib");
            }

            // Try extra locations for Windows because of .NET Core rids
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var rid = Environment.Is64BitProcess ? "win-x64" : "win-x86";

                if (ret == IntPtr.Zero)
                    ret = FuncLoader.LoadLibrary(Path.Combine(assemblyLocation, "../../runtimes", rid, $"native/{library}.dll"));

                if (ret == IntPtr.Zero)
                    ret = FuncLoader.LoadLibrary(Path.Combine(assemblyLocation, "runtimes", rid, $"native/{library}.dll"));
            }

            // Welp, all failed, PANIC!!!
            if (ret == IntPtr.Zero)
                throw new Exception("Failed to load SDL library.");

            return ret;
        }

        [UnmanagedFunctionPointer(CallConv)]
        public delegate IntPtr d_sdl_gl_getprocaddress(string name);
        private static d_sdl_gl_getprocaddress GetProcAddress = FuncLoader.LoadFunction<d_sdl_gl_getprocaddress>(NativeLibrary, "SDL_GL_GetProcAddress");

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void UseProgramDelegate(uint program);
        private static UseProgramDelegate useProgram = LoadFunction<UseProgramDelegate>("glUseProgram");
        public static void UseProgram(uint program)
            => useProgram(program);

        [UnmanagedFunctionPointer(CallConv)]
        private delegate void DispatchComputeDelegate(uint num_groups_x, uint num_groups_y, uint num_groups_z);
        private static DispatchComputeDelegate dispatchCompute = LoadFunction<DispatchComputeDelegate>("glDispatchCompute");
        public static void DispatchCompute(uint num_groups_x, uint num_groups_y, uint num_groups_z)
            => dispatchCompute(num_groups_x, num_groups_y, num_groups_z);

        internal delegate int GetErrorDelegate();
        internal static GetErrorDelegate getError = LoadFunction<GetErrorDelegate>("glGetError");
        public static int GetError()
            => getError();

        [UnmanagedFunctionPointer(CallConv)]
        private delegate uint CreateProgramDelegate();
        private static CreateProgramDelegate createProgram = LoadFunction<CreateProgramDelegate>("glCreateProgram");
        public static uint CreateProgram()
            => createProgram();

        [UnmanagedFunctionPointer(CallConv)]
        private delegate void LinkProgramDelegate(uint program);
        private static LinkProgramDelegate linkProgram = LoadFunction<LinkProgramDelegate>("glLinkProgram");
        public static void LinkProgram(uint program)
            => linkProgram(program);

        [UnmanagedFunctionPointer(CallConv)]
        private delegate uint glCreateShaderDelegate(ShaderType shaderType);
        private static glCreateShaderDelegate createShader = LoadFunction<glCreateShaderDelegate>("glCreateShader");
        public static uint CreateShader(ShaderType shaderType)
            => createShader(shaderType);

        [UnmanagedFunctionPointer(CallConv, CharSet = CharSet.Ansi)]
        internal unsafe delegate void ShaderSourceDelegate(int shaderId, int count, IntPtr code, int* length);
        internal static ShaderSourceDelegate shaderSource = LoadFunction<ShaderSourceDelegate>("glShaderSource");
        internal unsafe static void ShaderSource(int shaderId, string code)
        {
            int length = code.Length;
            IntPtr intPtr = MarshalStringArrayToPtr(new string[] { code });
            shaderSource(shaderId, 1, intPtr, &length);
            FreeStringArrayPtr(intPtr, 1);
        }

        [UnmanagedFunctionPointer(CallConv)]
        private delegate void CompileShaderDelegate(uint shader);
        private static CompileShaderDelegate compileShader = LoadFunction<CompileShaderDelegate>("glCompileShader");
        public static void CompileShader(uint shader)
            => compileShader(shader);

        [UnmanagedFunctionPointer(CallConv)]
        private delegate uint GetShaderivDelegate(uint shader, ShaderParameter parameter, ref IntPtr result);
        private static GetShaderivDelegate getShaderiv = LoadFunction<GetShaderivDelegate>("glGetShaderiv");
        public static int GetShaderiv(uint shader, ShaderParameter parameter)
        {
            IntPtr result = IntPtr.Zero;
            getShaderiv(shader, parameter, ref result);
            return result.ToInt32();
        }

        [UnmanagedFunctionPointer(CallConv)]
        private delegate IntPtr GetShaderInfoLogDelegate(uint shader, int size, IntPtr length, StringBuilder log);
        private static GetShaderInfoLogDelegate getShaderInfoLog = LoadFunction<GetShaderInfoLogDelegate>("glGetShaderInfoLog");
        public static string GetShaderInfoLog(uint shader)
        {
            var length = GetShaderiv(shader, ShaderParameter.LogLength);
            var sb = new StringBuilder(length);
            getShaderInfoLog(shader, length, IntPtr.Zero, sb);
            return sb.ToString();
        }

        [UnmanagedFunctionPointer(CallConv)]
        private delegate void AttachShaderDelegate(uint program, uint shader);
        private static AttachShaderDelegate attachShader = LoadFunction<AttachShaderDelegate>("glAttachShader");
        public static void AttachShader(uint program, uint shader)
            => attachShader(program, shader);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void MemoryBarrierDelegate(uint id);
        public static MemoryBarrierDelegate memoryBarrier = LoadFunction<MemoryBarrierDelegate>("glMemoryBarrier");
        public static void MemoryBarrier(uint id)
            => memoryBarrier(id);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void BindImageTextureDelegate(uint unit, uint texture, uint level, bool layered, uint layer, BufferAccess access, PixelInternalFormat format);
        private static BindImageTextureDelegate bindImageTexture = LoadFunction<BindImageTextureDelegate>("glBindImageTexture");
        public static void BindImageTexture(uint unit, uint texture, uint level, bool layered, uint layer, BufferAccess access, PixelInternalFormat format)
            => bindImageTexture(unit, texture, level, layered, layer, access, format);

        [UnmanagedFunctionPointer(CallConv, CharSet = CharSet.Ansi)]
        public delegate uint GetUniformLocationDelegate(uint program, string name);
        private static GetUniformLocationDelegate getUniformLocation = LoadFunction<GetUniformLocationDelegate>("glGetUniformLocation");
        public static uint GetUniformLocation(uint program, string name)
            => getUniformLocation(program, name);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void SetUniform3fDelegate(uint parameterId, float x, float y, float z);
        private static SetUniform3fDelegate setUniform3f = LoadFunction<SetUniform3fDelegate>("glUniform3f");
        public static void SetUniform3f(uint parameterId, float x, float y, float z)
            => setUniform3f(parameterId, x, y, z);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void SetUniform1iDelegate(uint parameterId, int i);
        private static SetUniform1iDelegate setUniform1i = LoadFunction<SetUniform1iDelegate>("glUniform1i");
        public static void SetUniform1i(uint parameterId, int i)
            => setUniform1i(parameterId, i);

        [UnmanagedFunctionPointer(CallConv)]
        public delegate void SetUniform1fDelegate(uint parameterId, float f);
        private static SetUniform1fDelegate setUniform1f = LoadFunction<SetUniform1fDelegate>("glUniform1f");
        public static void SetUniform1f(uint parameterId, float f)
            => setUniform1f(parameterId, f);

        private static T LoadFunction<T>(string function)
        {
            var ret = GetProcAddress(function);
            if (ret == IntPtr.Zero)
                throw new NotImplementedException($"{function} not found");
            return Marshal.GetDelegateForFunctionPointer<T>(ret);
        }

        private static IntPtr MarshalStringArrayToPtr(string[] strings)
        {
            IntPtr intPtr = IntPtr.Zero;
            if (strings != null && strings.Length != 0)
            {
                intPtr = Marshal.AllocHGlobal(strings.Length * IntPtr.Size);
                if (intPtr == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }
                int i = 0;
                try
                {
                    for (i = 0; i < strings.Length; i++)
                    {
                        IntPtr val = MarshalStringToPtr(strings[i]);
                        Marshal.WriteIntPtr(intPtr, i * IntPtr.Size, val);
                    }
                }
                catch (OutOfMemoryException)
                {
                    for (i--; i >= 0; i--)
                    {
                        Marshal.FreeHGlobal(Marshal.ReadIntPtr(intPtr, i * IntPtr.Size));
                    }
                    Marshal.FreeHGlobal(intPtr);
                    throw;
                }
            }
            return intPtr;
        }

        private static unsafe IntPtr MarshalStringToPtr(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return IntPtr.Zero;
            }
            int num = Encoding.ASCII.GetMaxByteCount(str.Length) + 1;
            IntPtr intPtr = Marshal.AllocHGlobal(num);
            if (intPtr == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }
            fixed (char* chars = str + RuntimeHelpers.OffsetToStringData / 2)
            {
                int bytes = Encoding.ASCII.GetBytes(chars, str.Length, (byte*)((void*)intPtr), num);
                Marshal.WriteByte(intPtr, bytes, 0);
                return intPtr;
            }
        }

        private static void FreeStringArrayPtr(IntPtr ptr, int length)
        {
            for (int i = 0; i < length; i++)
            {
                Marshal.FreeHGlobal(Marshal.ReadIntPtr(ptr, i * IntPtr.Size));
            }
            Marshal.FreeHGlobal(ptr);
        }
    }
}

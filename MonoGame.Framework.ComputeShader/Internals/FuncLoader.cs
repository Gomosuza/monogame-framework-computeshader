using System;
using System.Runtime.InteropServices;

namespace MonoGame.Framework.ComputeShader.Internals
{
    internal static class FuncLoader
    {
        private static class Windows
        {
            [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibraryW(string lpszLib);
        }

        private static class Linux
        {
            [DllImport("libdl.so.2")]
            public static extern IntPtr dlopen(string path, int flags);

            [DllImport("libdl.so.2")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
        }

        private static class OSX
        {
            [DllImport("/usr/lib/libSystem.dylib")]
            public static extern IntPtr dlopen(string path, int flags);

            [DllImport("/usr/lib/libSystem.dylib")]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
        }

        private const int RTLD_LAZY = 0x0001;

        public static IntPtr LoadLibrary(string libname)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Windows.LoadLibraryW(libname);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OSX.dlopen(libname, RTLD_LAZY);

            return Linux.dlopen(libname, RTLD_LAZY);
        }

        public static T LoadFunction<T>(IntPtr library, string function)
        {
            IntPtr ret;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                ret = Windows.GetProcAddress(library, function);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                ret = OSX.dlsym(library, function);
            else
                ret = Linux.dlsym(library, function);

            if (ret == IntPtr.Zero)
                throw new EntryPointNotFoundException(function);

            return Marshal.GetDelegateForFunctionPointer<T>(ret);
        }
    }
}

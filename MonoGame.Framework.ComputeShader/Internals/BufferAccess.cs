namespace MonoGame.Framework.ComputeShader.Internals
{
    /// <summary>
    /// Copied and extended from: https://github.com/cra0zy/MonoGame/blob/dc44fc2ca8de754c9039dab6f7a71558c177aa69/MonoGame.Framework/Graphics/OpenGL.cs#L18
    /// </summary>
    internal enum BufferAccess
    {
        ReadOnly = 0x88B8,
        WriteOnly = 0x88B9,
        ReadWrite = 0x88BA
    }
}

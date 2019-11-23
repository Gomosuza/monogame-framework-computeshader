# OpenGL Compute shader extensions for MonoGame

:warning: Please note that I only implemented parts of the OpenGL compute shader api (specifically those used by my [Raytracer](https://github.com/MarcStan/raytracer)).

:warning: Also note that I based it on the (slightly*) unofficial [MonoGame Core](https://github.com/MonoGame/MonoGame/issues/5339#issuecomment-287556964) fork to make it .Net Standard compliant (done so the raytracer can run on .Net Core 3).

(\* Slightly unofficial because it was done by one of MonoGames core contributors on his own)

:warning: As mentioned in the title this is a **OpenGL implementation only** (DirectX is not supported outside windows). For an idea how it could look like, check out this [rejected PR](https://github.com/MonoGame/MonoGame/pull/2100).

Beyond those warnings feel free to use the extension.

# Working demo

For a working implementation see the [Raytracer backend](https://github.com/MarcStan/raytracer/blob/master/Raytracer/Backends/ComputeShaderRaytracer.cs) and respective [shader code](https://github.com/MarcStan/raytracer/blob/master/Raytracer/Shaders/raytracer.glslcs).


# Known issues

* Some integrated GPUs (such as Intel Graphics 620) silently fail to run the shader and do not output anything on screen
* Instead of not showing anything the mobile chipsets might also throw an AccessViolationException on the linkProgram call (in-memory compile step of glsl shader code)

# Usage

Setup in MonoGame is trivial:

``` csharp
// file extension is irrelevant I use glslcs for "glsl compute shader"
var shader = new ComputeShader(graphicsDevice, "myShader.glslcs");
```
Using the shader as part of a draw call is similar to spritebatch calls:

``` csharp
// to use you must render into a rendertarget
shader.Begin(renderTarget);
// arbitrary shader parameters can be set from the outside
shader.SetParameter("eye", camera.Position);
shader.SetParameter("time", (float)gameTime.TotalGameTime.TotalSeconds);
// dispatch in compute space (in this example the space is 2D so the z component is set to 1)
// division by 8 is hardcoded in shader as well
// see https://www.khronos.org/opengl/wiki/Compute_Shader#Local_size for details
shader.Execute(x / 8, y / 8, 1);
shader.End();
// rendertarget is now finished and can be consumed in further code
```

Simple Shader example:

``` glsl
#version 430 core

layout(binding = 0, rgba32f) uniform writeonly image2D framebuffer;
uniform float time;
uniform vec3 eye;

layout(local_size_x = 8, local_size_y = 8) in;
void main()
{
  // most basic compute shader. Can be used to verify that everything is set up correctly
  // on execute the shader will fill the entire buffer with a single color

  ivec2 pos = ivec2(gl_GlobalInvocationID.xy);
  ivec2 size = imageSize(framebuffer);
  // discard points outside texture to prevent computing unnecessary pixels
  if (pos.x >= size.x || pos.y >= size.y)
  {
    return;
  }

  // calculation taken from https://shadertoy.com/new
  // Normalized pixel coordinates (from 0 to 1)
  vec2 uv = pos/size;
  // Time varying pixel color
  vec3 col = 0.5 + 0.5 * cos(time + uv.xyx + vec3(0,2,4));
  imageStore(framebuffer, pos, col);
}

```

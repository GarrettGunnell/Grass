# Grass

by Garrett Gunnell

An exploration of grass rendering techniques.

DISCLAIMER: While this grass is highly performant and optimized, please don't attempt to use it yourself as it is tuned to my needs. Instead, reference it as much as you'd like. I apologize for the mess.

# Features

* GPU Instancing
* GPU Frustum Culling
* Chunked grass position buffers to cover any desired area
* Level of detail applied to distant chunks
* Scrolling noise texture to simulate wind
* Shaders for quad grass and individual grass blade 3d models
* Geometry Shader Grass

## Quad Grass

Implementation details [here](https://www.youtube.com/watch?v=Y0Ko0kvwfgA).

![example](./example.png)

## 3D Model Grass

Implementation details [here](https://youtu.be/jw00MbIJcrk).

![example2](./example2.png)

## GPU Frustum Culling

![example3](./example3.png)

## Chunking

![example4](./example4.png)

## Level Of Detail

![example5](./example5.png)

## Geometry Shader Grass

(don't reference the code for this or use it)

![example6](./example6.png)
# Grass

by Garrett Gunnell

An exploration of grass rendering techniques.

DISCLAIMER: This code is far from production ready, please do not attempt to use it as it is highly tuned to my specific needs.

# Features

* GPU Instancing
* GPU Frustum Culling
* Chunked grass position buffers to cover any desired area
* Level of detail applied to distant chunks
* Scrolling noise texture to simulate wind
* Shaders for quad grass and individual grass blade 3d models

# TO DO:

* Geometry shader grass

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
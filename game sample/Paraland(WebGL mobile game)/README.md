## Project Name
>**Paraland**

## Software used or media
This example was developed in the **Unity 2022.3.34** environment. It can be exported to the WebGL platform to run on PC or mobile browsers, and can also be exported as an Android apk application.
The main game scene is: \Assets\Scenes\SampleScene.unity

## Role in the Project

>**Solo project**

## Date of Completion

>**Jun 6th/2024**

## Game Demo Video

 [Game Demo Video](https://a.unity.cn/client_api/v1/buckets/24b1eaa0-ff82-4c2e-8cb8-34692c4d352c/content/paraland.mp4)

## WebGL Game Playable Demo
 [WebGL Game demo page](https://huboyuan2.github.io/paralanddemo/)

## Game Design
>The birth of the game originated from a random thought: would it be interesting to combine the fixed-value RPG gameplay of "Magic Tower" with the optical illusion puzzle gameplay similar to "FEZ" or "Monument Valley"? However, The ultimate aesthetic pursuit of "impossible architecture" and "impossible geometric figures" in "Monument Valley" is beyond my reach, so I decided to change my design approach, allowing players to rotate the   camera perspective and designed the map as a sufficiently three-dimensional Metroidvania-style 3D sandbox map where each area is connected at specific perspective. Every time the player rotates the camera perspective, they will find that originally non-adjacent or even distant blocks are connected in this perspective, forming many shortcuts, thus enjoying the fun of exploration, while planning the optimal monster-fighting and leveling-up route in the fixed-value RPG gameplay similar to "Magic Tower" to gain the maximum benefit. The organic combination of the two gameplay styles brings players the fun of 1+1>2.

## Technical Challenges
>1. Pathfinding algorithm considering optical illusion gameplay in isometric perspective. Since this game needs to achieve "blocks that appear adjacent in a specific perspective can be reached", the built-in NavMesh pathfinding and original A Star pathfinding algorithm of the Unity engine cannot meet the game requirements, so I need to write my own pathfinding algorithm. The pathfinding algorithm I wrote generally follows the A Star approach. Each block corresponds to a pathfinding node. Each node needs to calculate and sort the Manhattan distance to the start and end points, storing the node with the smallest distance in the closed set, and the remaining nodes in the open set, and backtracking based on the open and closed sets when hitting a dead end. Particularly, due to the involvement of stairs, railings, and other complex situations, this game does not serialize the arrangement of blocks in the four perspectives in advance, but uses raycasting to determine the visually adjacent blocks in the current perspective during each recursive pathfinding. The advantage of this approach is that it can analyze specific problems when encountering stairs, railings, and block occlusion relationships that may cause visual glitches. Additionally, due to the large amount of optical illusion gameplay in this game, we also use some "tricks" to avoid visual glitches during player movement, such as secretly teleporting the player to a point corresponding to the same screen position but at a different distance from the camera.

2.  re-serializing the game level, modularizing the game block elements, achieving JSON serialization of level content, providing a foundation for future level editors.

3. Other systems such as dialogue, items, combat，save&load are relatively casual in implementation, with all configuration tables using ScriptableObject for quick editing in the Unity engine.

4. Graphics Rendering:
To create the game atmosphere, we plan to implement a height fog effect where "the lower the altitude, the denser the fog". In this project, we implemented two height fog solutions. The first solution is to add a CommandBuffer for full-screen fog rendering between the opaque and transparent rendering queues, using a shader to perform a series of complex matrix operations based on the depth map to restore the world space coordinates of each pixel, and calculate the fog density based on the y-axis height of each pixel's world space coordinates. The advantage of this solution is that it can also implement other effects related to world space coordinates, such as scanning effects, but the disadvantage is that the GPU computation is relatively large, making it a bit extravagant for mobile devices. The second solution is to place a large horizontal plane in the scene, treating it as the upper surface of the height fog area, setting its rendering queue to the transparent queue. In the shader of this plane, the depth difference between the plane and the model behind it taken from the depth map is calculated, and the current pixel transparency of the plane is set based on the calculation result. Since the plane is horizontal and the camera is tilted downward, the lower the model below the plane, the greater the difference, thus achieving the effect of "the lower the altitude, the denser the fog".


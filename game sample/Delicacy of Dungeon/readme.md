## Project Name
>**Delicacy of Dungeon**

## Software used or media
This game was developed in the **Unity2019.4 ** environment.

 It can only be exported to the **PC** Platform


## Role in the Project

>Technical artist(shader), level designer, programmer(UI system and character controller)

## Date of Completion

>**April 23th/2021**

## Game Demo Video

 [Game Demo Video](https://a.unity.cn/client_api/v1/buckets/24b1eaa0-ff82-4c2e-8cb8-34692c4d352c/content/DODvideo.mp4)

## Game Download Link(PC)
 [PC Game Download Link](https://a.unity.cn/client_api/v1/buckets/24b1eaa0-ff82-4c2e-8cb8-34692c4d352c/content/DOD.zip)

## Game Design
>**Delicacy of Dungeon** is a dungeon crawling action game with a cooking theme. Players take on the role of a Norse Valkyrie who has lost her divinity and serves as the head chef in a restaurant called Hvergelmir located in Niflheim. Her daily job is to hunt monsters in the dungeon directly below the restaurant, cleverly using weapons and traps to turn the monsters into delicious dishes, and then serve the dishes to the restaurant's patrons.

## Technical Challenges(Technical Art Part)
>**Goal:** Achieve a **cartoon** rendering with a certain degree of **realism**
>
>**Principles:** create shaders to achieve customized effects, minimize the use of ready-made plugins, avoid flashy techniques, avoid feature bloat, do not overly pursue physical realism but focus on overall effect harmonious, pursue performance optimization, and pursue development efficiency
>
>**Technical Selection:**  unity **Built-in** render pipeline, forward rendering, **Amplified Shader Editor**
>
>### shader features and effects
>
>1. Use **ramp textures** to layer lighting to achieve cartoon-style light and dark boundaries, and use interpolation between lambert and half-lambert diffuse reflections to facilitate artists in adjusting the position of light and dark boundaries. Use normal maps to make the light and dark boundaries more intricate
>
>2. Construct **TBN** (Tangent-Bitangent-Normal) matrix to achieve the conversion from tangent space to world space, thereby achieving tangent space **normal maps**
>
>3. Use **Blinn-Phong** specular for the highlight part and normalize it to facilitate achieving smoothness closer to the PBR standard
>
>4. Use a "dynamic-static separation" scheme for ambient light. Dynamic objects sample light probes as ambient light, while static objects use lightmaps as ambient light
>
>5. Add **metallic** mask maps
>
>   The higher the metallicity of objects, the weaker the diffuse, the stronger the specular and cubemap reflection (sampling reflection probes), and the stronger the correlation between the reflection color and albedo. The smoother the non-metallic material, the stronger the Fresnel effect
>
>6. Cartoon-style **outlines**: Add a second shader pass that **expands vertices along the normal direction** in the vertex shader stage and culls the front faces, rendering only the back faces
>
>   The outline pass for the main character is special. By changing the Ztest (depth test) mode to "Always", the outline pass of the main character can be displayed even when occluded
>
>7. Burning & dissolve effect: based on Alpha clip, noise map, and Emission
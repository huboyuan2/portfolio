# **RASS Technical Design Document**

# 

### **PREPARED FOR**

GAM541

### **PREPARED BY**

Team Nutmeg

[**1\. Introduction and Overview	3**](#1.-introduction-and-overview)

[**2\. System Architecture	4**](#2.-system-architecture)

[**3\. Core Subsystems and Components	7**](#3.-core-subsystems-and-components)

[3.1 Rendering	7](#3.1-rendering)

[**4\. Interfaces and APIs	9**](#4.-interfaces-and-apis)

[**5\. GamePlay Interface	13**](#5.-gameplay-interface)

[**6\. Implementation Considerations	13**](#6.-implementation-considerations)

[5.1 Performance	14](#5.1-performance)

[5.2 Testing	14](#5.2-testing)

[5.3 Build System	14](#5.3-build-system)

[5.4 Version Control	14](#5.4-version-control)

[5.5 Risks	15](#5.5-risks)

# **1\. Introduction and Overview** {#1.-introduction-and-overview}

ProjectRASS is a 2D game engine built in C++ for Windows 11, using OpenGL for rendering, FMOD for audio, and GLFW for input handling.  The engine is specialized for 2D side-scrolling, action-platformer genre: features such as basic animations, audio playback, input handling, JSON serialization, and basic physics are supported.  Furthermore, the engine will perform at around 30 FPS on mid-range hardware.  As the engine is built for a student project within a 15-week period, it will *not* support larger features, such as 3D graphics, multiplayer support, robust physics, and AI framework.

In short, ProjectRASS is a 2D game engine specializing in 2D side-scrolling, action platformers.  Its data-driven design makes it easy to get the graphics, audio, and physics running and started for a game running on a mid-range Windows computer.  The tech that runs the engine includes C++, OpenGL, FMOD, and GLFW.

# **2\. System Architecture** {#2.-system-architecture}

The ProjectRASS borrows heavily from the Unity game engine’s GameObject-Components software architecture: Entities (the engine’s GameObjects) holds a collection of specialized data units, Components; Scenes holds a collection of Entities; and specialized Systems handles the actions, such as events management, rendering, audio playback, physics simulation, and more.  When starting or switching a level, a Scene gets loaded, which in turn loads its collection of Entities, and finally, the Entities loads its Components.  Components hold a reference to their parent Entity, and Entities in turn allow Components to retrieve another Component, to read its data.  This data-driven approach is well-tested, easy to prototype in, and provides the necessary flexibility for the upcoming game prototypes.



Entities are created through the `EntityFactory`, which Scenes use to construct new Entities.  Scenes contain an `EntityContainer`, which pre-allocates a large number of Entities to reduce the need to allocate new Entities.  More importantly, the container uses the Flyweight design pattern: when a Component or Scene calls `EntityContainer::Build(name)`, it first reads the JSON and caches the resulting Entity into a filename-to-Entity map, then clones this cached Entity. That cached Entity is then added to a list of Archetypes to save on future redundant build costs for duplicate Entities.  `IResourceSystem` follows a similar design pattern, but with binary assets.  Furthermore, rather than cloning a binary asset, it simply returns a pointer to the loaded asset; obviously to save memory.

The `SystemsManager` uses Dependency Injection whenever a System is registered to it.  Scenes, Entities, and Components only have access to the interfaces of each System, thus allowing different configurations – such as unit tests – to swap out the concrete implementation of each System seamlessly.  For convenience, `SystemsManager` also sets (and un-sets) the static instances of any System extending `IGlobalSystem`.  Whenever a Scene, Entity, or Component communicates to a System, it is through this `IGlobalSystem::Get()` static function, which of course returns the static instance the `SystemsManager` has set.

To make it difficult to actually change the content of `SystemsManager`, once built and the game engine is running, a builder pattern (`SystemsManagerBuilder`) is used so one can register Systems to it.  Once is `SystemsManagerBuilder::build()` is called, though, one loses the access to add or remove any systems from the `SystemsManager`.  This provides some security and assurance on what Systems will be available to the rest of the code on runtime.

The `Engine` contains an instance of the `SystemsManager`, and runs the game loop by calling various events and functions in Systems registered to the manager.  In particular: the engine calls events in `IGlobalEventsSystem` every frame, as well as methods from `IRenderSystem` to render, and methods from `ITimeSystem` to lock the framerate.  Other Systems and Components, then, bind to events in the `IGlobalEventsSystem`, to receive a notification on when to update themselves.

Incidentally, the `IGlobalEventsSystem` uses the Observer pattern: it stores a map where the `GlobalEventID`s (which is just a static unique ID number, or `UUID`) are the keys, and a set of `IGlobalEventListener` and `IGlobalEventListenerLambda` are the values. As described in the [Game Loop](#heading=h.fyevdgj4bopv) section, the `IGlobalEventsSystem` is the primary method to receive a notification on frame update, especially for Systems and Components.

# **3\. Core Subsystems and Components** {#3.-core-subsystems-and-components}

## 3.1 Rendering {#3.1-rendering}

Shader class: shader compile and link.

Mesh class: manage VAO\&VBO, generate quad mesh or n\*n text mesh.

Texture class: use STB Image, manage gl texture binding.

RenderSystemOpenGL: support Render queue\&Render Layer depth sort, depth write depth test (for game scene with more complex depth relationship). In Rass engine, transform position is a vec3, z value is for sorting the render order in the same render layer (for opaque object from back to front, for transparent object from front to back).

Renderable struct (contains transform matrix, render layer, tint color, tiling\&offset(for sprite sheets) texture ptr, and character UV offsets for text rendering).

Simple Sprite Shader(support text batch rendering) alpha blend or clip, color tint, tilling\&offset.

Sprite::Render generate renderable by the data from transform, animation, sprite and text, and submit the renderable to render queue.

Flipbook Animation: use frame index to calculate uv offset for sprite sheet, advance frame in Update.

transform\&cam Matrix(Model View Projection) ,parent transform implemented (prepared for 2d front kinematic skeleton Animation).

Text rendering: use prestored mesh n\*n, use cpu to calculate uv for each character in the string, and vertex shader use the uv offsets.

Camera system: key point is setting view matrix, support camera follow.

Debug Drawing: based on Imgui, draw the debug ui check boxes, traverse each entities in current scene and draw their transform  axis, collider box.

| Subsystem | Responsibilities | Key Classes | Dependencies |
| :---- | ----- | ----- | ----- |
| Rendering | Draw sprites, handle shaders | Renderer, SpriteBatch | GLM,glbinding,SYSTB Image |
| Debug Rendering | Provides debug view of physics and graphics information, as well as performance specs | DebugDrawerSystem | Imgui |
| Physics | Simulate movement, detect collisions | ResolveCollisions IntegrateBodies | None (custom impl) |
| Audio | Manage sound and music | StreamMusic, PlaySFX | FMOD |
| Input | Receive and handle input | isKeyPressed, isKeyDown | GLFW |
| Game Loop | Scene initialization, updates at variable intervals, updates at pseudo-fixed intervals (yes, we support both,) render, lock framerate, scene cleanup. | Engine | None (custom impl) |
| Serialization | Traverses the files tree-style and passes the data onto the desired fields.  | Stream, ISerialization\<Stream\> | nlohmann library |
| UI | Draws buttons, sliders, and other menu-based interactions | UISystem, PauseMenuSystem,OptionsMenuSystem | Imgui |
| Components | Provides easy interface to add new gameplay elements | ComponentsFactory | None (custom impl) |

## 3.2 TileMap

**Architecture and Rendering:** 

The Breakable Tilemap is an efficient, data-driven rendering and collision system utilizing a single precalculated static mesh (Mesh::BuildStaticGrid) and shader-based pixel discarding. Tile states are encoded into a mask texture (TileMap::InitTileStateTexture), enabling dynamic destruction visuals without CPU-side mesh updates or multiple draw calls

**Physics and Collision:** 

The tile physics system is designed to collaborate with standard box collisions. It  uses Binary Collision mapped to four hot spots on standard box colliders. Collisions are resolved through axis separation and world-to-tile space transformations; if a hot spot enters a Tile Collision area, the object is reverted to its previous frame's position. It inherently supports flipped level scaling (y \= \-1).

**Level Logic and Tooling:** 

Tilemaps natively support mechanisms like interactable doors and checkpoints. Levels are authored in "Tiled", exported as .tmj files, and converted to the engine's standard JSON format using a custom Python script, streamlining the level design pipeline.

# **4\. Interfaces and APIs** {#4.-interfaces-and-apis}

To utilize ProjectRASS, one starts by registering Systems into the `SystemsManager`, through the `SystemsManagerBuilder`.  Once finished, one can then construct the `Engine` object with the built `SystemsManager` instance, which manages all the game flow and data.  To confirm the setup succeeded, it’s recommended to call `Engine::canRun()`, which returns true if all the necessary Systems are registered.  Finally, call `Engine::run()` to run the full game loop.

To add new features, either implement a System by extending the `IGlobalSystem<T>` for globally-acting features, and/or implement Components by extending `Component` for individual data.  To bind to global events (described in more details in the next paragraph,) create a `GlobalEventListener` or `GlobalEventListenerLambda` functor and bind it to an event via `IGlobalEventsSystem::Get()->bind(GlobalEventID, IEventListener<GlobalEventArgs>)`.  Developers can also optionally add a third argument, `CallFrequency`, to indicate whether the event should only trigger once before being unbinded automatically, or to remain binded event after the event has been triggered.  To clean-up this binding, don’t forget to call `IGlobalEventsSystem::Get()->unbind(GlobalEventID, IEventListener<GlobalEventArgs>)` in, say, the destructor of Component or `::Shutdown()` of a System.  Components extending `NewComponent : Cloneable<Component, NewComponent> {}` must define `string_view ::NameClass()`, and `bool ::Initialize()` returning true (success.)  Finally, a copy constructor, e.g. `NewComponent(const NewComponent&)`, and `bool ::Read()` returning true (success,) is required to be defined to make sure the factory operates correctly.

Systems are required to register to a `SystemsManagerBuilder`.  This can be done with `builder->register<ISystemInterface, SystemConcrete>()`.  As the type interfaces imply, the former must be an abstract class, while the latter needs to be a derivative class of the abstract class.  As such, to create a new System, first create an abstract class, e.g. `INewSystem : IGlobalSystem<INewSystem> {}`.  Then create a new concrete class extending that abstract class, with at least `bool ::Initialize()` returning true (success,) and `void ::Shutdown()` defined.  Once registered, the new system can be accessed via `INewSystem* INewSystem::Get()`.

The `Engine::run()` calls events through `IGlobalEventsSystem`: the event system that Systems, Scenes, and Components binds to (with a `GlobalEventListener` or `GlobalEventListenerLambda` functor) to get regular event calls.  `IGlobalEventsSystem` calls events through a key-system-pairing: for example, when one binds to an event under the `Events::Global::Update` key, that bounded function will be called once every frame.  If one needs to quickly add a new global event, they can do so by creating a new `GlobalEventID` instance, and calling `IGlobalEventsSystem::Get()->call(newGlobalEventID, args)`.  For the full list of existing events, the `Engine` calls the following, in order, each frame:

* `Events::Global::FixedUpdate` \- Called at a fixed interval, which by default is 0.02 seconds. In reality, this event is called as many times as necessary within a frame until its fixed duration “catches up” to the number of seconds that has passed last frame, to give the illusion that the duration is fixed.  Typically used by physics-related Systems and Components.  
* `Events::Global::Update` \- Called each frame, right after the last `Events::Global::FixedUpdate` that was necessary for the former to “catch up.”  This is the most common event Components are binded to.  
  * `Events::Update::After` \- Called right after `Events::Global::Update`, assuming it returned true.  Used for cleaning up Entities marked as destroyed, and their components.  
* `Events::Global::Render` \- Called after `IRenderSystem::BeginFrame()`, but before `IRenderSystem::DrawRenderables()` and `IRenderSystem::EndRender()`.  Used by graphical Components to queue themselves into the `IRenderSystem`.

At the end of all updates called above, `Engine` then calls `ITimeSystem::Get()->EndFrame()` to lock the framerate, and calculate the last frame’s delta time.  If the game loop gets interrupted by a bad event call (i.e. an event returns false,) or `ISceneSystem::Get()->IsRunning()` returns false, the `Engine` also calls the following special events:

* `Events::Global::Quit` \- called before the `Engine` is about to end its `::run()` method.	

In addition, `ISceneSystem` also adds a few global events to `IGlobalEventsSystem`, largely out of convenience (e.g., it’s difficult to bind to the first event without `IGlobalEventsSystem`:)

* `Events::SceneChange`  
  * `::AfterInitialize` \- Called after `Scene::Initialize()` is called, but before the next `Events::Global::FixedUpdate` and `Events::Global::Update`.  Useful for Components to run any setup after their `::Initialize()` method.  
  * `::BeforeShutdown` \- Called before `Scene::Shutdown()` is called.  Used by Systems referencing Entities and Components, like `IPhysicsSystems`, to clean themselves up.  
  * `::AfterShutdown` \- Called after `Scene::Shutdown()` is called.  Used by memory allocators like `IResourceSystem` to clean themselves up before the next scene initializes.

To change scenes, just call `ISceneSystem::Get()->SetPendingScene<NewScene>()`.  To implement a scene, extend the `Scene` class, and implement `::Initialize()`.

Scenes also provides an ability to search for a specific entity that has been loaded in the current scene.  To do so, the developer will need either the name or the UUID of the entity.  Then they can call `ISceneSystem::Get()->FindEntity(const std::string_view &name)` or `ISceneSystem::Get()->FindEntity(const UUID &id)` respectively to find the desired entity.  If a new entity is created, it must be added to the currently-active scene so its memory will be managed.  This can be called through `ISceneSystem::Get()->AddEntity(std::unique_ptr<Entity> &&)`.  This also makes the added entity searchable.

Finally, Entities themselves have an Event system not unlike `IGlobalSystem`.  By calling `Entity::DispatchEntityEvent(const EntityEventID& id, const EventArgs& args)`, a component can notify other components attached to the same entity any event that has triggered, thus allowing them to update themselves.  For components to listen to an entity event, they can simply call `BindEvent(const Events::EntityEventID &id, IEventListener<Events::EventArgs> *listener)` with an existing function pointer (e.g., a member method.)  Naturally, Un`bindEvent(const Events::EntityEventID &id, IEventListener<Events::EventArgs> *listener)` will reverse this method.

# **5\. GamePlay Interface** {#5.-gameplay-interface}

The above game engine architecture is compiled into a static library that is utilized by the `GameRass` project.  This provides the game engine engineer to focus on the feature implementation, while the gameplay engineers focus on the in-game features.  On the gameplay end of things, the `ComponentFactory` system plays a significant role: any time the gameplay engineer implements a gameplay-specific component, they need to be added to the `ComponentFactory` so that the serializer can register and parse their information.

Below is a brief introduction of components written specifically for the `GameRass` project, thus holding specific gameplay roles:

* `FlipOrigin.h` and `Flippable.h`:  
  * The former component performs the flip animation where the player is standing.  `FlipOrigin` will only flip Entities with the `Flippable` component attached.  
* `Switch.h` and `Door.h`:   
  * Handles the behavior of switches and locked doors.  They demonstrated how doors can search for switch entities through the use of UUIDs.  
* `QuitOnClick.h`, et. al.  
  * UI components.  These buttons listen to the entity’s event system, and detect if any component on said entity triggered the button click event.  Useful as reference for how the Entity event system works.

# **6\. Implementation Considerations** {#6.-implementation-considerations}

The primary driving force behind many implementations deals with the familiarity people have with each tech, thus reducing the training period.

## 6.1 Performance {#5.1-performance}

A simple frame-rate tracker is used to track the game’s real-time performance in `ITimeSystem`.  ImGUI also provides its own FPS counter, which corroborates results from `ITimeSystem`.

## 6.2 Testing {#5.2-testing}

GoogleTest C++ Unit Testing framework is used, through NuGet, to provide a unit testing framework to confirm the stability of ProjectRASS.  Github Actions is also used to run said unit tests under each PR, to test their stability. 

## 6.3 Build System {#5.3-build-system}

ProjectRASS is primarily built in Visual Studio 2022\.  It is a common and stable IDE with a strong history.  Some dependencies include:

* **STD** \- the standard C++ library  
* **GLM** \- for OpenGL rendering  
* **GLFW** \- input and window management  
* **SPDLog** \- for logging support  
* **Nholman JSON** \- for JSON serialization/deserialization  
* **IMGUI** \- for debug rendering   
* **FMOD** \- audio management

Github Actions is also used as a Continuous Integration server to make sure each PR correctly builds.

## 6.4 Version Control {#5.4-version-control}

Git and Github are used as our primary version control.  The familiarity of the SCM to each team member, as well as its stability and popularity to the rest of the world, makes it an ideal choice as a collaboration tool for ProjectRass.  Some risks include poor support of binary files.  As this project is primarily focused on the game engine itself, however, it’s unlikely the number of binary files will be very large.

The primary branching strategy is that of [Github Flow](https://docs.github.com/en/get-started/using-github/github-flow): the main branch serves as the production branch, and is locked.  Contributors, instead, must branch from main with their own feature branch.  The naming convention of feature branches are as follows: the contributor’s first name, followed by a slash, and the feature’s name (e.g. john/new-system.)  Once complete, the feature branch must be submitted as a Push Review on Github, where another team member must review and approve said code.  Finally, the branch is merged into main, under a single commit.

## 6.5 Risks {#5.5-risks}

As C++ is known for memory leaks and other issues, the following measures are being implemented:

First, a memory allocator is playing a role in pre-allocating memory for commonly used data features, such as Entities.  Second, a resource manager is being used to share and track memory used from loading larger binary assets such as textures and music.  For any pointers not managed by the first memory allocator, the `std::unique_ptr` feature provided by the C++ standard library is used extensively to manage that pointer’s lifetime and memory usage.


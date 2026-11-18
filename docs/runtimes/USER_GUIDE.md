# Using the Osteon2D runtimes: a step-by-step guide

Osteon2D ships two runtimes that play your animations inside a game engine:

| Engine | Folder | Status |
|---|---|---|
| **Godot 4.3+** | [`runtimes/godot/addons/osteon2d`](../../runtimes/godot/addons/osteon2d) | Core checked against the editor's own engine in headless Godot 4.7; the sprite node renders correctly in a real window. |
| **Unity 2021.3+** | [`runtimes/unity/com.osteon2d.runtime`](../../runtimes/unity/com.osteon2d.runtime) | Core checked against the editor's engine. The Unity components, importers and shaders compile, but **have not yet been run inside a real Unity editor**. Please report anything that goes wrong. |

Both are covered by the [Osteon 2D Community License](../../LICENSE), like the editor: you may ship them inside games you sell, but not sell the runtimes themselves. They contain no Esoteric Software code.

---

## 1. Export from Osteon2D

Every runtime needs three things from the editor: the skeleton, the atlas and its page images.

1. **Skeleton.** Open your project and choose **File > Export...** (Ctrl+E), then pick **JSON**. Keep the default 4.2 layout. You get `name.json`.
   - The runtimes can also read the `.osteon` project file directly, so exporting JSON is optional while you iterate.
   - **Binary (`.skel`) is not supported by the runtimes yet.** Use JSON.
2. **Atlas.** Choose **File > Texture Packer...** (Ctrl+P).
   - *Input folder*: your project's images folder. Every PNG in it is packed.
   - *Output folder*: where the atlas goes.
   - *Atlas name*: use the **same name as the skeleton file** (for example `hero` for `hero.json`). Unity then finds it automatically.
   - Click **Pack**. You get `name.atlas` plus its PNG page: `name.png`, or `name_1.png`, `name_2.png`, ... when everything does not fit on one page.
3. Keep the skeleton, the `.atlas` and the PNG pages **together in one folder**.

> The image names in the atlas must match the attachment names in the skeleton. They do if you pack the same images folder the project uses.

---

## 2. Godot 4

### Install

1. Copy `runtimes/godot/addons/osteon2d` into your project, so you have `res://addons/osteon2d/`.
2. Optional: enable it under **Project > Project Settings > Plugins**. The nodes also work without it, because they register themselves with `class_name`.

### Show a character

1. Copy the exported files (`hero.json` or `hero.osteon`, `hero.atlas`, `hero.png`) into the project, for example `res://characters/hero/`.
2. Add a node of type **Osteon2DSprite** to your scene.
3. In the Inspector:
   - **Skeleton Path**: the `.json` or `.osteon` file.
   - **Atlas Path**: the `.atlas` file.
   - **Skin**: `default`, or another skin name.
   - **Animation**: the animation to play, for example `idle`.
4. The character appears and animates in the editor straight away.

Other Inspector properties:

| Property | What it does |
|---|---|
| `loop` | Loop the animation set in the Inspector. |
| `playing` | Pause or resume. |
| `speed_scale` | Playback speed (1 = normal). |
| `update_mode` | `PROCESS` (every frame), `PHYSICS` (physics ticks) or `MANUAL` (you call `advance(delta)`). |
| `flip_h`, `flip_v` | Mirror the character. |
| `clipping` | Apply clipping attachments. |
| `debug_bones` | Draw the bones on top, for checking. |

### Control it from a script

```gdscript
extends Node2D

@onready var hero: Osteon2DSprite = $Hero

func _ready() -> void:
    hero.set_default_mix(0.15)            # crossfade between any two animations
    hero.set_mix("run", "jump", 0.05)     # a quicker one for this pair
    hero.play("idle")                     # track 0, looping
    hero.animation_event.connect(_on_event)
    hero.animation_completed.connect(_on_completed)

func _unhandled_input(e: InputEvent) -> void:
    if e.is_action_pressed("jump"):
        hero.play("jump", false)          # play once...
        hero.add_animation("idle", true)  # ...then go back to idle

func _on_event(track: int, event_name: String, i: int, f: float, s: String) -> void:
    if event_name == "footstep":
        $StepSound.play()

func _on_completed(track: int, animation_name: String) -> void:
    print(animation_name, " finished")
```

More calls:

| Call | What it does |
|---|---|
| `play(name, loop := true, track := 0)` | Start an animation now (crossfading from the current one). |
| `add_animation(name, loop := true, delay := 0.0, track := 0)` | Queue one after the current animation. |
| `stop(track := 0)` | Clear a track. |
| `set_skin(name)` | Switch skin. |
| `set_attachment(slot, attachment)` | Show a different image in a slot. An empty string goes back to what the animation shows. |
| `get_animation_names()`, `get_skin_names()`, `get_bone_names()` | List what the skeleton contains. |
| `get_bone_position(bone)`, `get_bone_rotation(bone)` | Where a bone is right now, in the sprite node's local space. |
| `reload()` | Load the files again after re-exporting. |

Signals: `animation_event`, `animation_completed`, `world_updated`, `skeleton_loaded`.

**Tracks** let animations play on top of each other. For example, legs run on track 0 while an "aim" animation plays on track 1: `hero.play("aim", true, 1)`.

### Attach something to a bone

Add an **Osteon2DBoneFollower** node, set **Sprite Path** to the Osteon2DSprite and **Bone Name** to the bone (for example `hand_r`). Its children then follow that bone. Use `follow_position`, `follow_rotation`, `follow_scale`, `local_offset` and `rotation_offset_degrees` to fine-tune.

### Exporting your game

Godot does not pack unknown file types by default. Under **Project > Export > Resources**, add `*.osteon, *.json, *.atlas` to **Filters to export non-resource files/folders**, or the character will be missing in the exported game.

---

## 3. Unity

> **Not yet tested inside a real Unity editor.** The code compiles against the Unity APIs it uses, and the animation core gives the same results as the Osteon2D editor. The scene components, importers and shaders still need a first real run. If something breaks, please open an issue with the Unity version and the console error.

### Install

1. Open **Window > Package Manager**, click **+**, choose **Add package from disk...** and pick `runtimes/unity/com.osteon2d.runtime/package.json`.
   Or copy the `com.osteon2d.runtime` folder into your project's `Packages/` folder.

### Show a character

1. Copy the exported files (`hero.json` or `hero.osteon`, `hero.atlas`, `hero.png`) into one folder under `Assets/`.
   The package imports `.osteon` and `.atlas` files as text assets automatically.
2. In the Project window, right-click the skeleton file (`hero.json` or `hero.osteon`) and choose **Create > Osteon2D > Skeleton Data From Selection**.
   - This creates `hero_SkeletonData.asset`, linked to the atlas next to it (`hero.atlas`, or the only `.atlas` in that folder) and its page images, with one material per page and blend mode.
   - It also sets the atlas page images to the import settings an atlas needs: no mipmaps, no compression, Clamp wrapping and *Alpha Is Transparency*. Without these, edges between packed images bleed and outlines get dark fringes. The Console says when it changed them.
3. Choose **GameObject > Osteon2D > Skeleton Animation**, or add the **Osteon2D/Skeleton Animation** component to any GameObject.
4. Drag the data asset into **Skeleton Data Asset**, and type an animation name into **Animation Name**.
5. The character previews in the Scene view (**Preview In Edit Mode**) and plays when you press Play.

Settings on the **Skeleton Data** asset:

| Field | What it does |
|---|---|
| `scale` | Pixels to Unity units. The default `0.01` means 100 pixels = 1 unit. |
| `defaultMix` | Default crossfade time between animations, in seconds. |

Settings on the **Skeleton Animation** component:

| Field | What it does |
|---|---|
| `initialSkin` | Skin at start (`default`). |
| `animationName`, `loop` | What plays at start. |
| `timeScale` | Playback speed. |
| `updateMode` | `Update`, `LateUpdate`, `FixedUpdate` or `Manual` (you call `Advance(delta)`). |
| `flipX`, `flipY` | Mirror the character. |
| `tintBlack` | Use the two-colour (tint black) shader. |
| `clipping` | Apply clipping attachments. |
| `sortingLayerName`, `sortingOrder` | 2D sorting, like a SpriteRenderer. |

### Control it from a script

```csharp
using UnityEngine;
using Osteon2D;

public class HeroController : MonoBehaviour
{
    OsteonSkeletonAnimation anim;

    void Start()
    {
        anim = GetComponent<OsteonSkeletonAnimation>();
        anim.State.SetMix("run", "jump", 0.05f);     // crossfade for this pair
        anim.Play("idle");                           // track 0, looping
        anim.AnimationEvent += (track, e) =>
        {
            if (e.name == "footstep") GetComponent<AudioSource>().Play();
        };
    }

    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            anim.Play("jump", false);                   // play once...
            anim.State.AddAnimation(0, "idle", true, 0); // ...then back to idle
        }
        anim.flipX = Input.GetAxisRaw("Horizontal") < 0;
    }
}
```

More calls:

| Call | What it does |
|---|---|
| `Play(name, loop = true, track = 0)` | Start an animation now. |
| `State.AddAnimation(track, name, loop, delay)` | Queue one after the current animation. |
| `State.SetMix(from, to, seconds)` | Crossfade time for one pair. |
| `State.ClearTrack(track)` | Stop a track. |
| `SetSkin(name)` | Switch skin. |
| `SetAttachment(slot, attachment)` | Show a different image in a slot. |
| `BoneWorldPosition(bone)` | Where a bone is, in world space. |
| `Initialize(true)` | Reload after replacing the data. |

Events: `AnimationEvent` (each event key the playhead passes; `e.name`, `e.intValue`, `e.floatValue`, `e.stringValue`), `UpdateWorld` (after the pose is computed, a good place to adjust bones) and `MeshRebuilt`.

### Attach something to a bone

Add the **Osteon2D/Bone Follower** component to a GameObject, set **Skeleton Animation** and **Bone Name**. Use `followPosition`, `followRotation`, `followScale`, `localOffset` and `rotationOffset` to fine-tune.

---

## 4. Updating art or animation

1. Change the project in Osteon2D and save it.
2. Export the JSON again, and run the Texture Packer again if any images changed.
3. Overwrite the files in your game project.
   - **Godot:** the sprite reloads when the paths change. Otherwise call `reload()` or reopen the scene.
   - **Unity:** the text assets reimport. Reselect the component, or call `Initialize(true)` from code.

## 5. What the runtimes do not support yet

- Path constraints, physics constraints and slider constraints (they are loaded but not applied).
- Spine 4.3 transform-constraint property links.
- Binary `.skel` files.
- Unity UI (Canvas) rendering, and Timeline or Mecanim integration.
- Tint black in Godot. Screen blend mode in Godot is drawn as additive.

## 6. Troubleshooting

| Problem | Fix |
|---|---|
| Nothing shows up | Check the skeleton and atlas paths, and that the animation and skin names are spelled exactly as in Osteon2D. |
| Some images are missing | The atlas was packed from a different images folder, or images were added after packing. Run the Texture Packer again on the folder the project uses. |
| The character is huge or tiny (Unity) | Change `scale` on the Skeleton Data asset (0.01 = 100 px per unit). |
| Works in the Godot editor but not in the exported game | Add `*.osteon, *.json, *.atlas` to the export resource filters. |
| "No .atlas file was found" (Unity) | Put the `.atlas` next to the skeleton file, ideally with the same name. |
| Blurry edges, lines between images or dark outlines (Unity) | The atlas PNGs have the wrong import settings. Run **Skeleton Data From Selection** again, which fixes them, or set them by hand: Texture Type *Default*, *Alpha Is Transparency* on, *Generate Mip Maps* off, Wrap Mode *Clamp*, Compression *None*. |
| Works in the editor, wrong colours or blending in a built game (Unity) | Select the Skeleton Data asset once in the editor and save the project. It then stores its shader references, so Unity includes the Osteon2D shaders in the build. |
| A Unity error you cannot explain | Please open an issue: the Unity layer has not been tested in the real editor yet. |

Questions and bug reports: <https://github.com/Divyash93/Osteon2D/issues>

# Fishing Game Sprite Sheet Spec (Suggested Defaults)

These are pragmatic defaults for a 1920x1080 2D fishing game. If your codebase already hard-codes frame sizes / counts, you should match that instead.

## General export rules
- Format: PNG (RGBA, straight alpha).
- Background: transparent.
- Keep a consistent pivot/anchor across frames (to avoid jitter).
- Prefer a fixed-size grid per sheet if your runtime uses frameWidth/frameHeight.
- If your runtime supports trimmed frames, you can trim and use JSON metadata (TexturePacker/Aseprite style).

## Fish (per species)
3 animations:
1) swim (loop)
2) caught (struggle loop or short non-loop)
3) escape (burst / dart; usually non-loop)

Recommended frame counts (good balance of quality vs memory):
- swim: 8 frames, 10–14 fps
- caught: 6–8 frames, 12–18 fps
- escape: 6–8 frames, 14–20 fps

Recommended layout options:
A) One sheet per animation
- fish_<name>_swim.png (1 row x 8 cols)
- fish_<name>_caught.png (1 row x 6–8 cols)
- fish_<name>_escape.png (1 row x 6–8 cols)

B) One sheet per fish, rows = animations (easiest to manage)
- fish_<name>.png
  Row 0: swim frames (8 cols)
  Row 1: caught frames (8 cols, pad empty slots if only 6)
  Row 2: escape frames (8 cols, pad empty slots if only 6)

Frame size guidance (choose a consistent size per fish):
- Small fish: 128x64 or 160x80
- Medium fish: 256x128
- Large fish: 384x192 or 512x256

Direction:
- If you allow horizontal flipping in-engine, animate facing RIGHT only and flip for left-facing.

Attachment point (hook):
- Put the fish “mouth” at a stable pixel location across all frames.
- If you need precision, store a mouth attachment point per frame in metadata (x,y in local sprite coordinates).

## Ships (per ship type)
Typical animations:
- idle/bob (loop): 6–8 frames at 6–10 fps
- move (loop): 6–10 frames at 10–14 fps
Optional:
- reel/winch (loop): 6 frames
- damage/splash (non-loop): 6–10 frames

Frame size guidance:
- 512x512 works well for large top-of-screen ships.
- If ships are smaller on screen, 384x384 or 256x256 may be sufficient.

Keep the waterline consistent frame-to-frame so the ship does not “jump”.

## Hooks (per hook type)
Typical animations:
- idle (loop): 4–6 frames (subtle sway)
- reel (loop): 6–8 frames
Optional:
- hookset (non-loop): 4–6 frames
- caught (loop): 4–6 frames (extra wiggle)

Frame size guidance:
- 128x128 to 256x256 depending on on-screen size.

Keep the “hook tip” position stable frame-to-frame (for collision and fish attachment).

## Shop icons (ships, hooks, fish if you sell them)
- Recommended icon size: 512x512 PNG (transparent background).
- Produce icons as individual PNGs, then pack into one atlas sheet + JSON.
- If you must deliver a simple grid sheet: cell = 512x512, consistent padding (e.g., 32px safe margin inside each cell).

Naming:
- icon_ship_<name>.png
- icon_hook_<name>.png
- icon_fish_<name>.png (if needed)


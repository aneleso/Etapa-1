THE PRIMEGAME — Etapa 4 art pack
Guide hand (mano guía) + soft snap VFX
Programación para Videojuegos 213027A  |  UNAD  |  palette matches the existing PrimeGame_Assets pack

================================================================
FILES
================================================================
hand_guide.png              512 x 512  RGBA   Idle pointing hand (index down)
hand_guide_tap.png          512 x 512  RGBA   Tap variant: finger slightly pressed
hand_guide.svg              vector source of the idle pose (editable)
hand_guide_tap.svg          vector source of the tap pose (editable)

vfx_snap_sheet.png         2048 x 256  RGBA   8-frame horizontal strip (left → right)
vfx_snap_frames/           256 x 256   RGBA   The same 8 frames as individual files
  vfx_snap_00.png … vfx_snap_07.png
svg/vfx_snap_00.svg …      vector source of each VFX frame

preview_hand_tap.gif        512 x 512         Cream-bg preview, 12 fps, 1 s loop
                                              (8 frames idle + 4 frames tap)
preview_vfx.gif             256 x 256         Cream-bg preview, 12 fps, 1 s loop
                                              (8 VFX frames + 4-frame rest)
preview_composite.png      1200 x 900         Labeled overview (not a game sprite)

================================================================
PALETTE (same as the existing pack / LEEME.txt)
================================================================
Cream       #F3EBDD
Mint        #A8DCC4      cuff, VFX ring, mint sparkles
Mint dark   #89B4A0      cuff outline / thickness / hem
Soft blue   #A9CDEE
Coral       #EE8E80
Star peach  #F3C9A0      skin (same fill as feedback_star.png)
Skin dark   #CA786C      skin outline / thickness (same as the star lip)
Soft yellow #F6E4B0      VFX sparkles
Background  #FAF8F3
Text        #2B2D33

Style: flat matte rounded shapes, mid-tone outline, a slight down-right
thickness. No harsh contrast, no flashing, no glossy highlights.

================================================================
GUIDE HAND — Unity import
================================================================
Texture Type        Sprite (2D and UI)
Sprite Mode         Single
Pixels Per Unit     100
Filter Mode         Bilinear
Compression         None
Max Size            512
Alpha               From Input, no extra alpha source
Generate Mipmaps    Off  (optional; 2D UI usually off)

PIVOT (custom) — at the index fingertip (the pad, front face, not the
thickness lip). Same value for idle and tap, so a 2-frame press keeps
the tip planted on the empty hole while the rest of the hand moves down.

  Pixel (origin top-left):   253, 424
  Unity pivot (origin bottom-left, 0–1):   (0.494, 0.172)

  Formula:  x = 253 / 512
            y = (512 - 424) / 512

Suggested use:
  - Place the sprite so the pivot sits on the free hole.
  - Move it toward the hole with a slow ease (the “machine” guide).
  - To tap: swap hand_guide → hand_guide_tap for ~0.33 s (4 frames at
    12 fps), then back. Do not play this as a rapid loop.
  - Honour the game’s “reduce / turn off animations” option (H/M/R):
    hide or freeze the hand when the player asks for no motion.

================================================================
SNAP VFX — Unity import
================================================================
Texture Type        Sprite (2D and UI)
Sprite Mode         Multiple
Sprite Editor       Slice → Grid By Cell Size → 256 x 256
                    (8 sprites in one row: vfx_snap_0 … vfx_snap_7)
Pixels Per Unit     100
Filter Mode         Bilinear
Compression         None
Max Size            2048
Alpha               From Input
Generate Mipmaps    Off
Pivot               Center  (0.5, 0.5)  for every slice

Animation:
  - 8 frames, 12 fps  →  about 0.67 s for one play-through.
  - Play once when a block snaps into a hole; do not loop by default.
  - Frame 0 is a small soft ring (gentle start, not a flash).
  - Frames 1–3: ring expands, mint + soft-yellow sparkles.
  - Frames 4–6: ring thins and fades, sparkles drift out.
  - Frame 7 is fully faded (empty / rest). Safe as a terminal frame.
  - No strobe: brightness peaks early and only fades after that.
  - If the player turns animations off, skip this VFX (or show only
    frame 3 as a still, then hide it).

If you prefer to import the frames one by one, use the files in
vfx_snap_frames/ with Sprite Mode = Single, PPU 100, pivot Center.

================================================================
NOTES FOR THE TEAM
================================================================
- The hand is a right hand, palm toward the camera, index pointing down,
  mint short-sleeve cuff, other fingers curled. It is a stylized toy
  hand, on purpose, so it sits with the existing rounded blocks.
- Both PNG sprites have a transparent background. Do not premultiply
  against cream in the importer.
- Editable SVG sources are included so the cuff, proportions or colours
  can be tweaked without redrawing from scratch.
- These are original assets made for The Primegame (no third-party art).

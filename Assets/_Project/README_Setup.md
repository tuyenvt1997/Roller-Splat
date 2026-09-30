# Roller Splat 2D — Hướng dẫn thiết lập trong Unity

Tất cả script nằm ở `Assets/_Project/Scripts/` (namespace `RollerSplat`).
Làm lần lượt các bước dưới đây. Tên field trong Inspector được ghi ở dạng `fieldName`
(Unity hiển thị thành "Field Name").

---

## Bước 1 — Sprite và Tile

1. Trong `Assets/_Project/Sprites/`: chuột phải → **Create → 2D → Sprites → Square**, đặt tên `Square`.
   Tiếp tục **Create → 2D → Sprites → Circle**, đặt tên `Circle`.
2. Mở **Window → 2D → Tile Palette** → bấm menu thả xuống tên palette → **Create New Tile Palette**,
   tên `RollerSplat`, bấm **Create** và chọn thư mục `Assets/_Project/Tiles/`.
3. Kéo sprite `Square` vào lưới của palette → Unity hỏi thư mục lưu → chọn `Assets/_Project/Tiles/`.
   Unity tạo tile tên `Square.asset` — đổi tên (F2) thành `FloorTile`.
4. Chọn `FloorTile.asset` → **Ctrl+D** để nhân bản → đổi tên thành `WallTile`.
   Chọn `WallTile.asset`, đổi **Color** sang màu tường (ví dụ xanh đậm `#1E2A44`).
   Kéo `WallTile.asset` vào một ô trống của palette.
   > `FloorTile` giữ màu trắng — màu của ô chưa sơn / đã sơn do script tô lúc chạy.

## Bước 2 — Tạo prefab level

1. Trong Hierarchy (Scene trống bất kỳ): **Create Empty**, tên `Level_01`, Position `(0,0,0)`.
   **Add Component → Level**.
2. Chuột phải `Level_01` → **2D Object → Tilemap → Rectangular**. Unity tạo `Grid` và một con `Tilemap`.
   Đổi tên con đó thành `Floor`; ở **Tilemap Renderer** đặt **Order in Layer = 0**.
3. Chuột phải `Grid` → **2D Object → Tilemap → Rectangular** lần nữa → đổi tên `Walls`, **Order in Layer = 1**.
4. Chuột phải `Level_01` → **Create Empty**, tên `StartPoint`.

   ```
   Level_01 (Level)
     Grid
       Floor   (Tilemap, Order 0)
       Walls   (Tilemap, Order 1)
     StartPoint
   ```

5. Vẽ màn trong Tile Palette:
   - Chọn **Active Tilemap = Floor**, cọ `FloorTile`, tô **mọi ô đường đi**.
   - Chọn **Active Tilemap = Walls**, cọ `WallTile`, tô viền tường bao quanh.
   - Đường đi là ô **có Floor và không có Walls**. Ô không có Floor luôn được coi là tường,
     nên bóng không bao giờ trượt ra ngoài bản đồ dù bạn vẽ sót tường.
6. Kéo `StartPoint` vào **bên trong** một ô Floor (với Grid mặc định, tâm ô `(x,y)` là `(x+0.5, y+0.5)`).
7. Chọn `Level_01`, gán component **Level**:

   | Field | Kéo vào |
   |---|---|
   | `floorTilemap` | `Floor` |
   | `wallTilemap` | `Walls` |
   | `startPoint` | `StartPoint` |
   | `floorColor` | màu ô chưa sơn (mặc định xám) |
   | `paintColor` | màu sơn (mặc định hồng) |

8. Kéo `Level_01` từ Hierarchy vào `Assets/_Project/Prefabs/Levels/` → tạo prefab. Xoá khỏi Scene.

### Hai màn mẫu (`#` = Walls, `.` = Floor, `S` = Floor + StartPoint)

**Level_01** — vòng tròn (giải: Phải → Xuống → Trái → Lên):
```
# # # # # # #
# S . . . . #
# . # # # . #
# . . . . . #
# # # # # # #
```

**Level_02** — có nhánh cụt (giải: Phải → Xuống → Trái → Lên → Phải → Lên):
```
# # # # # # # #
# S . . . . . #
# # # . # # . #
# . . . # # . #
# . # # # # . #
# . . . . . . #
# # # # # # # #
```
Tạo `Level_02` bằng cách **Duplicate** prefab `Level_01` (Ctrl+D), mở ra, xoá tile cũ và vẽ lại.

> **Mẹo thiết kế:** mọi ô Floor đều phải có cách lăn tới. Nếu còn ô không thể tới được thì màn
> không bao giờ thắng. Hãy tự chơi thử từng màn.

## Bước 3 — Scene `Game`

1. **File → New Scene** → chọn template **Lit 2D (URP)** (có sẵn Camera orthographic và Global Light 2D)
   → **Create** → lưu thành `Assets/_Project/Scenes/Game.unity`.
   (Nếu không thấy template này: chọn **Empty**, rồi thêm **Camera** và **Light → Global Light 2D**.)
2. `Main Camera`: **Projection = Orthographic**, Position `(0,0,-10)`, Background tuỳ chọn.
3. Tạo các GameObject rỗng và thêm component:

   ```
   Main Camera
   Input         (SwipeInput)
   GameManager   (GameManager)
   GridManager   (GridManager)
   LevelLoader   (LevelLoader)
   Ball          (SpriteRenderer + BallController)
   Canvas        (UIManager)       ← bước 4
   EventSystem
   ```

4. `Ball`: **Create → 2D Object → Sprites → Circle** (hoặc Empty + SpriteRenderer với sprite `Circle`),
   Scale `(0.8, 0.8, 1)`, **Order in Layer = 10**, màu trùng `paintColor` cho đẹp.

5. Gán tham chiếu:

   **Input → SwipeInput**
   | Field | Giá trị |
   |---|---|
   | `minSwipeFraction` | `0.05` (5% chiều cao màn hình) |

   **Ball → BallController**
   | Field | Kéo vào |
   |---|---|
   | `input` | `Input` |
   | `grid` | `GridManager` |
   | `cellsPerSecond` | `15` |

   **LevelLoader → LevelLoader**
   | Field | Kéo vào |
   |---|---|
   | `levels` | bấm `+` hai lần, kéo prefab `Level_01`, `Level_02` |
   | `levelRoot` | để trống (level sẽ là con của `LevelLoader`) |
   | `grid` | `GridManager` |
   | `ball` | `Ball` |
   | `targetCamera` | `Main Camera` (hoặc để trống = Camera.main) |
   | `fitCamera` | ✔ |
   | `cameraPadding` | `1` |

   **GameManager → GameManager**
   | Field | Kéo vào |
   |---|---|
   | `grid` | `GridManager` |
   | `loader` | `LevelLoader` |
   | `ball` | `Ball` |
   | `ui` | `Canvas` (có UIManager) |
   | `winDelay` | `0.5` |

## Bước 4 — UI

1. **Create → UI → Canvas**. Ở **Canvas Scaler**: **UI Scale Mode = Scale With Screen Size**,
   **Reference Resolution = 1080 × 1920**, **Match = 0.5**. Add Component **UIManager**.
   (Lần đầu tạo Text - TextMeshPro, Unity hỏi import **TMP Essentials** → bấm Import.)
2. Nếu EventSystem có **Standalone Input Module**, bấm nút **Replace with InputSystemUIInputModule**.
3. Tạo con của Canvas:

   ```
   Canvas (UIManager)
     LevelText      (Text - TextMeshPro, neo trên-trái, "Level 1")
     ProgressText   (Text - TextMeshPro, neo trên-phải, "0%")
     ProgressBg     (Image, neo trên, cao ~20)
       ProgressFill (Image, Image Type = Filled, Fill Method = Horizontal, Fill Amount = 0)
     RestartButton  (Button - TextMeshPro, neo dưới, chữ "Restart")
     WinPanel       (Panel, ⚠ bỏ tick Active ở góc trên Inspector)
       WinText      (Text - TextMeshPro, giữa, "Hoàn thành!")
       NextButton   (Button - TextMeshPro, chữ "Next")
   ```

   `ProgressFill` cần một sprite (chọn `Square`), nếu không Fill Amount sẽ không có tác dụng.

4. Gán **Canvas → UIManager**:

   | Field | Kéo vào |
   |---|---|
   | `levelText` | `LevelText` |
   | `progressText` | `ProgressText` |
   | `progressFill` | `ProgressFill` |
   | `winPanel` | `WinPanel` |
   | `winText` | `WinText` |

5. Nối nút:
   - `NextButton` → **On Click ()** → `+` → kéo `GameManager` → chọn **GameManager → OnNextPressed**.
   - `RestartButton` → **On Click ()** → `+` → kéo `GameManager` → chọn **GameManager → OnRestartPressed**.

> **Chữ tiếng Việt bị ô vuông?** Font mặc định của TMP có thể thiếu dấu. Chọn một font hỗ trợ tiếng Việt
> (ví dụ Roboto, Arial), **Window → TextMeshPro → Font Asset Creator** hoặc chuột phải font →
> **Create → TextMeshPro → Font Asset** (Atlas Population Mode = Dynamic), rồi gán cho các text.

## Bước 5 — Build Settings

**File → Build Profiles** (hoặc Build Settings) → **Add Open Scenes** để thêm `Game`.

## Bước 6 — Chơi thử

Bấm **Play** trong Scene `Game`:

- Phím **mũi tên / WASD** hoặc **kéo chuột / vuốt** để lăn bóng.
- Sơn kín 100% → sau 0.5s hiện `WinPanel` → **Next** sang màn tiếp. Hết màn → quay lại Level 1.
- **Restart** chơi lại màn hiện tại.

## Xử lý sự cố

| Triệu chứng | Nguyên nhân |
|---|---|
| Console: `has no walkable cells` | Chưa vẽ tile trên `Floor`, hoặc gán nhầm `floorTilemap`. |
| Console: `startPoint ... is not on a floor cell` | `StartPoint` nằm ngoài ô Floor hoặc trên ô có Walls. |
| Console: `has no levels in its list` | Chưa kéo prefab vào `levels` của LevelLoader. |
| Bóng không động đậy | `Ball` chưa gán `input`/`grid`; hoặc Project Settings → Player → **Active Input Handling** không có Input System. |
| Bấm nút UI không ăn | Thiếu EventSystem hoặc chưa đổi sang `InputSystemUIInputModule`. |
| Không bao giờ thắng | Còn ô Floor không thể lăn tới — sửa lại màn. |

## Chạy test

**Window → General → Test Runner → EditMode → Run All** (24 test cho GridModel, GridManager,
SwipeInput, SwipeTracker, LevelLoader).

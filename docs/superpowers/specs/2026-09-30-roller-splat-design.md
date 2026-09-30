# Roller Splat 2D — Design Spec

Ngày: 2026-09-30
Engine: Unity 6000.0.84f1, URP 2D Renderer, Input System 1.20, Unity Test Framework 1.6

## 1. Mục tiêu

Game puzzle 2D kiểu "Roller Splat": người chơi vuốt (chuột/cảm ứng) hoặc bấm phím mũi tên/WASD để bóng trượt thẳng theo 4 hướng cho đến khi chạm tường. Mỗi ô đường đi bóng lăn qua được sơn sang màu sáng. Sơn kín 100% ô đường đi → hiện UI chiến thắng → sang level tiếp theo.

Yêu cầu phi chức năng:
- Code modular: mỗi script một trách nhiệm, giao tiếp qua tham chiếu gán trong Inspector và C# event; không dùng `Find*`.
- Dựng màn bằng Tilemap + Tile Palette (kéo thả trong Editor).
- Kèm hướng dẫn thiết lập Scene chi tiết (`Assets/_Project/README_Setup.md`).

Ngoài phạm vi (bản này): lưu tiến trình, màn chọn level, âm thanh, particle/squash, undo, nhiều bóng/nhiều màu.

## 2. Quyết định kiến trúc

- **Di chuyển theo lưới (grid-based)**, không dùng Rigidbody2D. Đường trượt được tính trước bằng cách dò ô trong `GridModel`; bóng được nội suy mượt bằng coroutine. Bóng luôn dừng đúng tâm ô.
- **1 Scene duy nhất**, mỗi level là **1 prefab**. `LevelLoader` instantiate/destroy prefab.
- **Input đọc trực tiếp thiết bị** qua Input System (`Keyboard.current`, `Pointer.current`), không dùng file `.inputactions`.
- **Tường về mặt logic = mọi ô không thuộc tập walkable.** `wallTilemap` phục vụ hiển thị và loại trừ ô bị vẽ chồng.

## 3. Thành phần

| Script | Loại | Gắn vào | Trách nhiệm |
|---|---|---|---|
| `GridModel` | C# thuần | — | Tập ô walkable, tập ô đã sơn, `GetSlidePath`, `TryPaint`, `PaintedCount`, `TotalCount`, `IsComplete`. |
| `Level` | MonoBehaviour (dữ liệu) | Root prefab level | `floorTilemap`, `wallTilemap`, `startPoint`, `floorColor`, `paintColor`. |
| `GridManager` | MonoBehaviour | GO `GridManager` | `Setup(Level)` dựng `GridModel` từ Tilemap; `GetSlidePath`; `PaintCell` (đổi màu tile + phát event); `CellToWorld`/`WorldToCell`. Event `OnProgressChanged(int painted, int total)`. |
| `SwipeInput` | MonoBehaviour | GO `Input` | Phím + vuốt → event `OnSwipe(Vector2Int dir)`. |
| `BallController` | MonoBehaviour | GO `Ball` | Nghe `SwipeInput`; di chuyển theo path bằng coroutine; sơn ô khi tới; `PlaceAt(cell)`; `InputLocked`. |
| `LevelLoader` | MonoBehaviour | GO `LevelLoader` | `List<Level> levels`; `Load(i)`, `LoadNext()`, `Reload()`; tuỳ chọn `fitCamera`. Event `OnLevelLoaded(int index)`. |
| `GameManager` | MonoBehaviour | GO `GameManager` | Nghe progress; phát hiện thắng; khoá input; sau `winDelay` gọi UI; xử lý Next/Restart. |
| `UIManager` | MonoBehaviour | `Canvas` | Text "Level N", thanh/text %, panel Win, nút Next/Restart. |

### Luồng dữ liệu

```
Vuốt/Phím → SwipeInput.OnSwipe → BallController
  → GridManager.GetSlidePath → coroutine di chuyển
  → GridManager.PaintCell (từng ô) → OnProgressChanged
      → UIManager cập nhật %
      → GameManager: painted == total → Won → khoá input → (winDelay) → UIManager.ShowWin
  Next → GameManager → LevelLoader.LoadNext → GridManager.Setup + BallController.PlaceAt
```

## 4. Logic chi tiết

### GridModel
- Constructor nhận `IEnumerable<Vector3Int> walkableCells`.
- `GetSlidePath(Vector3Int start, Vector3Int dir)`: `next = start + dir`; `while walkable.Contains(next)` → thêm vào list, `next += dir`. Trả `List<Vector3Int>` (rỗng nếu bị chặn ngay).
- `TryPaint(cell)`: trả `true` nếu ô walkable và chưa sơn (thêm vào tập sơn); ngược lại `false`.
- `IsComplete` ⇔ `TotalCount > 0 && PaintedCount == TotalCount`.

### GridManager
- `Setup(level)`: duyệt `floorTilemap.cellBounds.allPositionsWithin`; ô có tile floor và không có tile wall → walkable. Với mỗi ô walkable: `SetTileFlags(None)` + `SetColor(floorColor)`. Tạo `GridModel`. Phát `OnProgressChanged(0, total)`.
- `PaintCell(cell)`: nếu `model.TryPaint(cell)` → `SetColor(cell, paintColor)` → phát `OnProgressChanged`.
- Chuyển đổi toạ độ qua `floorTilemap.GetCellCenterWorld` / `WorldToCell`.

### BallController
- Trường Inspector: `SwipeInput input`, `GridManager grid`, `float cellsPerSecond = 15`.
- `HandleSwipe(dir)`: bỏ qua nếu `isMoving || InputLocked`; lấy path; rỗng → bỏ qua; ngược lại chạy coroutine.
- Coroutine: với mỗi ô, `MoveTowards` tới tâm ô; tới nơi → `currentCell = cell`, `grid.PaintCell(cell)`.
- `PlaceAt(cell)`: dừng coroutine, `isMoving = false`, đặt vị trí, `currentCell = cell`, `grid.PaintCell(cell)`.

### SwipeInput
- Phím: `upArrow/wKey`, `downArrow/sKey`, `leftArrow/aKey`, `rightArrow/dKey` với `wasPressedThisFrame`.
- Vuốt: `Pointer.current`; `press.wasPressedThisFrame` → lưu `startPos`, `swipeConsumed = false`. Khi đang giữ và chưa consumed: `delta = pos - startPos`; nếu `delta.magnitude >= minSwipeFraction * Screen.height` (mặc định 0.05) → hướng theo trục trội → phát `OnSwipe`, `swipeConsumed = true`.

### GameManager
- Trường: `GridManager grid`, `LevelLoader loader`, `BallController ball`, `UIManager ui`, `float winDelay = 0.5f`.
- `OnProgressChanged`: nếu state `Playing` và `painted == total && total > 0` → state `Won`, `ball.InputLocked = true`, coroutine đợi `winDelay` → `ui.ShowWin(isLastLevel)`.
- `OnLevelLoaded`: state `Playing`, `ball.InputLocked = false`, `ui.HideWin()`, `ui.SetLevel(index + 1)`.
- `OnNextPressed()` → `loader.LoadNext()` (hết level → quay về level 0). `OnRestartPressed()` → `loader.Reload()`.
- `Start()` → `loader.Load(0)`.

### LevelLoader
- `Load(i)`: validate index; `Destroy` level hiện tại; `Instantiate(levels[i], levelRoot)`; `grid.Setup(level)`; tính ô start = `grid.WorldToCell(level.startPoint.position)`; nếu không walkable → `LogError`; `ball.PlaceAt(startCell)`; nếu `fitCamera` → căn camera theo `floorTilemap` bounds + `cameraPadding`; phát `OnLevelLoaded(i)`.

### UIManager
- Dùng `TMPro.TMP_Text` cho text; `Image` (Filled) cho thanh tiến độ (tuỳ chọn, null-safe).
- `SetLevel(n)`, `SetProgress(painted, total)`, `ShowWin(bool isLast)`, `HideWin()`.
- Nút Next/Restart nối `OnClick` → `GameManager.OnNextPressed/OnRestartPressed` trong Inspector.

## 5. Xử lý lỗi cấu hình

`Debug.LogError` với thông điệp rõ ràng (kèm tên object), không crash:
- Danh sách level rỗng / index ngoài phạm vi.
- Level thiếu `floorTilemap` hoặc `startPoint`.
- Level có 0 ô walkable.
- `startPoint` không nằm trên ô walkable.
- Tham chiếu Inspector bị thiếu ở các manager (`OnValidate`/`Awake` cảnh báo).

## 6. Cấu trúc thư mục & Scene

```
Assets/_Project/
  Scripts/         RollerSplat.asmdef (ref Unity.InputSystem, Unity.TextMeshPro)
  Tests/EditMode/  RollerSplat.Tests.asmdef (Editor only, ref RollerSplat + test runner)
  Sprites/ Tiles/ Prefabs/Levels/
  Scenes/Game.unity
  README_Setup.md
```

Hierarchy Scene `Game`:
```
Main Camera (Orthographic)
Input        (SwipeInput)
GameManager  (GameManager)
GridManager  (GridManager)
LevelLoader  (LevelLoader)   ← prefab level được spawn làm con
Ball         (SpriteRenderer, Order in Layer 10, BallController)
Canvas       (UIManager) + EventSystem (InputSystemUIInputModule)
```

Prefab level:
```
Level_XX (Level)
  Grid
    Floor  (Tilemap, TilemapRenderer Order 0)
    Walls  (Tilemap, TilemapRenderer Order 1)
  StartPoint
```

## 7. Kiểm thử

EditMode tests cho `GridModel`:
- Trượt trên hàng thẳng dừng đúng ô cuối trước tường.
- Bị chặn ngay → path rỗng.
- Lưới hình chữ L: trượt dừng ở góc.
- `TryPaint` lần 2 trên cùng ô trả `false`, không tăng đếm.
- `TryPaint` ô không walkable trả `false`.
- Sơn đủ mọi ô → `IsComplete == true`; lưới rỗng → `IsComplete == false`.

Kiểm tra tay trong Editor: chơi Level_01 bằng phím; chơi bằng vuốt chuột; hoàn thành → panel Win → Next sang Level_02; Restart reset màu; level cuối → Next quay về Level_01.

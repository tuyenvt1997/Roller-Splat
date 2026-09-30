# Roller Splat 2D Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Game puzzle 2D kiểu Roller Splat: bóng trượt theo lưới tới khi chạm tường, sơn ô đi qua, sơn 100% thì thắng và sang level kế.

**Architecture:** Logic lưới nằm trong class C# thuần `GridModel` (test EditMode được). Các MonoBehaviour mỏng (`GridManager`, `BallController`, `SwipeInput`, `LevelLoader`, `GameManager`, `UIManager`) nối nhau bằng tham chiếu Inspector + C# event. 1 Scene, mỗi level là 1 prefab Tilemap.

**Tech Stack:** Unity 6000.0.84f1, URP 2D Renderer, Tilemap, Input System 1.20, uGUI + TextMeshPro, Unity Test Framework 1.6 (NUnit).

**Spec:** `docs/superpowers/specs/2026-09-30-roller-splat-design.md`

## Global Constraints

- Mọi script runtime trong `Assets/_Project/Scripts/`, namespace `RollerSplat`, assembly `RollerSplat` (asmdef tham chiếu `Unity.InputSystem`, `Unity.TextMeshPro`).
- Test trong `Assets/_Project/Tests/EditMode/`, assembly `RollerSplat.Tests` (Editor only, `UNITY_INCLUDE_TESTS`, tham chiếu `RollerSplat`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `nunit.framework.dll`).
- Không dùng `Find*`/`FindObjectOfType`; mọi tham chiếu gán bằng `[SerializeField]` trong Inspector.
- Không dùng Rigidbody2D cho di chuyển; không dùng file `.inputactions`.
- Giá trị mặc định: `cellsPerSecond = 15`, `minSwipeFraction = 0.05`, `winDelay = 0.5`, `cameraPadding = 1`.
- Lỗi cấu hình → `Debug.LogError` kèm tên object, không throw.
- Hướng là `Vector3Int` trên trục x/y (`Vector3Int.up/down/left/right`).

## Cách chạy test

- **Editor đang mở** (hiện tại): *Window → General → Test Runner → EditMode → Run All*. Kiểm tra lỗi biên dịch: `grep -E "error CS" "$LOCALAPPDATA/Unity/Editor/Editor.log" | tail -20` sau khi focus lại cửa sổ Unity để nó recompile.
- **Editor đã đóng:**
  `"E:/unity/6000.0.84f1/Editor/Unity.exe" -batchmode -projectPath "E:/unity/game 1/game puzzle" -runTests -testPlatform EditMode -testResults "E:/unity/game 1/game puzzle/Temp/results.xml" -logFile -`
  rồi `grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' Temp/results.xml | head -1`. PASS = `failed="0"`.

## Review Focus

1. Vuốt chéo gần 45° hoặc vuốt ngắn dưới ngưỡng → chỉ ra 1 hướng theo trục trội (hoà thì ưu tiên ngang), dưới ngưỡng thì không ra hướng → test `DirectionFromDelta_*` (Task 3).
2. Ô vừa có tile Floor vừa có tile Wall (vẽ chồng) → không được tính là đường đi → test `Setup_ExcludesCellsCoveredByWall` (Task 2).
3. Bấm Restart/Next trong lúc đang chờ `winDelay` → panel Win không được bật lại sau khi level mới đã load → `GameManager.HandleLevelLoaded` dừng coroutine thắng (Task 4, bước kiểm tra tay 5.6).
4. Màn có kích thước khác nhau / màn hình dọc (aspect < 1) → camera vẫn chứa toàn bộ sàn → test `ComputeOrthoSize_*` (Task 4).
5. Tilemap bị xoá ô sau khi vẽ (cellBounds còn rộng, có ô rỗng) → ô rỗng không được đếm → test `Setup_IgnoresEmptyCellsInsideBounds` (Task 2).

---

### Task 1: Scaffolding + GridModel (TDD)

**Files:**
- Create: `Assets/_Project/Scripts/RollerSplat.asmdef`
- Create: `Assets/_Project/Scripts/GridModel.cs`
- Create: `Assets/_Project/Tests/EditMode/RollerSplat.Tests.asmdef`
- Create: `Assets/_Project/Tests/EditMode/GridModelTests.cs`
- Create (thư mục rỗng + `.gitkeep`): `Assets/_Project/Sprites/`, `Tiles/`, `Prefabs/Levels/`, `Scenes/`

**Interfaces:**
- Produces:
  - `public class GridModel { GridModel(IEnumerable<Vector3Int> walkableCells); int TotalCount {get;} int PaintedCount {get;} bool IsComplete {get;} bool IsWalkable(Vector3Int c); bool IsPainted(Vector3Int c); bool TryPaint(Vector3Int c); List<Vector3Int> GetSlidePath(Vector3Int start, Vector3Int dir); }`

- [ ] **Step 1: Tạo 2 asmdef** theo Global Constraints (tests asmdef: `"includePlatforms": ["Editor"]`, `"overrideReferences": true`, `"precompiledReferences": ["nunit.framework.dll"]`, `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`, `"autoReferenced": false`).

- [ ] **Step 2: Viết test thất bại** trong `GridModelTests.cs` (helper `Row(n)` tạo các ô `(0..n-1, 0, 0)`):

```csharp
[Test] public void GetSlidePath_StraightRow_StopsBeforeWall() {
    var m = new GridModel(Row(4));
    var path = m.GetSlidePath(new Vector3Int(0,0,0), Vector3Int.right);
    CollectionAssert.AreEqual(new[]{ new Vector3Int(1,0,0), new Vector3Int(2,0,0), new Vector3Int(3,0,0) }, path);
}
[Test] public void GetSlidePath_BlockedImmediately_ReturnsEmpty() {
    Assert.IsEmpty(new GridModel(Row(4)).GetSlidePath(Vector3Int.zero, Vector3Int.left));
}
[Test] public void GetSlidePath_LShape_StopsAtCorner() {
    // (0,0),(1,0),(2,0),(2,1),(2,2)
    var path = LShapeModel().GetSlidePath(Vector3Int.zero, Vector3Int.right);
    Assert.AreEqual(new Vector3Int(2,0,0), path[^1]); Assert.AreEqual(2, path.Count);
}
[Test] public void TryPaint_SameCellTwice_CountsOnce() {
    var m = new GridModel(Row(3));
    Assert.IsTrue(m.TryPaint(Vector3Int.zero)); Assert.IsFalse(m.TryPaint(Vector3Int.zero));
    Assert.AreEqual(1, m.PaintedCount);
}
[Test] public void TryPaint_NonWalkable_ReturnsFalse() {
    var m = new GridModel(Row(3));
    Assert.IsFalse(m.TryPaint(new Vector3Int(5,5,0))); Assert.AreEqual(0, m.PaintedCount);
}
[Test] public void IsComplete_AllPainted_True() {
    var m = new GridModel(Row(2)); m.TryPaint(new(0,0,0)); Assert.IsFalse(m.IsComplete);
    m.TryPaint(new(1,0,0)); Assert.IsTrue(m.IsComplete);
}
[Test] public void IsComplete_EmptyGrid_False() {
    Assert.IsFalse(new GridModel(new Vector3Int[0]).IsComplete);
}
```

- [ ] **Step 3: Chạy test → FAIL** (lỗi biên dịch `GridModel` chưa tồn tại).

- [ ] **Step 4: Implement `GridModel`** trong `GridModel.cs`: hai `HashSet<Vector3Int>` (walkable, painted); `GetSlidePath` theo spec §4.

- [ ] **Step 5: Chạy test → PASS** (7/7).

- [ ] **Step 6: Commit** — `git add Assets/_Project docs .gitignore Packages ProjectSettings Assets/*.meta` → `feat: add GridModel with EditMode tests`.

---

### Task 2: Level + GridManager

**Files:**
- Create: `Assets/_Project/Scripts/Level.cs`
- Create: `Assets/_Project/Scripts/GridManager.cs`
- Test: `Assets/_Project/Tests/EditMode/GridManagerTests.cs`

**Interfaces:**
- Consumes: `GridModel` (Task 1).
- Produces:
  - `public class Level : MonoBehaviour { Tilemap FloorTilemap {get;} Tilemap WallTilemap {get;} Transform StartPoint {get;} Color FloorColor {get;} Color PaintColor {get;} }` — backing fields `[SerializeField]`, mặc định `floorColor = new Color(0.25f,0.25f,0.3f)`, `paintColor = new Color(1f,0.45f,0.7f)`. Thêm `public void Init(Tilemap floor, Tilemap wall, Transform start)` chỉ để test dựng level bằng code.
  - `public class GridManager : MonoBehaviour { event Action<int,int> OnProgressChanged; GridModel Model {get;} bool Setup(Level level); List<Vector3Int> GetSlidePath(Vector3Int start, Vector3Int dir); bool PaintCell(Vector3Int cell); Vector3 CellToWorld(Vector3Int cell); Vector3Int WorldToCell(Vector3 world); bool IsWalkable(Vector3Int cell); Bounds GetFloorWorldBounds(); }`
  - `Setup` trả `false` + `LogError` nếu level null / thiếu floorTilemap / 0 ô walkable. `wallTilemap` null được phép.

- [ ] **Step 1: Viết test thất bại** — test dựng `GameObject` có `Grid`, 2 con có `Tilemap`, tile tạo bằng `ScriptableObject.CreateInstance<Tile>()`; `[TearDown]` `Object.DestroyImmediate` mọi thứ.

```csharp
[Test] public void Setup_CountsFloorCells()                 // 3 ô floor → Model.TotalCount == 3, trả true
[Test] public void Setup_ExcludesCellsCoveredByWall()       // 3 floor, 1 trong đó có wall → TotalCount == 2, IsWalkable(ô đó) == false
[Test] public void Setup_IgnoresEmptyCellsInsideBounds()    // SetTile 3 ô rồi SetTile(ô giữa, null) → TotalCount == 2
[Test] public void Setup_NoWalkableCells_ReturnsFalse()     // LogAssert.Expect(LogType.Error, new Regex("no walkable")); Setup(...) == false
[Test] public void Setup_SetsFloorColor()                   // floor.GetColor(c) == level.FloorColor
[Test] public void PaintCell_ChangesColorAndRaisesProgress() // PaintCell(c) == true; floor.GetColor(c) == PaintColor; event nhận (1,3)
[Test] public void PaintCell_AlreadyPainted_NoEvent()       // gọi 2 lần → event chỉ bắn 1 lần, lần 2 trả false
```

- [ ] **Step 2: Chạy test → FAIL** (chưa có `Level`, `GridManager`).

- [ ] **Step 3: Implement `Level` và `GridManager`** theo spec §4. Tô màu: `SetTileFlags(c, TileFlags.None)` rồi `SetColor`. `Setup` phát `OnProgressChanged(0, total)`. Thông điệp lỗi chứa `"no walkable"` và `level.name`.

- [ ] **Step 4: Chạy test → PASS** (14/14 tổng).

- [ ] **Step 5: Commit** — `feat: add Level data and GridManager tilemap wrapper`.

---

### Task 3: SwipeInput + BallController

**Files:**
- Create: `Assets/_Project/Scripts/SwipeInput.cs`
- Create: `Assets/_Project/Scripts/BallController.cs`
- Test: `Assets/_Project/Tests/EditMode/SwipeInputTests.cs`

**Interfaces:**
- Consumes: `GridManager.GetSlidePath`, `PaintCell`, `CellToWorld` (Task 2).
- Produces:
  - `public class SwipeInput : MonoBehaviour { event Action<Vector3Int> OnSwipe; static Vector3Int? DirectionFromDelta(Vector2 delta, float minDistance); }` — `[SerializeField] float minSwipeFraction = 0.05f`.
  - `public class BallController : MonoBehaviour { bool InputLocked {get;set;} bool IsMoving {get;} Vector3Int CurrentCell {get;} void PlaceAt(Vector3Int cell); }` — `[SerializeField] SwipeInput input; GridManager grid; float cellsPerSecond = 15f`. Subscribe `OnEnable`, unsubscribe `OnDisable`.

- [ ] **Step 1: Viết test thất bại** cho `DirectionFromDelta`:

```csharp
[Test] public void DirectionFromDelta_BelowThreshold_Null()   => Assert.IsNull(SwipeInput.DirectionFromDelta(new Vector2(3,2), 10));
[Test] public void DirectionFromDelta_Right()                 => Assert.AreEqual(Vector3Int.right, SwipeInput.DirectionFromDelta(new Vector2(20,5), 10));
[Test] public void DirectionFromDelta_Down()                  => Assert.AreEqual(Vector3Int.down,  SwipeInput.DirectionFromDelta(new Vector2(-5,-20), 10));
[Test] public void DirectionFromDelta_NearDiagonal_DominantAxis() => Assert.AreEqual(Vector3Int.up, SwipeInput.DirectionFromDelta(new Vector2(14,15), 10));
[Test] public void DirectionFromDelta_ExactDiagonal_PrefersHorizontal() => Assert.AreEqual(Vector3Int.left, SwipeInput.DirectionFromDelta(new Vector2(-15,15), 10));
```

- [ ] **Step 2: Chạy test → FAIL.**

- [ ] **Step 3: Implement `SwipeInput`** theo spec §4 (phím: `Keyboard.current` null-safe; vuốt: `Pointer.current`, ngưỡng `minSwipeFraction * Screen.height`, phát 1 lần mỗi lần nhấn, qua `DirectionFromDelta`).

- [ ] **Step 4: Implement `BallController`** theo spec §4. `PlaceAt` gọi `StopAllCoroutines()` trước khi đặt vị trí. Coroutine dùng `Vector3.MoveTowards(pos, target, cellsPerSecond * cellSize * Time.deltaTime)` với `cellSize` = khoảng cách giữa `CellToWorld(cell)` và `CellToWorld(cell + dir)` (tính một lần mỗi lượt trượt).

- [ ] **Step 5: Chạy test → PASS** (19/19), không có `error CS` trong Editor.log.

- [ ] **Step 6: Commit** — `feat: add swipe/keyboard input and grid ball movement`.

---

### Task 4: LevelLoader + GameManager + UIManager

**Files:**
- Create: `Assets/_Project/Scripts/LevelLoader.cs`
- Create: `Assets/_Project/Scripts/GameManager.cs`
- Create: `Assets/_Project/Scripts/UIManager.cs`
- Test: `Assets/_Project/Tests/EditMode/LevelLoaderTests.cs`

**Interfaces:**
- Consumes: `GridManager.Setup/WorldToCell/IsWalkable/GetFloorWorldBounds/OnProgressChanged`, `BallController.PlaceAt/InputLocked`, `Level.StartPoint`.
- Produces:
  - `public class LevelLoader : MonoBehaviour { event Action<int> OnLevelLoaded; int CurrentIndex {get;} int LevelCount {get;} bool IsLastLevel {get;} void Load(int index); void LoadNext(); void Reload(); static float ComputeOrthoSize(Vector2 boundsSize, float aspect, float padding); }` — `[SerializeField] List<Level> levels; Transform levelRoot; GridManager grid; BallController ball; Camera targetCamera; bool fitCamera = true; float cameraPadding = 1f`. `LoadNext` quay về 0 sau level cuối. `Load` dùng `Destroy` cho level cũ (runtime).
  - `ComputeOrthoSize` = `max(h/2, (w/2)/aspect) + padding`.
  - `public class UIManager : MonoBehaviour { void SetLevel(int levelNumber); void SetProgress(int painted, int total); void ShowWin(bool isLastLevel); void HideWin(); }` — `[SerializeField] TMP_Text levelText, progressText, winText; Image progressFill; GameObject winPanel;` mọi field null-safe. `progressText` = `"{percent}%"` (làm tròn xuống); `winText` = `"Hoàn thành!"` hoặc `"Bạn đã phá đảo!"` khi là level cuối.
  - `public class GameManager : MonoBehaviour { void OnNextPressed(); void OnRestartPressed(); }` — `[SerializeField] GridManager grid; LevelLoader loader; BallController ball; UIManager ui; float winDelay = 0.5f`. `Start()` → `loader.Load(0)`.

- [ ] **Step 1: Viết test thất bại** cho `ComputeOrthoSize`:

```csharp
[Test] public void ComputeOrthoSize_Landscape_FitsHeight() => Assert.AreEqual(6f, LevelLoader.ComputeOrthoSize(new Vector2(10,10), 16f/9f, 1f), 1e-4);
[Test] public void ComputeOrthoSize_Portrait_FitsWidth()   => Assert.AreEqual(10f/(9f/16f)/2f + 1f, LevelLoader.ComputeOrthoSize(new Vector2(10,6), 9f/16f, 1f), 1e-4);
```

- [ ] **Step 2: Chạy test → FAIL.**

- [ ] **Step 3: Implement `LevelLoader`** theo spec §4 + lỗi §5 (danh sách rỗng, index ngoài phạm vi, thiếu `StartPoint`, start không walkable → `LogError` và dừng). Camera: đặt `x,y` = tâm bounds, giữ `z`.

- [ ] **Step 4: Implement `UIManager`** theo Interfaces.

- [ ] **Step 5: Implement `GameManager`**: subscribe `grid.OnProgressChanged` và `loader.OnLevelLoaded` trong `OnEnable`, unsubscribe `OnDisable`. Progress → `ui.SetProgress` + kiểm tra thắng (spec §4). `HandleLevelLoaded(i)`: dừng coroutine thắng đang chạy (giữ `Coroutine winRoutine`), state `Playing`, `ball.InputLocked = false`, `ui.HideWin()`, `ui.SetLevel(i + 1)`, `ui.SetProgress(grid.Model.PaintedCount, grid.Model.TotalCount)`, rồi nếu `grid.Model.IsComplete` (màn 1 ô) thì kích hoạt thắng ngay. Lý do: `LevelLoader.Load` gọi `grid.Setup` và `ball.PlaceAt` (bắn progress) **trước** `OnLevelLoaded`, lúc state chưa là `Playing`. `Awake` cảnh báo field null.

- [ ] **Step 6: Chạy test → PASS** (21/21), không có `error CS`.

- [ ] **Step 7: Commit** — `feat: add level loading, win condition and UI`.

---

### Task 5: Assets, Scene, 2 level mẫu, hướng dẫn thiết lập

**Files:**
- Create: `Assets/_Project/README_Setup.md`
- Create (qua Unity Editor, theo README): `Sprites/Square.png` (hoặc sprite Square tạo từ menu), `Tiles/FloorTile.asset`, `Tiles/WallTile.asset`, `Prefabs/Levels/Level_01.prefab`, `Level_02.prefab`, `Scenes/Game.unity`

**Interfaces:**
- Consumes: tất cả component từ Task 2–4 (tên field Inspector như Interfaces).

- [ ] **Step 1: Viết `README_Setup.md`** (tiếng Việt), từng bước đánh số: (1) tạo sprite Square + Circle, 2 tile; (2) tạo prefab level đúng hierarchy spec §6, vẽ bằng Tile Palette, đặt `StartPoint` vào tâm ô, gán field `Level`; (3) tạo Scene `Game` đúng hierarchy spec §6, gán từng field của 6 component (liệt kê tên field → object kéo vào); (4) UI: Canvas Scaler *Scale With Screen Size* 1080×1920, `LevelText`, `ProgressText`, `ProgressFill` (Image Filled Horizontal), `WinPanel` (tắt sẵn) chứa `WinText` + nút `Next`, nút `Restart`; nối `OnClick` → `GameManager.OnNextPressed / OnRestartPressed`; EventSystem dùng `InputSystemUIInputModule`; (5) thêm Scene vào Build Settings; (6) mẹo thiết kế màn (mọi ô phải tới được, nếu không màn không thể thắng). Kèm sơ đồ ASCII 2 level mẫu.

- [ ] **Step 2: Người dùng dựng assets/Scene theo README** (Level_01: hình chữ U nhỏ; Level_02: có nhánh cụt).

- [ ] **Step 3: Kiểm tra tay (Play mode):**
  1. Phím mũi tên và WASD → bóng trượt tới tường, vệt sơn theo bóng.
  2. Vuốt chuột → cùng kết quả; click không kéo → không di chuyển.
  3. Bấm phím khi bóng đang trượt → bị bỏ qua.
  4. % tăng đúng; sơn kín → panel Win sau ~0.5s, input bị khoá.
  5. Next → Level_02, camera căn đúng; ở level cuối Next → Level_01.
  6. Bấm Restart ngay sau khi ô cuối được sơn (trong 0.5s) → level reset, panel Win **không** hiện lại.
  7. Console không có lỗi.

- [ ] **Step 4: Commit** — `feat: add sample levels, game scene and setup guide`.

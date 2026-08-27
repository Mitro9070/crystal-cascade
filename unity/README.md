# Neon Seven — перенос веб-прототипа в Unity «пиксель в пиксель»

Папка `unity/` содержит:

- `Assets/Textures/…` — готовые текстуры (шары, фон, UI, VFX), см. `Assets/Textures/README.md`.
- `Assets/Scripts/Neon7/…` — C#-порт всей логики и презентации прототипа.
- этот файл — точная спецификация метрик, цветов, таймингов и правил сборки сцены.

Прототип-эталон: `src/routes/index.tsx` + `src/styles.css` + `src/lib/game.ts` + `src/lib/audio.ts`.

---

## 0. Проект и Canvas

- Unity 2022.3 LTS+, URP или Built-in. Ориентация: **Portrait** (Auto Rotation off).
- Canvas: `Screen Space - Overlay`, **CanvasScaler**:
  - UI Scale Mode: `Scale With Screen Size`
  - Reference Resolution: `420 × 900`
  - Screen Match Mode: `Match Width Or Height`, `Match = 0` (ширина ведущая).
  Так 1 юнит UI == 1 CSS-px прототипа при ширине контента 420.
- Корневой `Root` (RectTransform, stretch на весь экран) — фон, см. §1.
- Внутри `Content`: anchor top-center, `width = 420` (это `max-w-md` = 28rem = 448px, но контент
  прототипа ограничен ещё `px-3` → внутренняя ширина **384**), `padding: left/right 12, top 16, bottom 20`.
  Практически: `Content` = 420 шириной, дочерний `Column` = 396 (420−2×12) шириной,
  Vertical Layout Group с `spacing` из §2.

Все размеры ниже — CSS-px = UI-юниты при reference 420×900.

---

## 1. Фон (`game-root`)

Три слоя (снизу вверх), все растянуты на экран:

1. Вертикальный градиент `#080924` (верх) → `#040212` (низ).
2. Радиальный «глоу» вверху: центр `(50%, −10%)`, радиус `120% × 80%` от экрана,
   цвет `#443081` alpha `0.55` → прозрачный на 60% радиуса.
3. Радиальный глоу снизу-слева: центр `(10%, 110%)`, радиус `90% × 60%`,
   цвет `#005B65` alpha `0.35` → прозрачный на 60%.

Реализация: `Assets/Textures/Backgrounds/bg_deep_indigo.jpg` на `RawImage` (stretch, Preserve Aspect off)
— текстура уже содержит все три слоя.

Шрифт: закруглённый гротеск (SF Pro Rounded / Nunito / Baloo 2). Веса: 800 (`font-black` → 900).
Цвет текста `#F3F4FC` (`ink`), приглушённый `#B3B6CB` (`ink-dim`).

---

## 2. Раскладка колонки (сверху вниз)

| Блок | Высота | Отступ сверху | Радиус | Примечание |
| --- | --- | --- | --- | --- |
| Header (заголовок + 2 кнопки 44×44) | 44 | 0 | 16 (кнопки) | gap между кнопками 8 |
| Scoreboard (3 колонки: Счёт / Рекорд / До подъёма) | 62 | 12 | 24 | padding 16/12, glass |
| Ряд Next + переключатель режимов | 56 | 12 | 24 | glass, слева шар 48 + next-шар 32, gap 12 |
| Aim-строка `верт. v · гор. h` | 22 | 12 | 999 | по центру, 11px bold |
| Board (квадрат) | = ширина (396) | 8 | 32 | glass, `overflow: hidden` |
| Подсказка внизу | 16 | 12 | — | 11px, `ink-dim`, пульсация §6.7 |

**В прототипе больше ничего нет.** Ряда бустеров (`BOMB / RAIN / SWAP`), второй строки подсказки
и любых иных панелей в вебе не существует — не добавлять, иначе колонка не сходится по высоте
(44+12+62+12+56+12+22+8+396+12+16 = 652 при контенте 900−16−20 = 864, остаток — свободный отступ снизу).

Правила, чтобы текст не обрезался и не наезжал (как на скриншотах Unity-сборки):
- Все `TMP_Text`: `Overflow = Overflow`, **Auto Size выключен**, `Wrapping = Disabled` для
  однострочных лейблов (заголовок, `NEXT`, лейблы счёта) и `Enabled` только для подсказки/баннера.
- Header: заголовок в `Horizontal Layout Group` с `flexibleWidth = 1`, кнопки — `minWidth 44`,
  `LayoutElement.flexibleWidth = 0`. Никаких `Content Size Fitter` на заголовке.
- Подсказка внизу — **один** объект текста в самом низу колонки; не размещать её поверх поля.
- Кириллица и знаки `↻ 🔊 ∞ ×` должны быть в TMP-атласе шрифта; иначе вместо них появятся
  пустые прямоугольники, а строки визуально «съедаются». Для иконок mute/restart лучше
  использовать спрайты, а не глифы шрифта.

Типографика:
- `NEON SEVEN` — 20px, weight 900, tracking −0.02em; слово `SEVEN` цветом `n2` `#00DFF2`.
- подзаголовок «линия ровно N — взрыв» — 11px `ink-dim`.
- лейблы в scoreboard — 10px, uppercase, letter-spacing 0.1em, `ink-dim`.
- значения — 18px weight 900: Счёт — `n2` `#00DFF2`, Рекорд — `ink`, До подъёма — `n5` `#FF8648`.

### Glass-панель (`glass`)

- fill `#2B314C` alpha `0.28`
- border 1px `#C5CAF5` alpha `0.18`
- внешняя тень `0 12 40` чёрная alpha `0.45`
- внутренний хайлайт сверху 1px белый alpha `0.12`
- blur фона 18px + saturate 1.5

В Unity: `Assets/Textures/UI/panel_glass.png` как 9-slice `Image` (border 40px) +
для реального blur — URP Renderer Feature «Blit» с downsample/blur в `_GrabTexture`,
либо готовый шейдер `UI/Blur` (Kawase, 2 прохода, radius 18px в экранных пикселях).

### 2.1 Как использовать готовые тайлы (обязательно)

| Тайл | Где | Настройки |
| --- | --- | --- |
| `Backgrounds/bg_deep_indigo.jpg` | `RawImage` на весь экран, самый нижний слой | Sprite/Default, stretch, tint белый |
| `UI/panel_glass.png` | все glass-панели (scoreboard, next, board, Game Over, баннер) | Sprite 2D/UI, 9-slice border 40, tint **белый**, `Image.Type = Sliced`, `Pixels Per Unit Multiplier` не менять |
| `UI/grid_cell.png` | клетка сетки поля, 49 шт. | 9-slice border 8, tint `#FFFFFF` alpha `0.05`, `raycastTarget = false` |
| `Balls/ball_<N>_<name>.png` | `face` в `BallPrefab` | **tint белый** — градиент, блик и тень уже в текстуре |
| `Balls/ball_obsidian.png` | скрытый шар | tint белый, glow выключен |
| `Balls/ball_obsidian_cracked.png` | слой `cracks` поверх обсидиана | tint белый, blend Screen/Additive, alpha 0.95 |
| `VFX/vfx_shockwave_ring.png` | кольцо взрыва | материал Additive, tint = цвет цифры |
| `VFX/vfx_spark_glow.png` | искры 6×6 | материал Additive, tint = цвет цифры |

Главное расхождение, которое чаще всего ломает картинку: спрайты шаров **уже окрашены**.
Если дополнительно умножить их на цвет из `Palette.Numbers`, шар становится пересвеченным
и не совпадает с вебом. Цвет из палитры применяется только к `glow`, VFX и тексту счёта
(см. `BallView.Refresh`).

Радиусы панелей заданы текстурой `panel_glass.png` (32px при 512). Для панелей с радиусом 24
уменьшать 9-slice border до 30, для кнопок 44×44 с радиусом 16 — до 20; не масштабировать
панель непропорционально, иначе углы «плывут» относительно веб-версии.


---

## 3. Поле 7×7

- `SIZE = 7`, клетка = `boardWidth / 7` (при 396 → **56.571**). Не округлять — прототип использует %.
- Позиция шара: `x = col * cell`, `y = -row * cell` (row 0 сверху), размер шара = `cell × cell`.
- Линии сетки: 1px `#FFFFFF` alpha `0.05` по каждой клетке (текстура `UI/grid_cell.png`, 9-slice).
- Подсветка колонки прицела: прямоугольник `cell` × высота поля, вертикальный градиент сверху вниз,
  прозрачный на 85% высоты:
  - обычный: `#00DFF2` alpha `0.18`
  - когда прицел даёт взрыв: `#46DA89` alpha `0.26`
  - плюс inset glow белый alpha 0.08
  - переезд к новой колонке — 150 мс linear.
- «Призрак» посадки: круг `cell × cell`, обводка 2px dashed белая alpha `0.30`, тот же переход 150 мс.

### 3.1 Обязательная иерархия RectTransform

Проблема «шары живут отдельно от клеток» возникает, если `ballsLayer`, `gridLayer` и `fxLayer`
имеют разные anchors, offsets или масштаб. Использовать только такую структуру:

```text
BoardRoot (396×396, pivot 0.5/0.5, RectMask2D)
├── GridLayer   (stretch/stretch, offsets 0, scale 1)
├── AimColumn   (anchor/pivot top-left)
├── LandingGhost (anchor/pivot top-left)
├── BallsLayer  (stretch/stretch, offsets 0, scale 1)
└── FxLayer     (stretch/stretch, offsets 0, scale 1)
```

- На `GridLayer`, `BallsLayer`, `FxLayer` **не должно быть** `GridLayoutGroup`,
  `Horizontal/VerticalLayoutGroup`, `ContentSizeFitter` или ненулевого padding.
- `BoardRoot` может находиться во внешнем `VerticalLayoutGroup`, но его квадратный размер должен
  задавать `AspectRatioFitter (1:1)` или `LayoutElement.preferredHeight = width`.
- Не назначать `ballsLayer = BoardRoot`: shake двигает `BoardRoot`, а локальные координаты шаров
  должны вычисляться внутри отдельного слоя с нулевыми offsets.
- Код `BoardView.EnsureReady()` принудительно завершает Layout до вычисления `cell`, растягивает
  все три слоя одинаково и только потом строит сетку/создаёт шары.
- У всех объектов `Ball` root: anchor/pivot top-left, размер `cell×cell`, позиция
  `(col×cell, -row×cell)`. Внутренние `face`, `glow`, `cracks`, `num` центрируются кодом;
  не задавать им Layout-компоненты.

---

## 4. Шар (`ball` / `ball-face` / `ball-num`)

Диаметр видимой сферы = `cell × 0.88` (inset 6% с каждой стороны). Слои внутри:

1. Основа: линейный градиент 160°, от `mix(color, white 8%)` к `mix(color, black 45%)`.
2. Тёмное пятно: радиальный `#131428` alpha 0.55, центр (70%, 78%), радиус 80%.
3. Блик: радиальный белый alpha 0.78, центр (32%, 26%), радиус 38%×32%.
4. Glow (вне сферы): `0 0 18` цвет alpha 0.60 + `0 0 42` цвет alpha 0.35.
5. Inner shadow снизу `0 −6 14` чёрный 0.35; inner highlight сверху `0 4 10` белый 0.22.

Цифра: по центру, weight 800, размер `clamp(14.4, 0.042*width, 24)` → при 396 ≈ **16.6**,
цвет `#070815`, тень `0 1 0` белая alpha 0.45.

Палитра цифр (sRGB hex):

| N | Цвет | Hex |
| --- | --- | --- |
| 1 | жемчужно-белый | `#EBF3FC` |
| 2 | cyan | `#00DFF2` |
| 3 | изумруд | `#46DA89` |
| 4 | янтарь | `#F7CC4B` |
| 5 | коралл | `#FF8648` |
| 6 | малина | `#FF53A5` |
| 7 | ультрамарин | `#9658FF` |
| — | обсидиан | `#14151F` (градиент `#272737` → `#010203`, без glow, тень `0 6 18` чёрная 0.5) |
| — | лава (трещины) | `#F9AD26` |

Трещины (`cracks == 1`): 3 радиальных луча цветом лавы поверх сферы, blend `Screen`,
свечение 6px, opacity 0.95 (текстура `Balls/ball_obsidian_cracked.png`).

Готовые спрайты: `Assets/Textures/Balls/ball_<N>_<name>.png` 512×512 с альфой;
цифра рисуется поверх TextMeshPro.

Точная иерархия `BallPrefab`:

```text
Ball (RectTransform; без Image и без Layout Group)
├── Glow   (Image, Raycast Target off)
├── Face   (Image, Preserve Aspect on, Raycast Target off)
├── Cracks (Image, Raycast Target off)
└── Num    (TextMeshProUGUI, alignment Middle Center)
```

Слои должны идти именно в таком порядке: цифра последней, иначе `Face` или `Cracks` перекроют её.
У `Num`: margins `0`, Auto Size off, Wrapping off, Overflow, один символ, scale `1,1,1`.
Не помещать `Num` внутрь `Face`, если на `Face` есть Animator/scale: squash применяется к сфере,
а координаты текста должны оставаться строго в центре ячейки. `BallView.Init()` теперь сам
выставляет anchors/pivot/position/size для всех четырёх слоёв.

---

## 5. Тайминги (мс) — соблюдать точно

| Событие | Значение | Кривая |
| --- | --- | --- |
| Падение шара (движение `top`) | 190 | cubic-bezier(0.34, 1.4, 0.64, 1) |
| Сдвиг по колонке (`left`) | 140 | ease |
| Squash при посадке | 260 | cubic-bezier(0.3, 1.6, 0.5, 1); кадры: 0% scale(1.25, 0.62), 55% scale(0.90, 1.12), 100% scale(1,1) |
| Пауза до появления шара | 30 | — |
| Взрыв (`pop`) | 300 | 0% scale 1 → 35% scale 1.28 brightness 2.2 → 100% scale 0.1 opacity 0 |
| Гравитация после взрыва | 230 | та же кривая падения |
| Кольцо (`ring`) | 520 | ease-out, scale 0.2 → 2.6, opacity 1 → 0, border 2px белый 0.85 + glow 24px цветом |
| Искры (7 шт) | 700 | cubic-bezier(0.2,0.8,0.3,1); угол `i/7*2π + rand`, дистанция `40..110 px`, 6×6 px, scale → 0.2 |
| Задержка звука pop i-го шара | `i * 45` | — |
| Плавающий счёт | 900 | 0% (−50%, 0) scale 0.7 op 0 → 25% (−50%,−12) scale 1.15 op 1 → 100% (−50%,−58) scale 1 op 0 |
| Баннер волны / Board Clear | 1500 | 0% scale 0.6 op 0 → 18% scale 1.06 → 75% scale 1 → 100% scale 1.10 op 0 |
| Подъём дна | 260 | та же кривая падения |
| Shake | wave1 260 / wave2 340 / wave3 420 | амплитуда 4 / 8 / 14 px; кадры 20% (−amp, +2, −0.5°), 45% (+amp, −2, +0.5°), 70% (−0.6amp, +1) |
| Пульсация подсказки | 2600, loop | opacity 0.55 ↔ 1 |
| Пауза после Board Clear | 700 | — |

---

## 6. Правила геймплея (эталон = `GameLogic.cs`)

1. Поле 7×7. `row 0` — верх, `row 6` — низ.
2. Шар с числом `N` детонирует, если непрерывная вертикальная **или** горизонтальная линия
   занятых ячеек через него имеет длину ровно `N`.
3. Скрытый (обсидиановый) шар цифры не имеет и не детонирует. Соседи по кресту от взорвавшихся
   шаров получают 1 урон: целый → треснутый → открывается случайная цифра 1–7.
4. Волна: найти все совпадения → взрыв + урон соседям → удалить → гравитация → проверить снова.
   Очки волны: `count * 100 * 2^(wave-1)`. При `wave >= 3` — баннер `WAVE n • x2^(n-1)`.
5. Пустое поле → `BOARD CLEAR! +70,000` и +70000 очков.
6. Генерация шара: 15% — скрытый, иначе равномерно 1–7.
7. Старт: в каждой колонке 1–2 шара снизу, из них 25% скрытые.
8. Классика: каждые 5 сброшенных шаров поле поднимается на 1 клетку, снизу вставляется ряд
   из 7 скрытых шаров. Если хоть один шар уже в `row 0` — Game Over. Дзен: без подъёма.
9. Game Over также если все 7 колонок заполнены.
10. Рекорд хранится в `PlayerPrefs["neon7-best"]` (в прототипе `localStorage`).

## 6.1 Управление

- Нажатие/движение пальцем по полю выбирает колонку: `col = floor((x - boardLeft) / boardWidth * 7)`, clamp 0..6.
- Смена колонки — звук `move` + вибро 6 мс.
- Отпускание пальца — сброс шара в выбранную колонку.
- Ввод игнорируется, пока идёт анимация каскада (`busy`) или после Game Over.

---

## 7. Звук (процедурный, `Sfx.cs`)

Гамма волн (Гц): `523.25, 587.33, 659.25, 783.99, 880, 1046.5, 1174.66, 1318.5`.

| Событие | Состав |
| --- | --- |
| move | sine/triangle 320 Гц, 50 мс, gain 0.05 |
| drop | sine 180 Гц 120 мс gain 0.18 + шум 120 мс gain 0.08 bandpass 500 Гц |
| pop(wave, i) | `f = scale[wave-1] * (1 + 0.03i)`: sine f 350 мс 0.16 + triangle 2f 180 мс 0.05 + шум 180 мс 0.05 bandpass 2400 Гц, задержка `0.045i` |
| crack | шум 200 мс 0.12 bandpass 900 + saw 140 Гц 200 мс 0.06 |
| rise | saw 90 Гц 500 мс 0.10 + шум 400 мс 0.10 bandpass 300 |
| clear | 6 нот `scale[i]*2`, sine 500 мс 0.12, шаг 70 мс |
| over | 440 → 350 → 260 → 180 Гц, saw 500 мс 0.12, шаг 130 мс |

Общий lowpass 6000 Гц. Огибающая: атака 12 мс до gain, далее экспоненциальный спад до 0.0001 за `dur`.

Haptics: move 6 мс, drop 14/16 мс, комбо `[18,30,24]`, подъём `[10,20,10]`, board clear `[30,40,30,60]`.
На Android — `Vibrator.vibrate(long[])` через `AndroidJavaObject`, на iOS — `UIImpactFeedbackGenerator`.

---

## 8. Порядок сборки сцены

1. Скопировать `unity/Assets` в проект (`Textures` + `Scripts`).
2. Создать `Scene: Game`, Canvas по §0.
3. Повесить `GameController` на пустой объект, заполнить ссылки:
   `boardRoot`, `ballPrefab`, `fxRingPrefab`, `fxSparkPrefab`, `floatTextPrefab`, тексты счёта,
   кнопки mute/restart, панель Game Over, баннер.
4. `BallPrefab` собрать строго по иерархии §4. Все четыре визуальных слоя — прямые дети `Ball`;
   `Glow`, `Face`, `Cracks`, `Num`, именно в таком sibling-порядке.
5. `Sfx` — на том же объекте, `AudioSource` с `playOnAwake = false`.
6. Проверка «пиксель в пиксель»: скриншот WebGL-прототипа при 420×900 и Unity Player 420×900,
   наложить в режиме Difference — расхождения по сетке/шарам не более 1 px.

---

## 9. Файлы кода

| Файл | Роль |
| --- | --- |
| `Scripts/Neon7/Palette.cs` | все цвета и метрики прототипа как константы |
| `Scripts/Neon7/GameLogic.cs` | чистая логика (порт `src/lib/game.ts`) |
| `Scripts/Neon7/Easing.cs` | cubic-bezier кривые из CSS |
| `Scripts/Neon7/BallView.cs` | визуал шара: цвет, glow, цифра, трещины, squash, pop |
| `Scripts/Neon7/BoardView.cs` | сетка, позиционирование, подсветка колонки, призрак, VFX, shake |
| `Scripts/Neon7/GameController.cs` | цикл хода, каскады, подъём дна, режимы, счёт, ввод |
| `Scripts/Neon7/Sfx.cs` | процедурный звук + haptics |

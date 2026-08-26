# Number7 — Unity texture pack (Cyber-Glass / Neon Neumorphism)

Готовые тайлы/материалы для импорта в Unity. Все файлы лежат в репозитории
(`unity/Assets/Textures/...`) — можно просто скопировать папку `unity/Assets` в проект Unity.

## Состав

### Balls/ (шары, 512×512, PNG с альфой)
| Файл | Цифра | Цвет |
| --- | --- | --- |
| `ball_1_white.png` | 1 | жемчужно-белый |
| `ball_2_cyan.png` | 2 | электрический лазурный |
| `ball_3_emerald.png` | 3 | изумрудный |
| `ball_4_amber.png` | 4 | янтарно-жёлтый |
| `ball_5_coral.png` | 5 | оранжево-коралловый |
| `ball_6_magenta.png` | 6 | малиново-розовый |
| `ball_7_violet.png` | 7 | фиолетовый ультрамарин |
| `ball_obsidian.png` | — | скрытый шар (обсидиан) |
| `ball_obsidian_cracked.png` | — | обсидиан с лавовыми трещинами (1 урон) |

Цифра сверху рисуется текстом (TextMeshPro) — так одна текстура переиспользуется без ре-экспорта.

### Backgrounds/
- `bg_deep_indigo.jpg` (1536×1536) — фон сцены, глубокий индиго/графит с неоновым свечением.

### UI/
- `panel_glass.png` — панель frosted glass, использовать как 9-slice sprite.
- `grid_cell.png` — пустая ячейка сетки 7×7, 9-slice.

### VFX/
- `vfx_shockwave_ring.png` — вспышка/кольцо взрыва (Additive).
- `vfx_spark_glow.png` — частица искры (Additive).

## Настройки импорта в Unity

Шары / UI / VFX:
- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single` (для `panel_glass` и `grid_cell` — задать Border для 9-slice)
- Alpha Is Transparency: ✔
- Generate Mip Maps: ✖ (для UI), ✔ (для 3D-квадов)
- Filter Mode: `Bilinear`, Compression: `High Quality`
- Max Size: 512

Фон:
- Texture Type: `Default`, Wrap Mode: `Clamp` (или `Repeat`, если тайлится), Max Size: 2048

Материалы:
- Шары: `Sprites/Default` или URP `Sprite Lit` + Emission-подсветка под цвет цифры.
- VFX: shader `Particles/Standard Unlit` с Blend Mode = `Additive`.

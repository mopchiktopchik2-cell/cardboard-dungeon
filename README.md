# Картонное подземелье

Top-down рогалик в духе Archero, собранный из плоских картонных вырезок. Маг сам стреляет по ближайшему врагу, игрок управляет движением, рывком и активными навыками, а между волнами выбирает перки и проходит постановочные переходы по подземелью.

![Меню игры](docs/menu.png)

## Скачать

Готовая сборка для Windows x64 лежит в разделе **Releases**:

- `CardboardDungeon_v1.0_win64.zip`
- распакуйте архив и запустите `CardboardDungeon.exe`;
- установка не требуется.

![Бой с боссом](docs/boss.png)

## Управление

| Клавиша | Действие |
|---|---|
| `WASD` / стрелки | движение |
| Автоматически | атака ближайшего врага |
| `1`–`5` | активные навыки |
| `Space` | рывок с короткой неуязвимостью; пропуск анимации перехода |
| `Esc` | пауза |
| `R` | быстрый перезапуск на экране финала |

Совет: держите дистанцию, следите за телеграфами атак и не тратьте рывок впустую. Навыки сбрасывают перезарядку при входе в новую комнату.

## Что внутри

![Комната подземелья](docs/screenshot.png)

- 12 комнат: бои, сокровищница, привал и финальный босс.
- 12 активных навыков: огненный шар, щит, ледяная нова, цепная молния, лечение, яд, метеор и другие.
- 19 перков: урон, скорость атаки, пробитие, мультивыстрел, вампиризм, взрывные смерти, дополнительные слоты навыков.
- 8 видов врагов и босс **Лич-Переплёт** с несколькими фазами.
- Выбор из трёх карт после боя, отдельные экраны сокровищницы и привала.
- Постановочные переходы: маг идёт к двери, дверь открывается, камера уводит в следующую комнату, оттуда вылезают новые враги.
- Процедурная графика в стиле картонных вырезок и процедурный звук.
- Сохранение лучшего забега между запусками.

## Технологии

- **Unity 6000.6.6f1**, 2D, Built-in Render Pipeline.
- **UGUI**, старый Input Manager, Windows Standalone x64 (Mono).
- Вся сцена собирается кодом через `RuntimeInitializeOnLoadMethod`; в проекте одна сцена `Assets/Scenes/Game.unity`.
- Графика: `tools/gen_art.py` (Python + Pillow), 85 PNG.
- Звук: `tools/gen_audio.py`, 26 WAV без сторонних сэмплов.
- Шрифты: PT Sans и PT Serif (ParaType, SIL OFL 1.1).

## Сборка из исходников

1. Установите Unity **6000.6.6f1** с модулем Windows Build Support.
2. Откройте папку проекта в Unity Hub.
3. Для сборки из командной строки:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "C:\путь\к\cardboard-dungeon" `
  -executeMethod Cardboard.EditorTools.ProjectSetup.BuildWindows `
  -logFile build.log
```

Результат: `Build/CardboardDungeon.exe`.

### QA-сборка

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "C:\путь\к\cardboard-dungeon" `
  -executeMethod Cardboard.EditorTools.ProjectSetup.BuildQA `
  -logFile qa-build.log
```

Запуск: `Build\qa\CardboardDungeonQA.exe -qa`.
QA-клавиши: `F1`/`F2` — убить всех врагов, `F3` — лечение, `F4` — выдать перк, `F5` — скриншот.

### Self-tests

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "C:\путь\к\cardboard-dungeon" `
  -executeMethod Cardboard.EditorTools.ProjectSetup.RunSelfTests `
  -logFile selftest.log
```

Проверяются данные контента, иконки, спрайты, шрифты, аудио и базовый баланс. Текущий результат: **395 проверок, 0 ошибок**.

## Структура

```
Assets/
  Editor/           настройка проекта, сборка, self-tests
  Resources/
    Art/            85 сгенерированных PNG
    Audio/          26 сгенерированных WAV
    Fonts/          PT Sans / PT Serif + OFL
  Scenes/Game.unity
  Scripts/
    Combat/         снаряды, навыки, опасности, союзники
    Core/           игра, данные, комнаты, камера, аудио, self-tests
    Entities/       игрок, враги
    UI/             весь UGUI-интерфейс
    Visual/         картонные акторы, эффекты, мировые полоски здоровья
tools/              генераторы графики и звука
docs/               скриншоты для README
```

## Лицензии

Сторонние игровые ассеты не используются. Подробности по графике, звуку и шрифтам: [ASSETS_LICENSES.md](ASSETS_LICENSES.md).

## Версия

`v1.0` — первый публичный релиз. История изменений: [CHANGELOG.md](CHANGELOG.md).

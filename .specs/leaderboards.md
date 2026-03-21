# Leaderboards

## Обзор

Глобальный лидерборд в стиле классических аркадных игр. Результаты сохраняются в облаке (Unity Leaderboards) и доступны всем игрокам. Аутентификация — анонимная (Unity Authentication), скрыта от игрока.

## Архитектура

```
GameScreen (контроллер)
  └── LeaderboardService (бизнес-логика)
        ├── IAuthProxy → UnityAuthProxy (анонимная аутентификация)
        └── ILeaderboardProxy → UnityLeaderboardProxy (облачный лидерборд)
```

Proxy-интерфейсы позволяют заменить реализацию без изменения бизнес-логики.

Имя игрока хранится как metadata в записи лидерборда (JSON: `{"playerName":"..."}`)

## Экран Game Over

### Флоу: имя уже сохранено (PlayerPrefs)

```
Game Over
  → показать score
  → loading
  → submit score с сохранённым именем
  → fetch top-10 + позиция игрока
  → показать лидерборд (результат игрока подсвечен жёлтым)
  → показать кнопку "Change Name"
  → если игрок не в top-10 — показать его позицию отдельной строкой
```

### Флоу: имя не сохранено (первый запуск)

```
Game Over
  → показать score
  → loading
  → fetch top-10 (без submit — имени ещё нет)
  → показать лидерборд + форму ввода имени одновременно
  → игрок вводит имя → нажимает Submit
  → сохранить имя в PlayerPrefs
  → submit score → refresh лидерборд (теперь с результатом игрока)
  → показать кнопку "Change Name"
```

### Флоу: смена имени

```
Игрок нажимает "Change Name"
  → показать форму ввода (pre-fill текущим именем) поверх лидерборда
  → игрок вводит новое имя → Submit
  → обновить PlayerPrefs
  → re-submit score с новым именем → refresh лидерборд
```

## Поле ввода имени

- Без ограничения длины (полное имя, не 3-буквенный код)
- Валидация: не пустое после trim
- Сохраняется в `PlayerPrefs` под ключом `"PlayerName"`

## Лидерборд

- Top-10 записей
- Каждая строка: ранг, имя, очки
- Текущий игрок подсвечен жёлтым цветом
- Если игрок не в top-10 — его позиция показана отдельно под списком

## Зависимости

- `com.unity.services.core`
- `com.unity.services.authentication`
- `com.unity.services.leaderboards`

## Конфигурация

- `GameData.LeaderboardId` — ID лидерборда в Unity Dashboard (по умолчанию `asteroids_highscores`)
- В Unity Dashboard: Sort Order = Descending, Update Type = Keep Best

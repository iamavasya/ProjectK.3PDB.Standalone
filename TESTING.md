# Тестове середовище (контейнери) та E2E

Застосунок — це десктоп-обгортка (ASP.NET Core API + Angular SPA, оновлення через Velopack,
запуск Chrome `--app`). Для тестування додано **headless container mode**, який запускає той самий
код без браузера/Velopack, плюс **mock-update** режим для детермінованого E2E оновлення.

## Режими запуску (env)

| Змінна | Призначення |
|---|---|
| `PROJECTK_CONTAINER=true` | Headless: сервить SPA + API на `0.0.0.0:5220`, без запуску браузера, Velopack і авто-зупинки по heartbeat. |
| `PROJECTK_MOCK_UPDATE=true` | Замінює реальний `UpdateService` на мок і вмикає test-ендпоінти `/api/testctl/*`. |
| `MOCK_CURRENT_VERSION` | Початкова «поточна» версія (дефолт `1.0.0`). |
| `MOCK_NEW_VERSION` | «Доступна» версія оновлення (дефолт `2.0.0`). |
| `MOCK_RELEASE_NOTES` | Тестовий ченджлог для нової версії (markdown). |

## Docker Compose

### E2E / mock-update контейнер (повний застосунок, оновлення змокано)
```bash
docker compose --profile e2e up --build
# застосунок: http://localhost:5220
```
Перевизначення сценарію:
```bash
MOCK_NEW_VERSION=3.1.0 MOCK_RELEASE_NOTES="## Реліз 3.1.0" docker compose --profile e2e up --build
```

### Dev-середовище (hot reload)
```bash
docker compose --profile dev up
# API: http://localhost:5220 (dotnet watch)
# SPA: http://localhost:4200 (ng serve)
```

## Test-control API (лише при `PROJECTK_MOCK_UPDATE=true`)

| Метод / шлях | Тіло | Дія |
|---|---|---|
| `GET  /api/testctl/state` | — | Поточний стан моку. |
| `POST /api/testctl/version` | `{ "version": "1.0.0" }` | Задати «поточну» версію. |
| `POST /api/testctl/available` | `{ "version": "2.0.0" }` | Задати доступне оновлення (null — вимкнути банер). |
| `POST /api/testctl/release-notes` | `{ "version": "2.0.0", "notes": "..." }` | Задати ченджлог для версії. |
| `POST /api/testctl/simulate-update` | `{ "toVersion": "2.0.0", "notes": "..." }` | Одразу «застосувати» оновлення: поточна версія = toVersion. |

> Механізм показу ченджлогу — клієнтський: фронтенд порівнює `localStorage['appVersion']`
> з `GET /api/update/current-version`; якщо відрізняються — тягне `release-notes/{version}`
> і відкриває діалог «Що нового».

## E2E (Playwright)

Тести в `e2e/`. Конфіг за замовчуванням сам піднімає `--profile e2e` контейнер
(`webServer`), тож достатньо:
```bash
cd e2e
npm install
npm run install-browsers   # перший раз
npm test
```
Якщо контейнер підіймаєте вручну (або хочете вимкнути авто-старт):
```bash
docker compose --profile e2e up --build -d
cd e2e && PLAYWRIGHT_NO_WEBSERVER=1 npm test
```
Інший базовий URL: `PLAYWRIGHT_BASE_URL=http://host:port`.

**Сценарій** ([e2e/tests/update-changelog.spec.ts](e2e/tests/update-changelog.spec.ts)):
стара версія `1.0.0` → банер оновлення `2.0.0` → «Завантажити» → «Перезапустити» →
(мок застосовує апдейт, фронтенд перезавантажується) → відкривається ченджлог із заданим
тестовим значенням, футер показує `v2.0.0`.

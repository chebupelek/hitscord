# Hitscord

Hitscord — веб-платформа для общения и учебного взаимодействия. В репозитории находятся React-клиент и ASP.NET Core API: серверы с каналами и ролями, личные чаты, расписание и пары, уведомления, загрузка файлов и обмен событиями в реальном времени через SignalR.

## Состав проекта

| Каталог / файл | Назначение |
| --- | --- |
| `ap/` | React 18-клиент на Create React App, Redux и Ant Design. |
| `hitscord_new/hitscord_new/` | API на .NET 8, Entity Framework Core, SignalR и Quartz. |
| `nginx/` | Конфигурация reverse proxy, HTTPS и раздачи собранного клиента. |
| `docker-compose.yml` | Полный production-подобный запуск всех сервисов. |

API использует PostgreSQL для постоянных данных, Redis для кэша и сессий, MinIO для файлов и ClamAV для проверки загружаемых файлов. Quartz выполняет фоновые задачи: работу с расписанием/посещаемостью и очистку старых сообщений и файлов.

## Быстрый запуск через Docker

1. Создайте `.env` в корне проекта. Не добавляйте его в Git: в нём содержатся пароли и ключ подписи JWT.
2. Задайте переменные окружения из таблицы ниже.
3. Выполните:

   ```powershell
   docker compose up --build -d
   ```

4. После запуска сайт доступен через Nginx. В текущей конфигурации домен — `hitscord.site`; для локальной разработки замените `server_name` в `nginx/conf.d/default.conf` и настройте сертификаты либо временно используйте HTTP-конфигурацию.

Полезные команды:

```powershell
docker compose logs -f hitscord
docker compose down
```

`down` останавливает контейнеры, но сохраняет именованные тома PostgreSQL, MinIO и сборки клиента. Для удаления данных требуются отдельные, намеренные действия с томами.

## Переменные окружения

| Переменная | Для чего нужна |
| --- | --- |
| `DB_HOST`, `DB_USER`, `DB_PASSWORD` | Подключение к PostgreSQL. |
| `DB_NAME_FIRST`, `DB_NAME_SECOND` | Имена основной и токенной баз данных. |
| `JWT_SECRET` | Секрет подписи JWT; используйте длинное случайное значение. |
| `REDIS_HOST`, `REDIS_PORT`, `REDIS_PASSWORD` | Подключение к Redis и пароль Redis. |
| `MINIO_ENDPOINT`, `MINIO_USER`, `MINIO_PASSWORD`, `MINIO_BUCKET` | Хранилище загружаемых файлов. |
| `CLAMAV_HOST`, `CLAMAV_PORT` | Адрес антивирусного сервиса. |
| `API_BASE_URL` | Публичный базовый URL API, используемый сервисом файлов. |
| `REACT_APP_API_BASE_URL` | Базовый URL API для React-сборки. |
| `CORS_ALLOWED_ORIGINS` | Зарезервировано для конфигурации разрешённых источников клиента. |

## Локальная разработка

Требования: .NET SDK 8, Node.js 18+, Docker (для инфраструктуры) и PostgreSQL/Redis/MinIO/ClamAV, доступные по указанным переменным.

Запустить API:

```powershell
dotnet run --project .\hitscord_new\hitscord_new\hitscord_new.csproj
```

Запустить клиент в отдельном терминале:

```powershell
Set-Location .\ap
npm install
npm start
```

Перед запуском API применяет миграции Entity Framework Core и создаёт базовые системные роли, если их ещё нет. Поэтому учётная запись PostgreSQL должна иметь права на создание и изменение схемы.

## Docker Compose: подробный сценарий запуска

Ваш вариант запуска корректен. Команды можно выполнять раздельно:

```powershell
docker compose build
docker compose up
```

Первая команда собирает образы API и клиента, вторая создаёт и запускает все контейнеры. Обычно их объединяют в одну команду, которая делает то же самое:

```powershell
docker compose up --build
```

Для фонового запуска добавьте `-d`:

```powershell
docker compose up --build -d
```

При первом запуске Compose создаёт сеть `hitscord_network`, тома `pgdata`, `minio_data`, `frontend_build`, PostgreSQL, Redis, MinIO, ClamAV, API, сборщик клиента и Nginx. Сервис `minio-create-bucket` один раз создаёт bucket и затем завершает работу — это штатное поведение.

Проверить состояние и диагностику:

```powershell
docker compose ps
docker compose logs -f hitscord
docker compose logs -f nginx
```

После изменения только кода API или клиента снова используйте `docker compose up --build`. Изменения `.env` подхватываются после пересоздания контейнеров: `docker compose up -d --force-recreate`. `docker compose down` останавливает стек, но не удаляет данные PostgreSQL и MinIO из именованных томов.

## Роли и права

Права выдаются не пользователю напрямую, а роли сервера. Пользователь получает итоговое разрешение, если хотя бы одна из его ролей на сервере его даёт. Создание и изменение роли требует серверного права `CanCreateRole`; работа с каналами и их настройками — `CanWorkChannels`.

Серверные права настраиваются запросом `PUT /api/roles/settings`: в теле передаются `serverId`, `roleId`, `setting` и `add`. `setting` — значение `SettingsEnum`, а `add` включает (`true`) или выключает (`false`) право.

| Серверное право | Что разрешает |
| --- | --- |
| `CanChangeRole` | Назначать и снимать роли у участников. |
| `CanWorkChannels` | Создавать, удалять и настраивать каналы и группы. |
| `CanDeleteUsers` | Удалять/блокировать участников сервера. |
| `CanMuteOther` | Менять mute-статус других пользователей в голосовом канале. |
| `CanDeleteOthersMessages` | Удалять чужие сообщения. |
| `CanIgnoreMaxCount` | Подключаться к голосовому каналу сверх лимита участников. |
| `CanCreateRole` | Создавать, менять и удалять роли. |
| `CanCreateLessons` | Создавать привязки занятий расписания к голосовым каналам. |
| `CanCheckAttendance` | Просматривать посещаемость занятий. |
| `CanUseInvitations` | Создавать и отзывать приглашения сервера. |
| `CanCheckGrades` | Просматривать оценки в учебных каналах; дополнительно требуется право канала `CanCreateTask`. |

Права конкретного канала изменяются специальными маршрутами `PUT /api/channel/settings/change/*`. Тело `ChannelRoleDTO` содержит `channelId`, `roleId`, `type` и `add`; `add=true` выдаёт право роли, `false` отзывает его.

| Право канала | Что разрешает |
| --- | --- |
| `CanSee` | Видеть канал и его содержимое. |
| `CanWrite` | Отправлять сообщения в текстовый канал. |
| `CanWriteSub` | Писать в подканалы/ветки. |
| `CanUse` | Пользоваться служебным/подканалом. |
| `CanJoin` | Подключаться к голосовому каналу. |
| `Notificated` | Получать уведомления канала. |
| `CanCreateTask` | Создавать задания в учебном канале; сервис также выдаёт `CanSee`. |
| `CanJoinQueue` | Вставать в очередь. |
| `CanTakeQueue` | Брать следующего пользователя из очереди. |

Сервис повторно проверяет членство в сервере, существование ролей/каналов и права на каждой операции. Поэтому изменение прав начинает влиять на HTTP и SignalR-вызовы сразу после обновления данных и кэша.

## Сообщения и SignalR

Подключение создаётся к `/api/wss`. Хаб защищён `[Authorize]`: передайте действующую cookie `access_token` (браузерный клиент) или параметр `access_token` в query string. После подключения сервер автоматически добавляет соединение в личную группу `user:{userId}`.

Перед работой с ресурсом подпишитесь на его группу. Сервер проверяет, что пользователь состоит в чате/сервере или имеет `CanSee`/`CanUse` для канала:

```javascript
await connection.invoke("JoinChat", chatId);
await connection.invoke("JoinServer", serverId);
await connection.invoke("JoinChannel", channelId);
```

При уходе вызывайте симметричные `LeaveChat`, `LeaveServer` и `LeaveChannel`. Подписка управляет только получением событий; каждая команда ниже всё равно проходит проверку прав в `MessageService`.

| Вызов хаба | Назначение |
| --- | --- |
| `SendMessageChannel`, `UpdateMessageChannel`, `DeleteMessageChannel` | Создать, изменить и удалить сообщение канала. DTO содержат канал, сообщение и текст. |
| `SendMessageChat`, `UpdateMessageChat`, `DeleteMessageChat` | Те же действия для личного чата. |
| `AddReactionChannel` / `RemoveReactionChannel`, `AddReactionChat` / `RemoveReactionChat` | Добавить или удалить реакцию. |
| `SendTask`, `UpdateTask`, `DeleteTask` | Создать, изменить или удалить задание в учебном канале. |
| `SendSolution`, `UpdateSolution`, `DeleteSolution`, `SendGrade` | Работа с решениями заданий и оценками. |
| `InQueue`, `OutQueue`, `TakeQueue`, `RemoveQueue` | Управление очередью в канале очереди. |
| `Vote`, `Unvote`, `GetVote` | Голосование; результат `GetVote` приходит вызывающему в событии `VoteData`. |
| `SeeMessage` | Отмечает сообщение прочитанным. |

Подпишитесь на событие `Error`: все ошибки бизнес-логики хаб отправляет только вызывающему клиенту в виде `{ Code, ObjectFront, MessageFront }`; непредвиденная ошибка приходит как `{ Message: "Internal server error" }`. Остальные события и названия сообщений формируются `MessageService`; для некоторых команд (например, удаление/реакции) событие отправляется группе канала или чата.

## HTTP API и Swagger

Swagger UI доступен по адресу `/api/swagger` при запуске через Nginx (внутри API — `/swagger`). Спецификация OpenAPI: `/api/swagger/v1/swagger.json`.

Документация Swagger автоматически показывает:

- назначение каждой операции и её раздел;
- query-параметры, их типы и назначение;
- тело запроса и описание полей DTO в блоке `Schema`;
- типовые ответы `200`, `400`, `401` и `500`;
- необходимость авторизации.

Описания аргументов и фактические ограничения (длины, форматы, диапазоны и проверки прав) собраны в [RequestContractDocumentation.cs](hitscord_new/hitscord_new/Swagger/RequestContractDocumentation.cs). Это прокомментированный единый источник для Swagger: при изменении валидации обновляйте правило рядом с ней.

Большинство операций требуют авторизации. API читает JWT из HTTP-only cookie `access_token`; также настроена схема Bearer для клиентов, передающих токен в заголовке `Authorization`. В Swagger UI включена передача cookies (`withCredentials`).

Основные группы API: `auth`, `server`, `channel`, `chat`, `files`, `friendship`, `notifications`, `roles`, `schedule` и `admin`. Все маршруты за Nginx имеют префикс `/api`, например `GET /api/server/get/List`.

Ошибки бизнес-логики возвращаются в виде объекта:

```json
{
  "object": "имя поля или ресурса",
  "message": "описание ошибки для клиента"
}
```

## SignalR

Хаб реального времени находится по `/api/wss` (внутри приложения — `/api/wss`) и требует авторизации. После подключения клиент может подписываться на группы:

- `JoinChat(chatId)` / `LeaveChat(chatId)`;
- `JoinServer(serverId)` / `LeaveServer(serverId)`;
- `JoinChannel(channelId)` / `LeaveChannel(channelId)`.

Хаб поддерживает отправку, изменение и удаление сообщений, реакции, голосования, задания и решения, оценки, очередь и отметки о прочтении. Полный контракт аргументов определён в DTO из `hitscord_new/hitscord_new/Models/socket/` и методах `SignalR/ChatHub.cs`.

## Проверка изменений

```powershell
dotnet build .\hitscord_new\hitscord_new\hitscord_new.csproj
Set-Location .\ap
npm test -- --watchAll=false
```

## Важные особенности

- У API нет глобального префикса маршрутов; префикс `/api` добавляет Nginx при публикации.
- Файлы проходят через MinIO и ClamAV, поэтому для тестирования загрузки должны быть доступны оба сервиса.
- В production Nginx ожидает сертификаты Let's Encrypt по путям из `nginx/conf.d/default.conf`.

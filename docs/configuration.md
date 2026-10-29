# Конфигурация и безопасность

[← Документация](README.md) · Раздел для C# / Blazor

Настройки в `appsettings.json`, ключи и секреты, рекомендации по безопасности.

## Настройки

Настройки необязательны: без них работают глобус, карта, контейнеры гео-данных и PWA,
участники хранятся в памяти, а адреса ищутся в OpenStreetMap без ключа. Ключ Google нужен,
только если адреса должен искать Google.

Библиотека читает секцию `ZealousMindedPeopleGeo`, если сервисы зарегистрированы с
конфигурацией:

```csharp
builder.Services.AddZealousMindedPeopleGeo(builder.Configuration);
```

```json
{
  "ZealousMindedPeopleGeo": {
    "GoogleMapsApiKey": "",
    "NominatimUrl": "",
    "EnableGeocoding": true,
    "Map": {
      "Projection": "EqualEarth",
      "CentralMeridian": 0,
      "DefaultLatitude": 20,
      "DefaultLongitude": 0,
      "DefaultZoom": 1
    }
  }
}
```

| Ключ | По умолчанию | Что делает |
|---|---|---|
| `GoogleMapsApiKey` | пусто | Необязательный ключ Google Maps Platform с включённым Geocoding API. Задан — адреса для `GeoDataParticipantForm` ищет Google, не задан — Nominatim (OpenStreetMap). Карта и глобус ключ не используют. |
| `NominatimUrl` | пусто | Сервер Nominatim для поиска без ключа Google. Пусто — публичный `https://nominatim.openstreetmap.org/`: бесплатно, но не больше запроса в секунду на приложение. Для большой нагрузки укажите свой сервер или платного поставщика с API Nominatim. |
| `EnableGeocoding` | `true` | `false` выключает поиск адресов совсем: форма сообщает, что он недоступен, и точку не добавляет. |
| `Map:Projection` | `EqualEarth` | Проекция 2D-карты: `EqualEarth` или `Equirectangular`. |
| `Map:CentralMeridian` | `0` | Центральный меридиан 2D-карты в градусах, от −180 до 180. |
| `Map:DefaultLatitude` | `20` | Широта начального центра 2D-карты. |
| `Map:DefaultLongitude` | `Map:CentralMeridian` | Долгота начального центра 2D-карты. |
| `Map:DefaultZoom` | `2` | Начальное приближение 2D-карты; `1` — мир целиком. |
| `Map:ClusterPoints` | `true` | Сливать близкие маркеры 2D-карты в группы (см. [Группы близких точек](components.md#группы-близких-точек)). |
| `Map:ClusterRadius` | `24` | Расстояние между центрами маркеров в пикселях (0–200), ближе которого они сливаются. |

Не заданные ключи берут значения по умолчанию из таблицы, есть секция `Map` или нет: без
центра и зума карта открывается на весь мир. Параметры `CommunityMapComponent`
(`Projection`, `CentralMeridian`, `Zoom`, `CenterLatitude`, `CenterLongitude`,
`ClusterPoints`, `ClusterRadius`) важнее настроек.

Глобус настраивается параметрами компонентов и панелью
настроек (см. [Настройки глобуса](components.md#настройки-глобуса)), длительность кэша — в коде (см. [Кэширование](services.md#кэширование)).

### Ключи и секреты

Не храните ключ Google в `appsettings.json` в репозитории.
Для разработки подойдут user secrets, на сервере — переменные окружения (двоеточие в
имени ключа заменяется на `__`):

```bash
dotnet user-secrets init
dotnet user-secrets set "ZealousMindedPeopleGeo:GoogleMapsApiKey" "<ключ>"

export ZealousMindedPeopleGeo__GoogleMapsApiKey="<ключ>"
```

Ключ используется только на сервере: компоненты не передают его в браузер. В Blazor
WebAssembly настройки загружает сам браузер, поэтому ключ из них виден пользователям.

## Безопасность

Рекомендации по безопасности:

1. **API ключи** - Храните ключи Google вне репозитория (см. [Ключи и секреты](#ключи-и-секреты))
2. **Валидация** - Контейнеры не сохраняют точки, которые не проходят `GeoPoint.Validate()` (см. [Валидация](geo-data.md#валидация))
3. **Доступ и частота** - Библиотека не ограничивает, кто и сколько точек добавляет; для формы на открытой странице задайте `OnSubmitting` (см. [Кто может добавлять точки](geo-data.md#кто-может-добавлять-точки))
4. **Свой HTTP API** - Если точки приходят через ваш API, закройте его аутентификацией; CORS разрешайте только своим доменам
5. **HTTPS** - Используйте HTTPS для всех запросов

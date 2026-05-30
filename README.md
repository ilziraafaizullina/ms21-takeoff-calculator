# MS-21 Takeoff Calculator

Настольное приложение на C# / .NET 8 / WPF для расчета взлетных характеристик МС-21.

Приложение работает как справочник по подготовленным CSV-таблицам: оно не интерполирует данные и не рассчитывает скорости по формулам. Все значения V1, VR, V2 и THS берутся из готовых файлов в `data/prepared`.

## Как запустить

```powershell
dotnet build MS21TakeoffCalculator.sln
dotnet run --project MS21TakeoffCalculator.csproj
```

Путь к папке с данными задается в `appsettings.json`:

```json
{
  "DataDirectory": "data"
}
```

Если нужно использовать другую папку, укажите абсолютный путь:

```json
{
  "DataDirectory": "D:\\MS21\\data"
}
```

Внутри этой папки должны быть подпапки `raw` и `prepared`.

## Какие данные используются

| Файл | Назначение |
| --- | --- |
| `data/raw/airports.csv` | Справочник аэропортов |
| `data/raw/runways.csv` | Справочник ВПП |
| `data/raw/wind_slope_corrections.csv` | Поправки к длине ВПП на ветер и уклон |
| `data/prepared/speed_*_prepared.csv` | Готовые таблицы скоростей V1 / VR / V2 |
| `data/prepared/ths_flaps_*_prepared.csv` | Готовые таблицы THS |

## Порядок работы в приложении

1. Выберите `ICAO`.
   Доступные варианты берутся из `airports.csv`.

2. Выберите `RWY`.
   Список полос фильтруется по выбранному аэропорту из `runways.csv`.

3. Введите `WIND / KT`.
   Это встречный ветер в узлах. Он влияет на скорректированную длину ВПП.

4. Выберите `CONF`.
   Доступные варианты:
   - `FLAPS 1 + F`
   - `FLAPS 2`

5. Выберите `QNH HPA`.
   В текущей версии это выбор барометрической высоты таблицы:
   - `0`
   - `1000`
   - `2000`

6. Выберите `OAT C`.
   Список температур берется из выбранного файла скоростей.

7. Выберите `TOW (T)`.
   Список масс фильтруется по выбранным `CONF`, `QNH`, `OAT`, ВПП и ветру.

8. Выберите `CG`.
   Список центровок берется из таблицы THS для выбранной конфигурации.

9. Нажмите `COMPUTE`.

## Выбор файла скоростей

Файл скоростей выбирается по `CONF` и `QNH HPA`.

| CONF | QNH HPA | Файл |
| --- | ---: | --- |
| `FLAPS 1 + F` | `0` | `speed_1f_0ft_prepared.csv` |
| `FLAPS 1 + F` | `1000` | `speed_1f_1000ft_prepared.csv` |
| `FLAPS 1 + F` | `2000` | `speed_1f_2000ft_prepared.csv` |
| `FLAPS 2` | `0` | `speed_2_0ft_prepared.csv` |
| `FLAPS 2` | `1000` | `speed_2_1000ft_prepared.csv` |
| `FLAPS 2` | `2000` | `speed_2_2000ft_prepared.csv` |

## Расчет скорректированной длины ВПП

Перед поиском скоростей рассчитывается `CorrectedRunwayLengthM`.

Из выбранной ВПП берутся:

```text
LengthM
SlopePercent
```

Из `wind_slope_corrections.csv` берутся коэффициенты:

```text
HeadwindPerKtM
DownSlopePerPercentM
UpSlopePerPercentM
```

Если точной длины ВПП нет в `wind_slope_corrections.csv`, используется ближайшая длина из этого файла.

Поправка на ветер:

```text
WindCorrectionM = HeadwindKt * HeadwindPerKtM
```

Если уклон положительный:

```text
SlopeCorrectionM = SlopePercent * UpSlopePerPercentM
CorrectedRunwayLengthM = LengthM + WindCorrectionM - SlopeCorrectionM
```

Если уклон отрицательный:

```text
SlopeCorrectionM = abs(SlopePercent) * DownSlopePerPercentM
CorrectedRunwayLengthM = LengthM + WindCorrectionM + SlopeCorrectionM
```

Если уклон `0`, фактически остается только поправка на ветер:

```text
CorrectedRunwayLengthM = LengthM + WindCorrectionM
```

После этого приложение выбирает ближайшее значение `CorrectedRunwayLengthM`, которое реально есть в выбранном файле скоростей.

Например, если расчет дал `3550`, а в таблице есть максимум `3500`, для поиска будет использована строка `3500`.

## Поиск скоростей

В выбранном `speed_*_prepared.csv` строка ищется по:

```text
OATC
CorrectedRunwayLengthM
MaxTakeoffWeightT
```

Из найденной строки берутся:

```text
V1Kt -> V1
VRKt -> VR
V2Kt -> V2
```

## Поиск THS

Файл THS выбирается по `CONF`.

| CONF | Файл |
| --- | --- |
| `FLAPS 1 + F` | `ths_flaps_1f_prepared.csv` |
| `FLAPS 2` | `ths_flaps_2_prepared.csv` |

В таблице THS:

```text
строка = WeightT
колонка = CG{значение CG}
```

Например:

```text
TOW = 79.3
CG = 25.0
```

значит приложение ищет строку `WeightT = 79.3` и колонку `CG25.0`.

## Тестовый набор входных данных

Можно проверить приложение таким набором:

```text
ICAO: UUEE
RWY: 06C
WIND / KT: 0
QNH HPA: 0
CONF: FLAPS 1 + F
OAT C: -10
TOW (T): 79.3
CG: 25.0
```

Что произойдет внутри:

1. Для `UUEE / 06C` из `runways.csv` берется:

```text
LengthM = 3550
SlopePercent = 0
```

2. Для длины `3550` ближайшая строка в `wind_slope_corrections.csv` - `3500`.

3. При ветре `0 kt`:

```text
WindCorrectionM = 0 * 17 = 0
CorrectedRunwayLengthM = 3550 + 0 = 3550
```

4. В файле скоростей ближайшая доступная длина к `3550` - `3500`.

5. По `CONF = FLAPS 1 + F` и `QNH HPA = 0` выбирается файл:

```text
data/prepared/speed_1f_0ft_prepared.csv
```

6. В нем находится строка:

```csv
OATC,CorrectedRunwayLengthM,MaxTakeoffWeightT,V1Kt,VRKt,V2Kt,LimitationCode
-10.0,3500,79.25,143,150,158,7
```

`79.25` округляется до `79.3`, поэтому выбранный `TOW = 79.3` соответствует этой строке.

Ожидаемые скорости:

```text
V1 = 143
VR = 150
V2 = 158
```

7. Для THS используется файл:

```text
data/prepared/ths_flaps_1f_prepared.csv
```

В нем берется строка `WeightT = 79.3` и колонка `CG25.0`.

Ожидаемый THS:

```text
THS = -3.29
```

## Ошибки

Если обязательные поля не заполнены, приложение показывает:

```text
All parameters are required.
```

Если подходящая строка в таблицах не найдена:

```text
DATA NOT FOUND
```

Если ICAO отсутствует в справочнике:

```text
N/A DATA
```

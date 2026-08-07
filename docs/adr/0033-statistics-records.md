# ADR-0033 — İstatistik ve rekorlar: kalıcı sezon arşivi + elde-çizili kariyer grafikleri

- **Durum:** Kabul edildi — indi (M24a–h: sezon arşivi + track record'lar + RECORDS ekranı [PROFILES / THIS SEASON /
  ALL-TIME / HALL OF FAME] + kariyer profili zenginleştirme + Canvas grafikleri)
- **Tarih:** 2026-08-07

## Bağlam
Faz 3 kabuğu M19–M23 indi; sıradaki milestone **M24 — istatistik & rekorlar** (ADR-0022 "tarih DB + hall of fame"
buraya erteledi). Kesin mimari zemin (3 keşif ajanı): **rekor altyapısı YOK** — tek çapraz-sezon veri `CareerRollover.Apply`
bitmiş sezonu skaler sayaçlara indiriyor (`DriverCareer` + `Team.ChampionshipsWon/RaceWins`) ve **tam `SeasonResult`'ı
atıyor**; geçmiş sezonlar **reconstruct edilemez** (`Reconstruct` yalnız mevcut sezonu replay eder, `_seasonIndex`
transient). Bu sayaçlar kalıcı ama UI'da neredeyse gösterilmiyordu (`EntityDetailViewModel` yalnız `Career.Wins` +
takım `ChampionshipsWon`). Grafik kütüphanesi yok; tek elde-çizili görsel M23 pist haritası (`TrackOutline` +
`Canvas`/`Path`). Kullanıcı iki kapsam kararı verdi: **(1) tam kapsam** (yıl-yıl hall of fame + çok-sezon trend +
pist rekoru için **yeni kalıcı arşiv**); **(2) elde-çizili Canvas grafikleri**.

## Karar

### Katman A — Yeni kalıcı sezon arşivi (Domain + Career + Persistence)
Geçmiş sezonlar reconstruct edilemediğinden, arşiv **hesaplanamaz — kalıcı olmalı.**
- **Domain (M24a):** `SeasonRecord { Year; DriversChampionId; ConstructorsChampionId; Drivers; Constructors }` +
  `DriverSeasonLine` + `ConstructorSeasonLine` + `TrackRecord { CircuitId; BestLapSeconds; DriverId; Year }`.
  `Carset`'e `SeasonHistory` + `TrackRecords` (varsayılan boş → inert → byte-özdeş; değer-tipli → byte-stable).
- **Career (M24b):** `SeasonArchive.Append(next, played, season)` — saf/deterministik: bitmiş sezonu bir
  `SeasonRecord`'a indirir (yıl = oynanan takvimin ilk turunun yılı; şampiyonlar; final tablolar) ve **`next.SeasonHistory`'e
  ekler**; her devrenin en hızlı yarış turunu `TrackRecords`'a birleştirir (mevcut rekorla kıyaslar, daha hızlıyı tutar).
  Üretilen listeler **stabil sıralı** (mevcut kayıtlar yerinde, yeni devreler takvim sırasında) → byte-stable.
  `LiveCareer.RollToNextSeason` bunu `CareerRollover.Apply`'dan **hemen sonra**, oynanan sezonun (`SeasonStart`) takvimiyle
  çağırır. Arşiv **birikir, sezon sınırında TEMİZLENMEZ** (oyuncu strateji/komut günlüklerinin aksine).
- **Persistence (M24c):** `CareerState` `SeasonHistory` + `TrackRecords` (varsayılan boş) yakalar/geri-yükler —
  additive + inert-default + **authoritative-restore** (Roster/Boards deseni). Arşiv carset'te birikip
  `SeasonStart`'a bindiğinden `SaveSession`'la kaydedilir; boş → byte-özdeş.

### Katman B — Rekorlar ekranı (UI, golden-safe)
Yeni `NavKey.Records` (NAVIGATION bölümü, Database'ten sonra) + `RecordsViewModel`/`RecordsView`, dört sekmeli
`TabControl`:
- **PROFILES (M24d):** her pilot kariyer-puanına göre sıralı (Yarış/Galibiyet/Podyum/Pole/Şampiyonluk/Puan) + seçili
  profil kartı. **`EntityDetailViewModel` zenginleştirildi** ("CAREER RECORD" bölümü: tam sayaçlar + türev win-rate;
  takım için şampiyonluk + yarış galibiyeti) — paylaşımlı panel olduğundan **Drivers/Database ekranlarına da yansır**
  ("yalnız Career.Wins" boşluğu kapandı).
- **ALL-TIME (M24e):** kümülatif top-beş leaderboard'lar (en çok şampiyonluk/galibiyet/pole/podyum/puan/hızlı tur —
  pilot; konstrüktör titles / takım yarış galibiyeti).
- **HALL OF FAME (M24e):** `Current.SeasonHistory`'den yıl-yıl şampiyonlar (en yeni önce) + `Current.TrackRecords`'dan
  devre-başı en hızlı tur (M:SS.mmm). Sezon tamamlanana dek boş-durum.
- **THIS SEASON (M24f):** `live.Results`/`Standings`/`Qualifying`'den bu-sezon liderleri (galibiyet/podyum/pole/hızlı
  tur/emeklilik), takım-eşi kafa-kafaya (sıralama/yarış/puan) ve elde-çizili puan-ilerleme grafiği.

### Katman C — Elde-çizili grafikler (M24f/g)
Harici kütüphane yok (CSP-safe zaten değil ama tutarlılık için). Yeni saf `ChartScale` (M23 `TrackOutline` analoğu):
`(min,max,width,height,pad)` → veri noktası → canvas (Y ters çevrilir; dejenere eksen düşük kenara toplanır → sıfıra
bölme yok) — **deterministik, view'sız unit-testlenebilir.** VM her çizgiyi **SVG path string'i** olarak üretir →
**platform-bağımsız kalır** (`StreamGeometry` VM'de kurmak render platformu ister ve testlenemezdi); view string'i
`Path.Data`'ya bağlar (Avalonia tip-dönüştürücüsü render bağlamında parse eder). İki grafik: sezon-içi kümülatif puan
ilerlemesi (top-6, THIS SEASON) ve çok-sezon kariyer trendi (seçili pilotun sezon-sezon puanı, PROFILES detayına
gömülü; <2 sezon → gizli).

## Sonuç
- **Golden + byte-özdeşlik korundu:** M24 sim'e dokunmaz (`RaceSimulator`/`SeasonSimulator` değişmez) → golden
  trivially. `SeasonArchive.Append` **yalnız** `LiveCareer.RollToNextSeason`'da → headless `WorldSweep`/`RunProgressed`/
  Model-B parite dokunulmaz. Yeni `Carset` + `CareerState` alanları inert-default `= []` → hiç sezon tamamlamamış
  kariyer byte-özdeş; değer-tipli → dolu round-trip byte-stable. **784 test yeşil, warning-clean.**
- **Reconstruction:** arşiv append-only, `next`→`SeasonStart`→`CareerState`→restore (reconstruct EDİLMEZ);
  `Reconstruct`/`ApplyToSeasonStart` arşive yazmaz → çift-say/kayıp yok; live = reload.
- **Mimari gardiyan:** rekor tipleri `LTF.Domain`; `SeasonArchive` + `CareerState` `LTF.Career`; ekran + `ChartScale`
  `LTF.App`. Hepsi izinli yön.
- **Katman haritası:** Domain (`SeasonRecord`/`TrackRecord` + `Carset.SeasonHistory`/`TrackRecords`) · Career
  (`SeasonArchive.Append` + `CareerState` persist) · App (`LiveCareer.RollToNextSeason` çağrısı; `RecordsViewModel`/
  `RecordsView`; `ChartScale`; `EntityDetailViewModel` zenginleştirme).
- **Sınırlamalar / ertelenenler:** uzun-emekli bir şampiyonun adı roster'dan düşerse hall of fame id'ye düşer (arşiv
  kompakt tutuldu, ad saklanmadı); pist haritası hâlâ tek generic outline. **Kapsam dışı:** haber/söylenti motoru
  (ADR-0022, ayrı), scout/scouted%/rapor (M14/M22), Practice istatistikleri (kariyerde practice koşulmuyor), grafik
  animasyonları/interaktif drill-down.

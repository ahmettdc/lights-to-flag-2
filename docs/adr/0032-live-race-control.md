# ADR-0032 — Canlı yarış kontrolü: stepping-seam + kayıtlı komut günlüğü + tam görsel katman

- **Durum:** Kabul edildi — indi (M23d–j: sekmeli ekran + pist haritası + sektör/telsiz + `RaceStepper` refactor +
  canlı komut kanalı + interaktif canlı yarış)
- **Tarih:** 2026-08-07

## Bağlam
ADR-0031 ile M23a–c indi (deterministik yarışın replay'i olan zamanlama kulesi + oyuncu yarış-öncesi başlangıç-
bileşimi). Kullanıcı **ertelenen kısmı** istedi ve iki kapsam kararı verdi: **(1) tam görsel katman** — sekmeli
Race/Qualifying ekranı + şematik pist haritası + hareketli araç işaretçileri + sektör renkleri/delta + telsiz
feed'i; ve **(2) gerçek yarış-içi canlı komutlar** — yarışı adım-adım durdurup BOX / PUSH / EXTEND / MANAGE verme
(golden-riskli **stepping-seam**, önceden-taahhüt edilmiş güvenli seçenek değil).

Zemin (keşiften):
- **Telemetri intra-lap POZİSYON taşımaz** — yalnız tur-sonu snapshot (sıralama + zaman farkları + sektör süreleri).
  İşaretçiler `GapToLeader ÷ tur-süresi → tur-fraksiyonu` ile **yaklaşıklanır**; tek generic outline var (mockup SVG
  path), **per-circuit geometri yok** (`Circuit` yalnız skaler; `Circuit.BaseLapTimeSeconds` referans tur).
- **Quali** her tur koşuluyor ama `RunRound`'da atılıyordu (`RoundOutcome` yalnız pole id). Deterministik →
  reconstruct edilebilir. `LiveCareer` per-tur yalnız `RaceResult` saklıyordu.
- **Practice kariyerde HİÇ koşulmuyor** (`setups` her zaman null) → Practice sekmesi ayrı bir gameplay sistemi ister
  → **kapsam dışı**.
- **`RaceSimulator.Run` monolitik** (tek lap-loop) ve pit turları ÖN-planlıydı. Canlı komut için stepping-seam
  gerekir; golden ancak "komut yok → byte-özdeş" + parity-test ile korunabilir.

## Karar

### Golden-güvenlik disiplini (fazların sıralama gerekçesi)
Tek kanonik yarış digest'i (`RaceSimulator.Run` @ seed 2024) ve byte-özdeş kayıt asla bozulmaz. Fazlar riske göre
sıralandı: **görsel katman (M23d–f)** saf replay/projeksiyon + quali re-capture → sim kod-yolu değişmez → golden
trivially korunur (önce iner). **Stepper refactor (M23g)** golden loop'a dokunur → **izole, parity-gated, komut
kanalı yok** (en yüksek-riskli iş, tek başına faz). **Canlı komutlar (M23h–i)** ancak parity kilitlendikten sonra;
her komut inert-default + "boş → golden birebir" testiyle korunur.

### M23d — Sekmeli ekran + Qualifying sekmesi (golden-safe)
`RaceWeekendView` merkez bölgesi `TabControl`'e sarıldı (**RACE / QUALIFYING**). Quali **kayıt formatı değişmeden**
saklanıyor: `RoundOutcome`'a `QualifyingResult Qualifying` eklendi (`SeasonSimulator.RunRound`/`StartRound` zaten
`quali` hesaplıyor — atmayı bıraktı, döndürüyor); `LiveCareer._qualifying` `_results`'a **paralel** liste, `RunAndEvolve`'da
doldurulur, `Reconstruct` + `RollToNextSeason`'da temizlenir. **Diske yazılmaz** — standings/RaceResult gibi
`(SeasonStart, seed, Date)`'ten reconstruct edilir → save formatı değişmez. Sekme: grid pozisyonu · pilot+takım ·
Part (Q1/Q2/Q3) · `BestLap` · sektörler.

### M23e — Şematik pist haritası + hareketli işaretçiler (golden-safe)
Yeni `TrackOutline` (LTF.App, saf C#): mockup Bézier path'inden **ön-örneklenmiş polyline** (kümülatif yay-uzunluğu)
+ `PointAtFraction(double)` (platform path-length API'si yok). Outline **hem** çizilir **hem** işaretçi konumlandırır
→ tutarlı. `RaceWeekendViewModel` her araç için `frac = GapToLeader ÷ referenceLap → PointAtFraction(-frac)` → (x,y);
takım-renkli, oyuncu aracı vurgulu (`TrackMarkerViewModel`). Canvas-konumlu `ItemsControl` `ReflectionBinding`'le
bağlanır (derlenmiş binding attached-property'yi çözemiyor). **Saf replay projeksiyonu — motor verisi yok, golden'a
dokunmaz.** İşaretçi konumu broadcast-stili zaman-farkı yaklaşıklaması (kayıtlı pozisyon değil).

### M23f — Sektör renkleri/delta + telsiz feed'i (golden-safe)
`Project`'e kadar session-best + per-driver personal-best sektör defteri tutulur; tower sektör hücreleri **mor**
(session-best) / **yeşil** (personal-best) / nötr. Olay feed'i **telsiz-stili** panele dönüştü (`RaceEventKind`'dan
türetilen zengin metin). İkisi de `Telemetry.Laps[0..CurrentLap]` / `Events`'in saf projeksiyonu. **İş parçacığı
notu:** VM-tarafı fırçalar `ImmutableSolidColorBrush` (thread-safe) olmalı — mutable `SolidColorBrush` thread-affine
`AvaloniaObject`, statik alanda paralel testleri düşürür.

### M23g — Yarış motoru stepping-seam refactor (golden-RİSKLİ, izole, parity-gated)
`RaceSimulator.Run`'ın kurulum + lap-loop'u içiçe **`RaceStepper`**'a taşındı: ctor (RNG stream'leri, `cars`,
`ApplyStart`), `AdvanceLap()` (mevcut loop gövdesi **bir tur**; `state`/`neutralLapsLeft` + RNG'ler alan), `Finish()`
(`Classify`). `Run` artık `new RaceStepper(...)` + `while AdvanceLap()` + `Finish()` — **aynı kod, aynı sıra →
byte-özdeş.** Kritik disiplin: **tek implementasyon** (ikinci bir lap-body yazmak drift üretir → kaçınıldı). Parity
testi (stepper-sürülen == monolitik-referans == pinned golden hash) + değişmeyen `The_canonical_race_matches_the_golden_digest`
birlikte kapıdır. Bu faz komut kanalı **içermez**.

### M23h — Canlı komut kanalı + kayıtlı komut günlüğü (golden-riskli, reconstruction-tutarlı)
- **Domain:** `enum RaceCommandKind { BoxThisLap, PushMode, ExtendStint, ManageTyres }` + `RaceCommand { int Round;
  int Lap; string DriverId; RaceCommandKind Kind }` + `Carset.PlayerRaceCommands` (varsayılan boş → inert →
  byte-özdeş; değer-tipli).
- **Sim:** `RaceStepper`/`Run` opsiyonel komut-günlüğü alır; günlük lap'e indekslenir (`_commandsByLap`) ve her lap
  **field işlenmeden önce** `ApplyCommands(lap)` ile uygulanır. Komut **draw sırasını bozmaz**: mod değişimi (Push/
  Conserve) **saf-aritmetik** (lap-time deltası, rastgele çekiliş yok); `ExtendStint` sonraki planlı pit'i geciktirir
  (`_pitDelay`); `BoxThisLap` aracın **kendi** `PitRng`'inden çekilen pit'ini bu tura çeker (sayı korunur, sadece
  erken). **null/boş günlük → golden birebir** (parity + golden testi kapı).
- **Career:** `SeasonSimulator.StartRound`/`RunRound` carset'in komutlarını `round.Round`'a indirir; yoksa null →
  sezon byte-özdeş (`Run`/`RunProgressed`/`RunCore` imzaları değişmez).
- **Persistence:** `CareerState` `PlayerRaceCommands` yakalar/geri-yükler (additive + inert-default + değer-tipli →
  boşken byte-özdeş, dolu round-trip byte-stable).
- **Reconstruction-tutarlı:** günlük carset-taşımalı (`SeasonStart → Current`), `RollToNextSeason`'da temizlenir →
  **live = reconstruct = rollover** (M23b deseninin zengin hali).

### M23i — İnteraktif canlı yarış (stepping-seam UI) + komut kaydı
- `RaceStepper.Issue(RaceCommand)` bir emri **gelecek** bir lap'e enjekte eder (ctor'un günlükten kurduğu **aynı**
  per-lap indekse). Bir emri hedef-lap **ilerlemeden önce** vermek, onu kurulum-günlüğünde taşımaya **denktir** →
  canlı-sürülen yarış ile sonraki reconstruction'ı **byte-özdeş** kılar. `Snapshots`/`Events` getter'ları büyüyen
  telemetriyi açar.
- `SeasonSimulator.StartRound` kurulumun **tek** implementasyonu (qualify + grid + penaltı + format + bileşim +
  komut) — `RunRound` onu tek-seferde sürer, canlı ekran lap-lap sürer → **drift yok**. `LiveCareer.StartLiveRace`
  sıradaki tur için stepper döndürür.
- **App/UI:** `RootViewModel` `startLive`/`commitLive`'ı bağlar. Ekran canlı moda girer (**RACE LIVE**), View'ın
  timer'ı stepper'ı sürer, pit-duvarı butonları (**BOX / PUSH / EXTEND / MANAGE**, oyuncu sürücüsü başına) emirleri
  kaydeder; bayrakta günlük `ApplyToSeasonStart` ile `Carset.PlayerRaceCommands`'e commit edilir ve kariyer ilerler
  — **otorite sonuç `RunRound(log)`** (Continue'nun `RunAndEvolve`'u), canlı stepper yalnız UX/komut-kaydı. Track map
  / tower / sektör / telsiz **aynı** `Project` projeksiyonunu tüketir (replay ile ortak). Komut vermeden oyna → boş
  günlük → **bugünkü yarış birebir** (golden-safe). Uygulama yarıda kapanırsa yarım günlük commit edilmez (tur
  "sıradaki" kalır, yeniden oynanır).

## Sonuç
- **Golden + byte-özdeşlik korundu:** görsel katman sim'e dokunmaz; stepper refactor byte-özdeş (parity + golden
  hash); komut yok → birebir. **757 test yeşil, warning-clean.**
- **Determinizm:** komut-günlüğü + quali carset/reconstruct-taşımalı → canlı / reconstruct / rollover üç-yönlü parite;
  `CareerSave*` + Model B parite testleri yeşil. Wall-clock yok (timer yalnız View-tarafı; projeksiyon timer'sız test
  edilir).
- **Mimari gardiyan:** görsel + interaktif-stepper UI `LTF.App`; `RaceStepper`/`Issue`/komut-okuma Sim/Career; domain
  alanı `LTF.Domain`. Hepsi izinli yön (motor Avalonia/`LTF.App`'e referans vermez).
- **Katman haritası:** Domain (`RaceCommand` + `RaceCommandKind` + `Carset.PlayerRaceCommands`) · Sim (`RaceStepper`
  komut-günlüğü + `Issue` + `Snapshots`/`Events`; `RaceSimulator.Run` delegasyonu) · Career (`SeasonSimulator.StartRound`
  paylaşımlı kurulum + `RunRound` + `RoundOutcome.Qualifying`; `CareerState` persist) · App (`LiveCareer` `StartLiveRace`/
  `UpcomingRound`/`_qualifying` + sezon-sınırı temizleme; `RootViewModel.CommitLiveRace`; `RaceWeekendViewModel`/
  `RaceWeekendView`: sekmeli ekran + pist haritası + sektör/telsiz + RACE LIVE + pit-duvarı; `TrackOutline`).
- **Ertelendi:** Practice sekmesi (kariyerde practice koşulmuyor → ayrı gameplay sistemi ister), post-race rapor /
  istatistik (= M24 sınırı).

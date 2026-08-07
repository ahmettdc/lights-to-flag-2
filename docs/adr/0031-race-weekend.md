# ADR-0031 — Yarış hafta sonu: canlı zamanlama kulesi (replay) + oyuncu strateji kontrolü

- **Durum:** Kabul edildi — indi (M23a–c kule replay + oyuncu başlangıç-bileşimi + docs; **M23d–j tam görsel
  katman + gerçek canlı komutlar indi → ADR-0032**)
- **Tarih:** 2026-08-07

## Bağlam
Faz 3 kabuğu (M19–M22) + Model B / FIA freeze (ADR-0028 / ADR-0030) sonrası sıradaki roadmap milestone'u
**M23 — yarış hafta sonu / canlı kontrol paneli**. Kullanıcı bu turda iki parça seçti: **(1) canlı zamanlama
kulesi**, **(2) oyuncu strateji kontrolü** (yarış-öncesi başlangıç-bileşimi). **Şematik pist haritası + telsiz +
mid-race canlı komutlar sonraya** ertelendi.

Zemin (keşiften): `RaceResult.Telemetry.Laps[]` zaten **tam tur-tur kayıt** taşıyor — her `LapSnapshot` o turdaki
tam sıralamayı taşır (`CarLapSample`: pozisyon, lidere-fark, öndekiyle-interval, son tur + sektörler, lastik
bileşim/yaş, yakıt, nötralizasyon durumu); `RaceResult.Events[]` tur-damgalı (geçiş/pit/kaza/SC-VSC-RF/emeklilik).
`RaceSimulator.Run` per-car strateji almıyordu (yalnız field-geneli `startingCompound`). `Competitor.Id = driverId`
(oyuncu araçları = oyuncu takımının `DriverIds`).

## Karar

### M23a — Kule = deterministik yarışın replay'i (sıfır sim değişikliği)
Yeni kabuk-içi `RaceWeekend` ekranı, son koşulan turun **deterministik `RaceResult` telemetrisini** tur-tur
oynatır. `SetLap(n)` kule satırlarını `Telemetry.Laps[n-1].Order`'dan (pozisyon, pilot+takım adı, takım-aksan
rengi, gap/interval, lastik bileşim+yaş, pit sayısı), olay feed'ini `Events ≤ n`'den yeniden kurar; bayrağa
varınca final klasman gösterilir. Oynat/duraklat/ileri/geri/başa/sona + hız kontrolleri; oynatmayı View-tarafı
`DispatcherTimer` sürer, projeksiyon (`SetLap`/`AdvanceLap`) timer'sız doğrudan test edilir. **Motor kod-yolu
değişmez → golden dokunulmaz** (replay'de RNG yok; kule yalnızca kaydı okur).

### M23b — Oyuncu başlangıç-bileşimi (opt-in-varsayılan, reconstruction-tutarlı)
- **Domain:** `RaceStrategy { int Round; string DriverId; TyreCompound Compound }` + `Carset.PlayerRaceStrategies`
  (varsayılan boş → inert → byte-özdeş). Değer-tipli (int + string + enum) → kayıtta byte-stable round-trip.
- **Sim:** `RaceSimulator.Run` opsiyonel `IReadOnlyDictionary<string, TyreCompound>? startingCompounds` kazanır;
  araç başlangıç lastiği `startingCompounds?.GetValueOrDefault(id, startingCompound) ?? startingCompound`. **null
  (M23b öncesi tüm çağıranlar, ve açık `null`) → birebir bugün → golden @ seed 2024 dokunulmaz.**
- **Career:** `SeasonSimulator.RunRound` carset'in o-tura ait stratejilerini `{DriverId: Compound}` sözlüğüne
  indirip `Run`'a geçirir; carset hiç taşımıyorsa null → sezon byte-özdeş. `RunRound` zaten carset taşıdığından
  `Run`/`RunProgressed`/`RunCore` imzaları değişmez (sezon-sim imza-plumbing'i yok).
- **Reconstruction-tutarlı:** strateji carset'te yaşar → `SeasonStart → Current` taşınır (R&D bu alana dokunmaz);
  canlı `RunAndEvolve` + `Reconstruct` + sezon-sınırı `RunProgressed` **aynı** stratejileri okur → live =
  reconstruct = rollover. Her bileşim yalnız kendi turunu etkiler (gelecek-tur seçimi geçmiş yarışı değiştirmez).
  Sezon sınırında `RollToNextSeason`'da **temizlenir** (`PlayerRaceStrategies = []`) — her sezon taze seçilir.
- **Persistence:** `CareerState` `PlayerRaceStrategies` yakalar/geri-yükler (additive + inert-default → boşken
  byte-özdeş, dolu round-trip byte-stable; golden kayıt yok → round-trip idempotence korunur).
- **App + UI:** `RootViewModel.SetRaceStrategy(round, driverId, compound)` seçimi `SeasonStart`'a upsert eder
  (diğer oyuncu-mutasyonları gibi `ApplyToSeasonStart` + autosave + ekran yeniden-kurma). RaceWeekend ekranı
  sıradaki (henüz koşulmamış) tur için **strateji paneli** (oyuncu takımının sürücüleri + her biri için Soft/
  Medium/Hard seçici) + **Start Race** (üst-bar Continue'yu tetikler → tur seçilen bileşimle koşar → ekran
  replay'e döner) gösterir. M23a replay'i korunur; gelecek-tur seçimi standings'e no-op ama carset'e inip
  reload'da yaşar.

## Sonuç
- **Golden + byte-özdeşlik korundu:** kule replay (sim yok, deterministik kaydı oynatır); strateji `null`/boşta
  birebir bugün. Golden digest tek kanonik yarış (`RaceSimulator.Run` @ seed 2024) → dokunulmaz. Tek yeni persist
  alanı additive + value-serialize.
- **Determinizm:** strateji carset-taşımalı; seed'li tüm yollarda (canlı / reconstruct / rollover) üç-yönlü parite
  korunur; wall-clock yok.
- **Mimari gardiyan:** kule UI'da (`LTF.App`); strateji-okuma `RaceSimulator` (Sim) / `RunRound` (Career); domain
  alanı `LTF.Domain`. Hepsi izinli yön (motor Avalonia/`LTF.App`'e referans vermez).
- **Katman haritası:** Domain (`RaceStrategy` + `Carset.PlayerRaceStrategies`) · Sim (`RaceSimulator.Run` per-car
  `startingCompounds`) · Career (`SeasonSimulator.RunRound` okuma + `CareerState` persist) · App (`LiveCareer`
  sezon-sınırı temizleme + `RootViewModel.SetRaceStrategy` + `RaceWeekendViewModel`/`RaceWeekendView`: kule replay +
  strateji paneli).
- **İndi (M23 kalanı — tam görsel katman + gerçek canlı komutlar, ADR-0032):** sekmeli Race/Qualifying ekranı
  (quali reconstruct edilir, diske yazılmaz — M23d), şematik pist haritası + hareketli araç işaretçileri (zaman-farkı
  yaklaşıklaması, tek generic outline — M23e), sektör renkleri/delta + telsiz feed'i (M23f), yarış motoru
  stepping-seam refactor (`RaceStepper`, parity-gated — M23g), **gerçek yarış-içi canlı komutlar** (kaydedilmiş
  komut günlüğü + interaktif stepper — M23h/i). Ayrıntı: ADR-0032.
- **Ertelendi:** Practice sekmesi (kariyerde practice koşulmuyor → ayrı gameplay sistemi ister), post-race rapor /
  istatistik (= M24 sınırı).

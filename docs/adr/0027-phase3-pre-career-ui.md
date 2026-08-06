# ADR-0027 — Faz 3 kariyer-öncesi UI (menü · kariyer başlatma · kayıt/yükleme · ayarlar · Quick Race)

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-06

## Bağlam
M19 kalıcı **kabuğu** kurdu ama uygulama doğrudan kabuğa atlıyordu (`App` flagship carset
üzerinde bir `ShellViewModel` kurup `MainWindow`'a koyuyordu). M20, kabuğu saran
**kariyer-öncesi yüzeyi** ekler: ana menü, yeni Takım Patronu kariyer akışı, kayıt/yükleme,
ayarlar ve kariyer dışı bir **Quick Race**. Bu ADR o katmanın mimari kararlarını kaydeder.
Değişmez kısıt: M20 yalnızca UI + kariyer-kurulumudur; deterministik yarış ve golden digest
kımıldamaz.

## Karar

**1. Kök menü↔kabuk görünüm geçişi.** `RootViewModel` tek bir gözlemlenebilir `Content` tutar
ve onu menü katmanı (ana menü / yeni kariyer / yükle / ayarlar / Quick Race) ile oyun-içi
`ShellViewModel` arasında değiştirir — tıpkı kabuğun kendi içerik bölgesi gibi, `Application.
DataTemplates` ile çözülür. Menü view-model'leri hiç `Window`'a dokunmaz; geçişi
`IAppShellController` dikişi üzerinden ister (`RootViewModel` uygular). `App` artık menüde açılır.

**2. Carset keşfi.** I/O App sınırında (`CarsetCatalog`, `SessionLoader`'ı yansıtır:
`carsets/*.json` numaralandırılır, her biri bir kez ayrıştırılıp önbelleğe alınır); saf
projeksiyon Content'te (`CarsetSummary.From(carset)`). İki carset (global-prix + global-prix-2026)
uygulama çıktısına kopyalanır ki carset seçimi gerçek bir tercih olsun.

**3. Kayıt-slot şeması `CareerStore` üstünde — ikinci serileştirici yok.** `CareerStore` düz bir
yol alır; slotlar UI'nın işi. Slotun carset id'si **dosya adında** yaşar
(`{carsetId}__{slot}.json`) çünkü `CareerState` taşımaz; "son kaydedilen" ise dosya damgasıdır
(yalnız gösterim, oyun durumu değil). Böylece `CareerState`/`CareerStore` değişmez ve byte-özdeş
kayıt testleri yeşil kalır. Yükleme, katalogdan taze carset'i okuyup durumu bindirir. Tasarımda
yakalanan bir **hata düzeltildi**: `ShellSession.FromCarset` sezon-ortası bir kaydın tarihini
sezon başına sıfırlıyordu; yeni `ShellSession.Resume(carset, seed, date)` takvimi korur ama
kaydedilen tarihi geri yükler.

**4. Etkileşimli yönetim kurulu hedef müzakeresi.** Yeni `BoardNegotiation` (LTF.Career,
deterministik; `ContractNegotiation`/`RegulationBallot` kalıbında). Birim-bağımsız **notch**
cinsinden çalışır: sahiplik-tabanlı bir tolerans (risk-almaz üyelerle gevşer, hırslı/sabırsız
üyelerle sıkılaşır) + tohumlu küçük bir jitter; ask ≤ tolerans → **kabul**, ≤ 2×tolerans →
**karşı-teklif** (yarı yol), yoksa **ret**. `BoardReview.DefaultObjective` (artık public) çıpadır.
Kariyer-kurulum aritmetiği — `RaceSimulator`/golden yoluna asla değmez; anlaşılan hedefler
`TeamBoardRecord.Objectives` ile sıfır ek kodla round-trip eder.

**5. Ayarlar — zorluk saklanır ama motora bağlanmaz.** `GameSettings` + `SettingsStore` (JSON,
`CareerStore` tarzı: indented, UTF-8 no BOM, string enum). Zorluk **kalıcıdır ve gösterilir**
ama hiçbir kod onu `RulesSet`/`BalanceCoefficients`/`RaceSimulator`/kariyer-kurulumuna okumaz.
Üç kat garanti: (a) `GameSettings` `LTF.App`'te yaşar, mimari gardiyan motorun `LTF.App`'e
referansını yasaklar; (b) `NewCareerService`/`QuickRaceService`/`ShellSession` zorluk argümanı
almaz; (c) yeni kariyerler `SessionLoader.DefaultSeed` kullanır. Golden korunur.

**6. Quick Race motoru yeniden kullanır, yeni motor yok.** `QuickRaceService` test-kanıtlı
minimal yolu izler: `EntryList.Build(carset)` → `RaceSimulator.Run(circuit, grid, rules, balance,
seed)`. `LTF.App`, `LTF.Simulation`'a (LTF.Career üzerinden) transitively ulaşır. Sınıflandırma
yalnız id taşıdığından sonuç ekranı id→pilot/takım **isimlerine join** eder. Deterministik.

**7. NavigationService fabrika dikişi.** `Register(NavKey, Func<object>)` eklenir; fabrikası
olmayan anahtar hâlâ placeholder'a çözülür. Settings ilk **gerçek** in-shell ekrandır; kabuğun
kenar-çubuğu satır seçimini kabuğun sağladığı bir dispatch delegesi üzerinden yönlendirir
(Menüye-Çık host'a gider, gerisi navige eder). Menüye-Çık ayarı açıksa çıkışta otomatik kaydeder.

## Sonuçlar
- Motor projeleri değişmedi → golden digest, byte-özdeş kayıt, sweep ve round-trip testleri M20
  boyunca yeşil (App headless testleri 52 → 96).
- `LTF.App` artık `LTF.Simulation`'ı doğrudan kullanır (Quick Race); mimari gardiyan yalnız
  **motor → UI** referansını yasakladığından ihlal yok (App zaten en üst katman).
- Ertelenen: **interface-scale canlı uygulama** (değer kalıcı; transform binding'i için küçük bir
  ek adım gerekir), özel kariyer tohumu, Linked-budget hedef müzakeresi, gerçek oynanış süresi
  (yeni kalıcı alan → byte-özdeşliği bozar), tam keybinding + canlı-yarış bildirim editörleri, ve
  difficulty/race-speed/audio'nun kendi kilometre taşlarına (M23/M30) bağlanması.

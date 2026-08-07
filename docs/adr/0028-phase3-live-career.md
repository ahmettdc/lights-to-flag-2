# ADR-0028 — Faz 3 kabuk-içi kariyer merkezi (canlı Continue · 5 ekran · tarihli gelen kutusu · sezon-devri)

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-07

## Bağlam
M20 kariyer-öncesi yüzeyi kurdu; kariyere girmek artık oyun-içi **kabuğu** açıyor ama kabuk
**salt-okunurdu**: içerik bölgesi her NavKey için `PlaceholderScreenViewModel` gösteriyor, üst-bar
**Continue** düğmesi düz bir no-op, ve tutulan `ShellSession(Carset, CareerClock, int Seed)`
**değişmez** — hiç ilerlemiyordu. M21 kariyeri **canlı** yapar: oyunun kalbi. Değişmez kısıt:
deterministik yarış (golden digest, `RaceSimulator` @ seed 2024) ve **byte-özdeş kayıt** kımıldamaz.

## Karar

**1. Beş ekran, M20f kayıt-dikişiyle.** Her ekran = `ViewModels/Screens/` altında bir
`ViewModelBase` + `Views/Screens/` altında derlenmiş-binding'li bir `UserControl`, iki dokunuşla
bağlanır: `App.axaml`'de bir `<DataTemplate>` ve `RootViewModel.EnterCareer`'de bir
`navigation.Register(key, factory)`. Fabrikalar **`LiveCareer`'in üstüne kapanır**, böylece bir
Continue'dan sonra o anahtara yeniden navige etmek ekranı güncel durumdan yeniden kurar. Ekranlar
(Paddock Hub · Drivers · Standings · Calendar · Database) yalnız carset/clock/standings'i **yansıtır**
(salt-okunur projeksiyon). `ISessionSnapshot` genişletilmez (Seçenek A) — fabrikalar carset/clock'a
doğrudan kapanır, tıpkı Settings fabrikasının `_services`'e kapandığı gibi. Ekran krom'u yerelleştirme
paketinden (`StringKeys`/`EnglishStrings`); yoğun veri değerleri (Easy/High/isimler) satır-içi.

**2. `LiveCareer` — değişebilir tutucu (App katmanı).** Kabuğun sürdüğü canlı kariyer. Tutar:
`Carset SeasonStart` (sezon açılış carset'i — deterministik yeniden-kurulum tabanı), `CareerClock
Clock`, `int Seed`, biriken `List<RaceResult>`, `Standings` ve evrilen `Carset Current` (M21'de
`SeasonStart`'a eşit — sezon-içi R&D evrimi ertelendi). `Continue()` saati bir sonraki olaya
ilerletir, yarış olayına denk gelince turu koşar, sonucu biriktirir ve `ChampionshipStandings.From`
ile puan durumunu yeniden hesaplar.

**3. Kayıt formatı değişmez — puan durumu yeniden kurulur.** Kayıt yalnız **sezon-başı carset +
tarihi** taşır (M11/M20'deki gibi; `CareerState`/`CareerStore` dokunulmaz). Puan durumu asla
serileştirilmez: `LiveCareer` onu `(SeasonStart, Seed, Date)`'ten **deterministik olarak yeniden
kurar** — geçmişteki her turu sezon-başı carset'ten yeniden koşarak. Böylece sezon-ortası bir kayıt,
oraya canlı ilerlemenin göstereceği tabloyu **byte-değişikliği olmadan** gösterir. `CareerSave*`
byte-özdeşlik testleri yeşil kalır.

**4. Public `SeasonSimulator.RunRound` — davranış-koruyucu çıkarım.** `RunCore`'un tur-gövdesi
(entry list → `QualifyingSimulator` → grid + `GridOrder.WithPenalties` → `RaceFormat` →
`RaceSimulator.Run`) public bir `RunRound`'a çıkarıldı; `RunCore` onu döngüsünde çağırır, çıktı
bit-özdeş. **Kritik incelik:** grid cezaları **sezon-geneli** hesaplanır (`ComponentPenalties.
ForSeason`, tura göre indeksli), tek bir turun carset'inin fonksiyonu değil — bu yüzden `RunRound`
cezayı **parametre olarak alır** (yeniden hesaplamaz), yoksa byte-özdeşlik bozulurdu. `RoundSeed` de
public yapıldı; `LiveCareer` aynı tohumu türetir. Tam bir sezonluk Continue, `SeasonSimulator.Run`'ı
**birebir** üretir (test edilir).

**5. Continue dağıtımı olay-türüne göre; kariyer sonsuz.** `Continue()`, `Clock.Today`'deki her olayı
`Kind`'e göre dağıtır: `RaceWeekend` → tur koş + yarış bildirimi; `ContractDeadline` →
**eylem-gerektiren** bildirim + **duraklat** (Rev 15); `BoardReview` → kurul bildirimi. Sezon
sonunda (SeasonComplete) bir sonraki Continue **devreder** (aşağıda) — kariyer sonsuzdur, bu yüzden
`CanContinue` yalnız bekleyen bir eylemle (Rev 15) bloke olur; üst-bar Continue düğmesi sınırları
geçmek için etkin kalır. Eylem-gerektiren öğe `Acknowledge()` ile temizlenir; hiçbir tur atlanmaz
(yarışlar yine koşar), böylece onaylanan duraklamalarla tam sezon her turu sınıflandırır.

**6. Tarihli gelen kutusu / haber akışı.** `Notification` iki alan kazanır: `DateOnly Date` +
`bool RequiresAction` (ikisi de opsiyonel, mevcut çağıranlar değişmez). Yeni
`CareerNotificationSource` (`IMutableNotificationSource`: `Append`/`Reset`/`Changed`) çalışan
uygulamada `SampleNotificationSource`'un yerini alır; `InboxViewModel` `Changed`'e abone olur ve
yeni öğeleri id'ye göre ekler (gösterilmiş satırların okunma durumunu korur). `CareerNews` bir
dağıtılmış olayı tarihli bir bildirime eşler (yarış→`Race`/derin-bağlantı Standings;
deadline→`Staff`/RequiresAction; kurul→`Board`; yeni sezon→`Press`). Yeniden-kurulum haber üretmez —
yüklenen kariyer bir sonraki Continue'ya kadar boş gelen kutusuyla açılır.

**7. Sezon-devri M18 world-sweep zincirini yeniden kullanır.** Sezon sınırında `RollToNextSeason`,
oynanan sezonun kanonik `SeasonResult`'ını (`SeasonSimulator.Run(SeasonStart, Seed)`) alır ve
**`WorldSweep`'in tam sınır zincirini** birebir koşar: `RelationshipEvolution` → `CareerRollover` →
`DriverProgression` → `DriverRetirement` → `ContractLedger.AdvanceSeason` → `TransferMarket.Resolve`
→ `RegulationChange`. Devrilen carset yeni `SeasonStart`/`Current` olur, saat yeni takvimle yeniden
başlar, sonuçlar temizlenir. Devrilen carset yeni **kayıt tabanı** olur; mevcut `CareerState`
(M18h: dinamik kadro + reserves + DriverIds + regülasyon) onu round-trip ettiğinden yükleme yeni
sezonu **yeniden-devirmeden** sürdürür (kayıt formatı yine değişmez). Yeni motor kodu yok — yalnız
mevcut motor fonksiyonları çağrılır, bu yüzden golden yolu kımıldamaz.

## Sonuçlar
- Motor projeleri **değişmedi** (yalnız `SeasonSimulator`'da davranış-koruyucu çıkarım + iki public
  imza) → golden digest, byte-özdeş kayıt, sweep ve round-trip testleri M21 boyunca yeşil (App
  headless testleri 96 → 129; toplam 613).
- `Notification` iki alan kazandı ama `LTF.App` UI tipidir (kalıcı değil, golden yolunda değil);
  `TreatWarningsAsErrors=true` → warning-clean.
- Tur-seed her sezonda aynı `Seed`'i kullanır (kayıt yalnız carset+tarih+seed taşıdığından yeniden
  kurulum sezon indeksine ihtiyaç duymamalı); sezonlar farklı **çünkü carset devirle evrilir**.
- Ertelenen: sezon-içi R&D/test-günü carset evrimi (`Current` şimdilik `SeasonStart`'a eşit); ADR-0021
  paddock **diyalog/müzakere** paneli ve Database **transfer/scout** eylemleri (M22); hava/lojistik/
  viraj sayısı ve sürücü "Adaptation" (domain modeli yok); çok-yıllı takvim (devrilen sezon aynı
  tarihleri kullanır); sezon-sınırında-tam-kayıt anındaki devir-RNG'sinin sezon-indeksi bağımlılığı
  (küçük, bilinen sınır; golden/byte-özdeşlik yollarını etkilemez); canlı Race-Weekend UI (M23).

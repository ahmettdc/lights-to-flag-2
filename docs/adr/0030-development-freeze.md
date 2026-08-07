# ADR-0030 — FIA gelişim-dondurma rejimi (üç-mod eksen dondurması · kış-darbesi · ballot ile dinamik)

- **Durum:** Kabul edildi — indi (Ri3 motor + kış-darbesi, Ri4 dinamik ballot + persistence, Ri5 UI)
- **Tarih:** 2026-08-07

## Bağlam
Model B (ADR-0028; canlı sezon-içi R&D) araç gelişimini sezonun içine taşıdıktan sonra kullanıcı istedi:
**FIA regülasyonlarıyla parça ve motor gelişimini sezon-içi yasaklayabilmek**, ve bunun **dinamik dünyaya
bağlı** olması — sezon sezon açılıp kapanabilmesi. Gerçek F1'deki güç-ünitesi homologasyon dondurması + kış-arası
geliştirme mantığı.

Zemin (keşiften): regülasyon durumu `Carset.Regulations` (`RegulationSet`); dinamik evrim `RegulationProposal`
→ `RegulationBallot.Resolve` → `RegulationChange.Apply` (sezon-sınırı, `WorldSweep`/`LiveCareer`). Var olan
gelişim-dondurma mekaniği **yoktu**. Gelişim ekseni = `CarAxis` enum'u (`TechNode.Category` /
`DevelopmentProject.TargetAxis`) — kaba `CarRatingTarget` kayıplı (`PowerUnitReliability → Reliability`), o yüzden
dondurma **`CarAxis`'e** anahtarlanır. "Komponent" (`ComponentKind` Engine/Gearbox/Brakes) ayrı bir aşınma/grid-
cezası sistemi — R&D ile geliştirilmiyor, bu yüzden dondurma R&D `CarAxis` düğümlerine uygulanır.

## Karar

**Kullanıcıyla kilitlenen model: eksen başına üç seviye.** FIA bir ekseni ya **komple dondurur**, ya **kısmi
kısıtlar** (yalnız sezonlar-arası gelişime izin), ya **komple açık** bırakır:
- `DevelopmentFreezeMode.Full` — eksen hiç gelişmez (ne sezon-içi ne kış).
- `DevelopmentFreezeMode.InSeasonOnly` — sezon-içi donar, sezon sınırında (kış) bir darbeyle gelişir.
- Listede yok = **Open** — kısıtsız (varsayılan; inert → byte-özdeş).

### 1. Domain — `RegulationSet.DevelopmentFreezes`
`AxisFreeze { CarAxis Axis; DevelopmentFreezeMode Mode }` listesi `RegulationSet`'e asılır (varsayılan boş).
FIA-geneli: her takıma uygulanır. Değer-tipli (enum + eksen) → kayıtta byte-stable round-trip.

### 2. Enforcement — `ResearchLedger` iki müdahale noktası + iki pas
`Develop(...)` bir `IReadOnlySet<CarAxis> frozen` skip-seti alır: **başlatma** döngüsünde `node.Category ∈ frozen`
→ atla; **ilerletme** döngüsünde `project.TargetAxis ∈ frozen` → projeyi değiştirmeden koru (ne ilerler ne çözülür).
- **Sezon-içi** (`DevelopSeason`/`DevelopStep`): `frozen = {InSeasonOnly ∪ Full}` → Open eksenler gelişir.
- **Kış** (yeni `DevelopWinter`, sezon-sınırında `RollToNextSeason`'da): `frozen = tüm CarAxis \ {InSeasonOnly}` →
  yalnız InSeasonOnly eksenler kışın gelişir; Full hiç, Open zaten sezon-içi gelişti. Hiç InSeasonOnly eksen yoksa
  **inert** (aynı carset'i döndürür) → freeze'siz devir byte-özdeş. Harcaması cost-cap'e katlanır.

### 3. Dinamik — regülasyon ballot (dünyayla evrilir)
`RegulationProposal` bir freeze yükü kazanır (`DevelopmentFreezeMode? FreezeMode` + `IReadOnlyList<CarAxis>
FreezeAxes`; null → mevcut setback-önerisi, byte-özdeş). Sezon-sınırında `RegulationChange.Apply` her öneriyi
`RegulationBallot.Resolve` ile yeniden çözer; **geçen** freeze-önerilerinin ekseni/modu gelecek sezonun
`Regulations.DevelopmentFreezes`'ini **yeniden yazar** (geçmeyen/geri-çekilen freeze lapse olur). Oy **favored
axis**'e göre (mevcut M17 heuristiği); frozen eksenler ayrı yük. `Magnitude 0` bir freeze setback olmadan
dondurur. Freeze-işleme setback-penalty **kapısından önce** koşar (penalty 0'da bile çalışır); freeze-önerisi
geçmezse `Regulations` kımıldamaz (`Assert.Same`).

### 4. Persistence
`carset.Regulations` gemi-carset'inde statik ve kaydedilmiyordu; ballot ile evrilen freeze reload'da kaybolurdu.
`CareerState` **additive, inert-default** bir `DevelopmentFreezes` yakalaması kazandı (boşken byte-özdeş; golden
kayıt yok → round-trip idempotence korunur). Restore edilen freeze reload-sonrası reconstruct + kış-darbesine
doğru okunur.

### 5. UI + bildirim
R&D & Facilities ekranı (Ri5) "FIA DEVELOPMENT FREEZE" bölümünde donmuş eksenleri + modu gösterir
("Frozen (full)" kırmızı / "Frozen (in-season)" amber); freeze yoksa gizli. `CareerNews.ForFreeze` bir freeze
aktifleşince/kalkınca tarihli gelen-kutusu haberi doğurur (R&D ekranına deep-link).

## Sonuç
- **Byte-özdeşlik + golden korundu:** freeze inert-default (boş liste); freeze'siz carsetler byte-özdeş,
  `DevelopWinter` inert, `RegulationChange` freeze-önerisi yokken `Assert.Same`. Golden digest tek kanonik yarış
  (sezon döngüsü/regülasyon yolunda değil) → dokunulmaz. Tek yeni persist alanı additive + value-serialize.
- **Determinizm:** ballot + kış-darbesi + skip-seti tamamen seed'li, wall-clock yok. `ResearchSweep`
  `RegulationChange` çağırmadığından freeze'i asla aktifleştirmez → `FlagshipResearchTests` etkilenmez.
- **Mimari gardiyan:** dondurma Domain/Content/Career'da; UI yalnız yansıtır.
- **Katman haritası:** Domain (`DevelopmentFreezeMode`/`AxisFreeze` + `RegulationSet.DevelopmentFreezes` +
  `RegulationProposal` freeze yükü) · Content (`regulations.developmentFreezes` + öneri `freezeMode`/`freezeAxes`,
  `?? []` inert) · Career (`ResearchLedger` skip + `DevelopWinter`; `RegulationChange` ballot; `CareerState`
  persist) · App (`LiveCareer` kış-darbesi + haber; R&D ekranı rozetleri). Flagship demo: `power-unit-freeze`
  önerisi gerçek `InSeasonOnly` freeze taşır (motor+şanzıman+parça: `PowerUnit`, `PowerUnitReliability`,
  `GearboxReliability`, `MechanicalGrip`, `Braking`); mevcut setback'i korunur.

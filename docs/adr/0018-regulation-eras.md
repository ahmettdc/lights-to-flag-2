# ADR-0018 — Regülasyon çağları (DRS ↔ 2026)

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-05

## Karar
Motor **çoklu regülasyon çağı** destekler; hangi çağın geçerli olduğunu **carset seçer**.
Çağlar: `DrsEra` (2024/2025, mevcut davranış) ve `ActiveAero2026`. Bu, ADR-0010'un
`RegulationSet` / `regulations` bloğunun **ilk somut parçasıdır** — moddable, çok-dönemli
oyun için temel.

## 2026 kuralları (web'den doğrulandı)
DRS kaldırıldı → **aktif aero** (X-mode düşük sürtünme / Z-mode yüksek downforce); **Manuel
Override** = takipçi MGU-K'dan elektrik boost (DRS kanadı yerine); PU ~%50 elektrik (MGU-H
yok), %100 sürdürülebilir yakıt, **enerji yönetimi / de-rating** kritik; hafif/dar araç, az
downforce/sürtünme.

## Motor eşlemesi
`RegulationEra` enum + `RegulationSet` record (LTF.Domain.Racing). `RaceSimulator.Run` sonuna
**opsiyonel** `RegulationSet? = null` (null → DrsEra) → geriye uyum. Geçiş yardımı çağ-duyarlı:
DrsEra slipstream, 2026 enerji-bağlı Manuel Override. `CarRaceState.Energy` (rejen/deploy/
de-rating). Aktif aero X/Z lap-time delta **RaceSimulator seviyesinde** (LapTimeModel değişmez).

## Determinizm & hassasiyet
Enerji/aero/override **RNG'siz deterministik aritmetik** → mevcut pace/güvenilirlik/incident/
traffic stream'leri byte-özdeş; DrsEra yarışı bit bit eskisi gibi. 2026 kural adları jenerik
teknik terim; gerçek 2026 sezonu ayrı mod (ADR-0007).

## Uygulama
26a ✅ (çağ + Manuel Override + enerji temeli); 26b (aktif aero X/Z + top speed); 26c
(kalibrasyon + carset `regulations` bloğu).

## Sonuç
Kurallar statik bir arka plan değil; oyun **çoklu dönem** koşabilir ve regülasyon değişimi
(ADR-0022, Dinamik Dünya) gerçek bir motor mekanizmasına oturur.

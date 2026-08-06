# ADR-0017 — Pilot havuzu & scout (keşif) sistemi

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-04

## Karar
Ana seri dışında bir **pilot havuzu** var: alt/paralel serilerde (F2, F3, DTM, WEC…) yarışan
**+ boştaki (free agent)** pilotlar — ana serinin transfer ve akademi kaynağı. **Scout ekibi**
(personel) havuzu araştırır: pilotların **gizli nitelik/potansiyeli** keşif eforuyla açığa
çıkar (FM tarzı); keşfedilmemiş pilot = belirsiz/tahmini değerler. Scout raporu bir **aralık +
güven** verir; daha çok kaynak / daha iyi scout → daha kesin.

## Seri etiketi & hassasiyet
Her havuz pilotunun bir **kaynak seri** etiketi + o seride (soyut) form/sonucu var. Seriler
kurgusal (ADR-0003); gerçek seri isimleri yalnızca mod olabilir (ADR-0007).

## Determinizm
Keşif takvime göre ilerler (ADR-0011); bütçe (M13) + scout yeteneği (M14) hızlandırır.
Deterministik (tohumlu; §4.2 / ADR-0002).

## Model (özet; M12/M14/M18)
Havuz = `Carset.Reserves` + genişletilmiş **serbest/dış-seri havuzu**. Pilota **kaynak seri** +
(gizli) `Potential` (ADR-0015) + **keşif durumu** (scouted %). `Scout` (StaffRole'a eklenir veya
ayrı rol) + `ScoutingAssignment` (hedef seri/pilot, süre, kaynak) + `ScoutReport` (tahmini
nitelik aralığı + güven).

## Milestone dağılımı
M18 (transfer / regen kaynağı), M12 (havuzdan imzalama), M14 (scout işe alım), M17 (scout
yönlendirme + imzalama kararı), M22 (scout raporları / havuz ekranı + bildirim), ADR-0015
(imzalanan genç pilot gelişir).

## Durum (M18 — havuz transfer/regen kaynağı olarak uygulandı)
Atıl `Carset.Reserves` havuzu Core M18'de tüketildi: `TransferMarket.Resolve` (M18f) emeklilikle boşalan
koltukları serbest-ajan → `Reserves` → `RookieGenerator` (M18e, kurgusal rookie; ADR-0003) sırasıyla
doldurup `Team.DriverIds`'i yeniden yazar (bunu yapan ilk kod). **Ertelendi:** scout personeli + keşif eforu
+ `ScoutReport` (aralık + güven) + scouted% belirsizliği (M14/M22); alt/paralel-seri form modeli.

## Sonuç
Transfer piyasası derinleşir; keşif belirsizliği gerçek bir risk/ödül kararı yaratır.

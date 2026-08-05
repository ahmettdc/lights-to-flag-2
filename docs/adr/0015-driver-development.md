# ADR-0015 — Pilot gelişimi (potansiyel + yaş eğrisi + antrenman)

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-04

## Karar
Pilotlar zamanla **gelişir**: genç pilot gizli bir **`Potential`** tavanına yükselir
(yaş + yetenek + yarış deneyimi + antrenman + mentorluk). **Belli bir yaştan sonra yetenek
düşer** (zirve bandı ~27–32, ayarlanabilir): fiziksel nitelikler (Pace, baskı altında
Consistency) hızlı, deneyim nitelikleri (Racecraft, TyreManagement, Feedback, WetWeather)
yavaş düşer / bir süre korunur ("kurnaz veteran"). **Antrenman programı**: pilot başına aktif
nitelik odağı; takvim üzerinde, potansiyele yaklaştıkça azalan getiriyle ilerler.

## M8 ile ayrım (karıştırılmaz)
- **M8 "antrenman programları"** = yarış hafta sonu **serbest seansları** (FP1/2/3), kısa vade.
- **Bu (ADR-0015)** = **uzun vadeli pilot yetenek gelişimi**. İki ayrı sistem.

## Determinizm
Gelişim/gerileme/antrenman tümü deterministik (tohumlu; duvar saati yok — §4.2 / ADR-0002).
Aynı tohum + kararlar → aynı gelişim yörüngesi.

## Milestone dağılımı
M14 (antrenman altyapısı: pilot koçu / simülatör), M17 (patron: iki
pilotun antrenmanı + genç akademisi), M18 (**yaş eğrisi + potansiyele büyüme**), M24 (kariyer
gelişim grafikleri), ADR-0013 (mentorluk hızlandırır). Faz 1 kod değişmez: gelişim
`DriverAttributes`'ı değiştirir, lap-time modeli (M3/M4) onu okur.

## Sonuç
Pilotlar zaman içinde yükselen ve gerileyen yaşayan kariyerler olur; genç yetenek yatırımı anlam kazanır.

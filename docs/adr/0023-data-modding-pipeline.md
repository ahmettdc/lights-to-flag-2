# ADR-0023 — Veri boru hattı & modlama mimarisi

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-05

## Karar
Ham gerçek-dünya verisi **doğrudan** oyun puanına dönüşmez. İki katmanlı ayrım:
`ham kaynak → içe aktar+doğrula → normalize ana DB → oyun dengeleme katmanı → carset/mod`.
ADR-0007'nin (mod sistemi) veri-altyapısı derinleşmesidir.

## Katmanlar
- **Ana DB (ham/doğrulanmış):** `seasons / teams / drivers / staff / circuits / events / sessions /
  lap_times / pit_stops / results / race_control_events / data_sources`.
- **Denge katmanı (türetilmiş):** `mod_balance` — driver_pace/consistency/racecraft, team_car_pace,
  strategy/pit_crew rating, circuit_overtaking/tyre_wear/rain.

## Kaynak & lisans (izlenebilirlik zorunlu)
TracingInsights (birincil ham; **Apache-2.0 + NOTICE korunur**), StatsF1 (yalnız **doğrulama**,
scrape / yeniden-yayın yok), BigDataF1 (pist doğrulaması), Sidepodcast (rol sözlüğü), Formula
Careers (mekanik referans), F1 Salaries (ekonomi katsayısı — kesin değil). `data_sources`:
source / url / retrieved_at / license / confidence.

## Mod paketi
`carsets/<mod>/`: `manifest.json` + `sources.json` + `balance.json` + `changelog.md`; sürümlenebilir.
Doğrulayıcı: geçersiz bağımlılık, döngüsel tech ağacı, negatif maliyet, yanlış kategori. Şema sürümü
+ migration. Rastgele olaylar **seed'li → tekrar oynatılabilir** (ADR-0002).

## Hassasiyet
Gerçek-isimli/gerçek-veri modları **dağıtılmaz** (ADR-0007); shipped kurgusal (ADR-0003); kaynak
lisansları korunur; StatsF1 scrape edilmez.

## Milestone dağılımı
M2 (format: manifest/sources + ham↔denge), M10 (denge süpürmesi `mod_balance` doğrular), M26/M27/M28
(editör / katmanlama / içe aktarıcı / dokümantasyon), ADR-0022 (DWS ratingleri). Alt-sistem L.

## Sonuç
Gerçek veri saygıyla + izlenebilir kullanılır; oyun dengesi ham veriden ayrık ve sürdürülebilir kalır.

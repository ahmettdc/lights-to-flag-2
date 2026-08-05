# ADR-0025 — Yönetim kurulu, sahiplik & baskı sistemi

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-05

## Karar
Kısa-vade baskı ↔ uzun-vade plan gerilimi. Oyuncu teknik doğruyu yaparken kurul/sponsor/medya aynı
fikirde olmayabilir. Bu, Dinamik Dünya (ADR-0022) sahiplik ekosisteminin **oyuncu-tarafı yüzeyidir**.

## Yönetim tipleri (6)
Yarış-odaklı sahip · finans-odaklı kurul · üretici-destekli · gelişim projesi · sponsor-ağırlıklı ·
miras/aile. Hedef + bütçe sabrı + oyuncunun karar alanını belirler.

## Kurul & baskı
Kurul **çok üyeli**: her üye `priorities` (sporting / financial / long_term / brand / driver_dev) +
`traits` + `confidence_in_player` + `risk_tolerance`. **6 bağımsız baskı metriği:** board confidence ·
sporting · financial · sponsor · media · internal. Bağımsız — pistte iyi ama mali baskı olabilir.

## Hedefler & toplantılar
Hedefler sonuç/yarış/sürücü/teknik/finans/ticari; **açık / gizli / esnek / bağlantılı**; oyuncu hedef
karşılığı bütçe + risk müzakere eder. Toplantılar takvim ritmiyle (ADR-0011): sezon başı, ilk 3 yarış,
ATR/kural dönemi, sezon ortası, kriz, sezon sonu.

## Determinizm & içerik
Kurul kararları / güven evrimi tohumlu (aynı tohum+kararlar → aynı baskı; ADR-0002). Kurgusal.

## Milestone dağılımı
**M17 (ana)**, M13 (mali baskı/cap), M11 (görev güvenliği kalıcı), ADR-0022 (sahiplik), Rev 21 /
ADR-0021 (medya baskısı + toplantı diyalogları), ADR-0010/0018 (kural dönemi kararı), M12 (hedef↔prim),
M24 (görev güvenliği geçmişi), Rev 15 (bildirim). Alt-sistem N. M2 opsiyonel yönetim tipi + kurul.

## Sonuç
Patron modu yalnız teknik değil politik bir dengeleme oyunu olur: doğruyu yap, ama sahibini de tut.

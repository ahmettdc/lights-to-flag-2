# ADR-0024 — Araç puanlama modeli & R&D doğrulama döngüsü

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-05

## Karar
R&D katmanı üç ADR ile temsil edilir (çakışma yok): **ADR-0016** *ne* geliştirilecek (tech tree),
**ADR-0020** *ne kadar iyi* (tesisler), **ADR-0024** geliştirmenin *nasıl ölçülü performansa*
döndüğü. Bu ADR: **12-eksen araç puanı** + **pist-doğrulama döngüsü** + korelasyon/risk.

## Araç puan modeli (M1 Car'ın Faz 2 inceltmesi)
12 eksen, **500 merkezli** (450- gerisi / 500-549 rekabetçi / 600+ elit-pahalı-riskli):
`aero_low/medium/high_speed`, `aero_floor`, `drag_efficiency`, `mechanical_grip`, `tyre_management`,
`power_unit`, `power_unit_reliability`, `gearbox_reliability`, `braking`, `pit_operations`. Mevcut
5-kaba Car başlangıçtır; **LapTimeModel finer eksenleri okuyacak şekilde genişler** (ayrı milestone;
26b/26c'yi etkilemez, determinizm korunur).

## Pist-doğrulama (kritik)
**Puan anında artmaz.** In Design → In Manufacture → Ready for Track Test → Fitted for Practice →
Data Review → **Approved for Race** / Rework / Abandoned. Kalıcı puan yalnız onaydan sonra güncellenir.
Tasarım yalnız tahmin (aralık + güven + korelasyon %); gerçek etki antrenmanda (M8) + telemetride
(M5e) ölçülür. Test planları: A/B, back-to-back, uzun koşu, quali sim, doğrudan yarış. Sürücü
`technical_feedback` + `adaptability` (→ ADR-0015) yorum güvenini belirler.

## Bağlar
Cost-cap / üretim / lojistik (ADR-0010 + ADR-0019/0020); hava/pist/lastik (M4); güvenilirlik/ömür
(M5b/M15); rakip istihbaratı **bulanık, casusluk yasadışı sistem değil** → medya/ağ/gözlem
(ADR-0021/0013); teknik direktif/protesto (ADR-0010/0014); regülasyon-öncesi araştırma riski
(ADR-0018); AI takım kişilikleri (ADR-0022); tüm katsayı/düğüm/ATR **veri dosyası** (ADR-0023).

## Determinizm
R&D / korelasyon / AI kararları tohumlu (tekrar-üretilebilir; ADR-0002).

## Milestone dağılımı
**M14 (ana)**, M1 (12-eksen inceltme), M4/M5b/M15 (bağlam/güvenilirlik), M8 (doğrulama seansları),
M13 (cost-cap), M22 (mühendis↔oyuncu ekranları). Alt-sistem M. M2 opsiyonel 12-eksen + tech tree +
ATR tablosu.

## Sonuç
Geliştirme bir "buton" değil; ölçülen, doğrulanan, riskli bir mühendislik süreci olur.

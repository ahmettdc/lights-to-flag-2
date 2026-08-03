# ADR-0010 — Regülasyon & Yönetişim (FIA)

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-03

## Karar
Oyun, canlı bir **regülasyon & yönetişim** katmanı taşır: bir yönetişim organı (FIA
benzeri) aktif kuralları koyar, sezon içinde uyumu denetler ve sezon sonunda kuralları
değiştirir. Kapsam **teknik + sportif + finansal**; ceza hem **araca** hem **takıma** gelir.

## Kontrol
- **FIA (sistem) yönetir:** kuralları belirler, ihlalleri yaptırıma bağlar, sezon sonu
  değişiklikleri duyurur/uygular.
- **Takım Patronu modunda oyuncu etkiler:** önerilen kural değişikliklerine oy verir /
  lobi yapar (F1 Komisyonu tarzı), kendi aracına avantajlı yönü iteleyebilir.
- (Serbest sandbox düzenleme bu ADR'nin kapsamı değil; carset editörü M26'da.)

## Üç sütun
1. **Aktif regülasyonlar** — `GoverningBody` (denetim sıkılığı), `Regulation`
   (Technical / Sporting / Financial), `RegulationSet`. Mevcut `RulesSet` ve
   `Finances.CostCap` üstüne oturur.
2. **Sezon içi uyum & yaptırım** — İhlal iki kaynaktan doğar: (a) **risk/ödül** — takım
   veya oyuncu performans için sınırı bilerek zorlar (illegal aero/setup, bütçe aşımı,
   parc fermé); yakalanma olasılığı denetim sıkılığına bağlı. (b) **AI/sistem olayları** —
   rastlantısal scrutineering bulgusu, rakip protestosu. Cezalar: on-track (grid, süre,
   DSQ), teknik (araç illegal → diskalifiye, üretici puan kaybı), finansal (bütçe aşımı →
   para cezası, puan silme, aero-test/rüzgâr tüneli kısıtı). `Violation`, `Penalty`,
   `ComplianceState` + **itiraz** süreci.
3. **Regülasyon evrimi & hazırlık** — Sezon sonu kural değişikliği duyurulur; her takımın
   yaklaşan kurallara `Readiness` derecesi vardır (Ar-Ge ile yükseltilir). **Hazırlıksız
   takımlar yeni sezona araç performans cezasıyla başlar** (geriye düşme), hazırlıksızlıkla
   orantılı.

## Milestone dağılımı
Kesişen bir sistemdir; tek milestone değildir. On-track cezalar **M7** (Faz 1); uyum &
finansal yaptırım **M11/M13**; hazırlık & Ar-Ge **M14**; politik oy/lobi **M17**; kural
değişikliği + geriye düşme **M18**; arayüz (Regülasyon & Uyum ekranı + oylama) **M22**.
Carset formatı (M2) opsiyonel bir `regulations` bloğu taşır.

## Sonuç
Kurallar statik değil, oynanışın parçası: uyum bir risk/ödül ekseni, regülasyon değişimi
ise stratejik bir hazırlık yarışı olur.

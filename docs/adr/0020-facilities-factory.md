# ADR-0020 — Tesis & fabrika modeli

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-05

## Karar
Tesisler **doğrudan araç puanı vermez**; geliştirmenin **kapasite / hız / doğruluk / kalite /
risk**ini belirler. Her tesis **Level 1–5**; personel ile **tamamlayıcı** çalışır (tesis + insan
gücü birlikte). Level 5 otomatik zafer değil — aynı kaynakla daha iyi karar, daha kısa süre, daha
düşük risk.

## Tesisler
Tasarım ofisi (Design Points/slot/hata) · Rüzgâr tüneli (verim çarpanı/korelasyon) · CFD (paralel
proje/hız/güven) · Kompozit üretim (slot/süre/kusur) · Mekanik atölye · Kalite kontrol (kırılma/
kusur → M5) · Simülatör (setup/geri bildirim + genç gelişim → ADR-0015) · Dinamometre/PU
entegrasyonu · Pit crew merkezi (→ M7) · Veri merkezi (telemetri/strateji → M5e/M24).

## İlkeler
Basit "+10 aero" yok. Her yükseltme zaman + para + inşaat kapasitesi + personel; eşzamanlı sınırsız
yatırım yok; **bu-sezon aracı ↔ gelecek-sezon altyapısı** anlamlı seçim. Rüzgâr tüneli/CFD ATR
**kotasını artırmaz** (ADR-0010), kotanın verimini + pist korelasyonunu belirler.

## Determinizm
Tesis etkileri katsayı; olasılıklar tohumlu (ADR-0002).

## Milestone dağılımı
M14 (ana), M13 (yatırım/bakım maliyeti), M15 (kalite kontrol → güvenilirlik), ADR-0015 (simülatör),
M7 (pit crew), ADR-0016 (tech tree ile), M22 (arayüz). Alt-sistem I. M2 opsiyonel başlangıç tesis
seviyeleri.

## Sonuç
Tesis yatırımı bir "geleceğe hazırlık" ekseni olur; küçük takım doğru yatırımla açığı kapatabilir.

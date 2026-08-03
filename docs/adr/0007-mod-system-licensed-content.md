# ADR-0007 — Mod sistemi ve lisanslı içerik

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-03

## Karar
İçerik üç aşamada ilerler: (1) **kurgusal** carset (varsayılan, oyunla gelir) →
(2) **lisanslı içerikli modlar** (gerçek takım/pilot isimleri) → (3) **birden çok
modu bir arada koşturan kariyer sezonu**.

## Lisans sınırı
Gerçek isimli içerik yalnızca **kullanıcı modu** olarak var olur; ana dağıtıma
**dahil edilmez**. Telif/lisans riskini oyunun kendisinden ayırır (ADR-0003 mantığı).

## Teknik sonuç
İçerik formatı **M2'den itibaren katmanlamaya (overlay/layering) hazır** tasarlanır:
bir mod, temel carset'in üstüne isim/görsel/ayar bindirebilmeli. Mod sistemi
özelliğinin kendisi Faz 4'te (M27–M28) gelir.

## İlk lisanslı mod: gerçek 2024/2025 sezonu (güncelleme)
Kullanıcı örnek sezon için **statsf1.com**'u referans gösterdi ve içerik türünü
"gerçek sezon ama mod olarak" seçti. Buna göre:
- Oyunla gelen örnek carset **kurgusal** kalır (bkz. ADR-0003) ama gerçek **2024/2025**
  verisiyle kalibre edilir (takvim boyutu, pist özellikleri, performans sıralaması, puan
  sistemi).
- Gerçek takım/pilot isimli **tam 2024/2025 sezonu**, aynı carset formatında bağımsız
  bir carset olarak `content/mods/` altında hazırlanır — **yayınla dağıtılmaz**, hiçbir
  paket hedefine girmez. Overlay/kurulum UX'i M27'de olgunlaşır; veri şimdiden yazılabilir.
- Bu, ADR-0007'nin somut ilk örneğidir: lisans-hassas içerik oyundan ayrık kalır.

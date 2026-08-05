# ADR-0021 — Medya & diyalog motoru (paddock etkileşimi)

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-05

## Karar
Paddock etkileşimi tek bir gelen kutusu değil; **mekân-tabanlı diyalog + medya + delegasyon**
sistemi. ADR-0013 (ilişkiler) ve Rev 15 (bildirim merkezi) bunun üstünde birleşir.

## Medya
Metrikler: team reputation · driver popularity · sponsor confidence · paddock credibility · media
pressure · narrative heat. Basın toplantısı yalnız tetikte (kriz/galibiyet/kaza/takım emri/
sözleşme); oyuncu/sürücü/press officer konuşur; yanlış açıklama uzun-vade güven kaybı.

## Mekân & delegasyon
Kişiler farklı mekânlarda erişilir (Garage / Pit wall / Hospitality / Motorhome / Teknik ofis /
FIA ofisi / Havaalanı). Oyuncunun zamanı sınırlı → **delegasyon** (Press Officer / Sporting
Director / HR / TD / Chief Race Engineer) hızlı ama daha az kontrol ve ilişki etkisi.

## Diyalog motoru
Olay → önem/aciliyet skoru → mesaj/yüz-yüze/toplantı → yanıt/ertele/delege → sonuç → hafıza/ilişki
güncelle. Kişi/konu/olay **cooldown** + hafızaya atıf; "otomatik-çöz / yalnız-kritik" tercihleri.
İlişki ekranı **bulanık** (kesin puan gösterilmez — yalnız UI; veri kesin).

## Determinizm & içerik
Diyalog/medya sonuçları tohumlu (ADR-0002). Kurgusal.

## Milestone dağılımı
M11 (metrikler kalıcı), M12 (sözleşme diyalogları), M16/M17 (ajans), M21 (Paddock Hub — Rev 15),
M22 (kişi/diyalog/ilişki ekranı), M23 (hafta sonu mekânları). ADR-0010/0014 (FIA/protesto).
Alt-sistem J.

## Sonuç
Padok bir haber + insan ilişkisi sahnesi olur; iletişim kararları da bir kaynaktır.

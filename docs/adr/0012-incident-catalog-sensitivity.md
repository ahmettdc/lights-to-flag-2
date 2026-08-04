# ADR-0012 — Olay kataloğu & hassasiyet politikası

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-04

## Karar
F1 tarihinden ilham alan yarış olayları, oyuna **tür olarak** entegre edilir — **isim
olarak değil**. Genel mekanikler (pit yangını, lastik patlaması, ilk-viraj karambolü,
güvenlik aracı manipülasyonu…) veri-güdümlü bir **`IncidentCatalog`** üzerinden gelir;
telifli isimler / gerçek pilotlar / gerçek yarışlar oyunla **dağıtılmaz**. Katalog
carset'ten (`balance` / `incidents`) tunelenebilir ve moddable'dır (ADR-0007 hattı).

## Hassasiyet (bağlayıcı)
- **Ölümlü veya ağır yaralanmalı gerçek trajediler oynanabilir/adlandırılmış olay olarak
  modellenmez.** Ciddi olaylar soyutlanır: **DNF / sezon-dışı sakatlık** — grafik detay
  yok, gerçek kurban ismi yok.
- **Kurtarma aracı / pist görevlisi çarpması modellenmez.** Yalnızca soyut "kurtarma
  devrede → VSC/SC".
- İsimli gerçek-yarış canlandırmaları yalnızca **opsiyonel senaryo/mod** olarak var
  olabilir (ADR-0007 kapsamında); ana dağıtım genel türleri kullanır (isim yok).

## Katalog (genel türler; F1 tarihinden ilham)
- **A — Mekanik/güvenilirlik:** motor/şanzıman/fren/hidrolik/ERS arızası, aşırı ısınma,
  yakıt basıncı / yakıt bitmesi, süspansiyon, lastik delaminasyonu/patlama (→ debris).
- **B — Pilot hataları:** kilitlenme, pist dışı (pist limiti), spin, tek-araç kazası,
  aquaplaning, hatalı start.
- **C — Araç-arası:** ilk-viraj karambolü, geçişte çarpışma, arkadan çarpma, sıkıştırma,
  şampiyonluk-kritik temas (isimsiz, nadir).
- **D — Pit:** güvensiz bırakma, tekerlek kopması, pit yangını (yakıt ikmali açıksa), pit
  hız aşımı.
- **E — Pist/çevre:** debris, gevşek rögar kapağı, pistte cisim/hayvan, izinsiz giriş
  (soyut), ani sağanak, aşırı sıcak/soğuk, sis, kararan ışık.
- **F — Yarış kontrolü:** siyah / siyah-turuncu / mavi bayrak, ceza servisi, teknik DQ.
- **G — Strateji/nadir sonuç:** son turda yakıt bitmesi, yanlış lastik çağrısı,
  double-stack gecikmesi.
- **H — Tuhaf renk (nadir):** takım arkadaşı teması, yavaş yanan araç, vizör bandı fren
  kanalını tıkaması, radyatöre debris, sosis-kerb sıçraması. (Ciddi sonuç yok.)

## Determinizm
Her olay bir şablondur (id, kategori, tetik koşulu, olasılık ağırlığı, şiddet dağılımı,
sonuç, nötralizasyon eğilimi, ceza bayrağı) ve tüm ruloları `DeterministicRandom`
fork'larından çeker (§4.2 / ADR-0002). Aynı tohum → aynı olay dizisi.

## Milestone dağılımı
- **M5b:** A grubu (mekanik) + H mekanik renk — kataloğun mekanik dalı. *(Bu milestone.)*
- **M5c:** B (pilot) + C (çarpışma) + D'nin start-ilişkili kısmı — **hassasiyet çerçevesi
  burada devreye girer**.
- **M5d:** E (çevre/tetik) → nötralizasyon durum makinesi (VSC/SC/kırmızı).
- **M5e:** olay günlüğü/telemetri (`RaceEvent`) → canlı zamanlama (M23), istatistik (M24)
  ve ilişki bağı (ADR-0013). Tam-olaylı yarış için altın-determinizm.
- **F/G:** ağırlıkla M7 (ceza/pit/strateji) ve ADR-0010 (regülasyon DQ); M5 bunlara
  yalnızca bayrak bırakır.

## Sonuç
Zengin, tanıdık bir olay repertuarı — ama telif ve hassasiyet açısından güvenli: türler
genel, isimler yok, trajedi canlandırması yok, hepsi deterministik ve moddable.

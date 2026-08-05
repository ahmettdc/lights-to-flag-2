# ADR-0019 — Araç yönetim modülü (Vehicle)

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-05

## Karar
Oyuncu bir **Teknik Direktör** gibi aracın her yönünü yönetir (sürüş dışında). Motorsport
Manager / Football Manager derinliği; her değişikliğin **finansal + mühendislik + sportif**
sonucu vardır; **her parça geçmiş taşır, her eylem kalıcı kayıt**. Modül Finance / Staff /
R&D / Manufacturing / Regulations ile tam bağlıdır.

## Ekranlar (Faz 3 yüzeyi)
Overview · Garage · Assembly (exploded view) · Components · Upgrades · Setup · Reliability ·
Damage · Inventory · Manufacturing · Livery · Electronics · Fuel System · Tyres · Homologation ·
Regulations · Development History · Performance Analysis · Comparison · Global Search.

## Mevcut sistemlerle bağ (çift çalışma yok)
Components → M1 domain + M5b sağlık + M15 kota; Upgrades → **ADR-0016 tech tree** + **ADR-0024
doğrulama** (araç-tarafı yüzeyi, ayrı ağaç değil); Reliability/Damage → M5b-d + ADR-0012;
Setup → M8; Regulations/Homologation/BoP → ADR-0010/0018; Performance/Comparison → M5e + M24.

## Yeni parçalar
Manufacturing (üretim kuyruğu; ADR-0020), Inventory/Logistics, Homologation & BoP, Setup ekranı,
Livery editörü, Electronics/ECU, Development-history zaman çizelgesi.

## Determinizm & içerik
Üretim/hasar/kurulum sonuçları tohumlu (ADR-0002). Tedarikçi/parça isimleri kurgusal; gerçek
isimler yalnız kullanıcı modu (ADR-0007/0023).

## Milestone dağılımı
M1 (bileşen envanteri), M5 (reliability/damage), M14 (Upgrades/Manufacturing), M15 (tahsis/
homologasyon), M8 (setup), M13 (maliyet), **M22 (Faz 3 tam Vehicle merkezi)**, M24 (analiz).
Alt-sistem H. M2 opsiyonel bileşen/tedarikçi/homologasyon verisi.

## Sonuç
Araç bir sayı seti değil; geçmişi ve sonuçları olan yaşayan bir mühendislik projesi olur.

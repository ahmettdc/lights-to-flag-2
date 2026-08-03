# ADR-0003 — Her şey sıfır: içerik ve marka dahil

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-03

## Karar
Yeni veri formatı, elle yazılmış **kurgusal** örnek carset, yeni marka kiti
(palet + logo), yeni tipografi.

## Gerekçe
Kararın kendisi kullanıcıya ait. Yan fayda önemli: v1'in `carsets/F1 2019/`
klasöründe 107 gerçek pilot fotoğrafı ve gerçek takım/pilot/sponsor isimleri vardı;
lisans durumu belirsizdi. Kurgusal içerik bu riski tümden kaldırır ve oyunu
dağıtılabilir kılar.

## Marka kiti geldi (güncelleme)
Kullanıcı marka kitini ve UI mockup'ını **Claude Design**'da hazırlayıp teslim etti
(bkz. `design/`). Kilitlenen kimlik:
- **Palet:** Track Black `#08090B`, Garage `#14171C`, Lights Out Red `#FF3B2F`,
  Flag White `#F2F4F6`.
- **Tipografi:** **Saira Condensed 900 / Chakra Petch 700 / Archivo 500** — hepsi
  Google Fonts (OFL, ticari kullanıma açık). *Not: bunlar isim olarak v1'in fontlarıyla
  aynı; erken taslakta "kullanılmayacak" denmişti ama marka kararı kullanıcıya ait ve
  bu fontlar ücretsiz/OFL olduğu için sorun yok — kullanılacaklar.*
- **Logo:** 5 ışık + "LIGHTS TO FLAG 2" lockup; SVG kaynak `design/brand/`.

Bu, M19'un (tema + marka) risklerini büyük ölçüde düşürür.

## Sonuç
Kurgusal içerik korunuyor. UI mockup'ındaki kurgusal takım/pilot isimleri M2'nin örnek
carset'inde yeniden kullanılacak (Talon Racing, Kuro Dynamics, Kestrel Racing, Sable GP,
Nordwind, Aurelia Corse, Marchetti Corse; Mateo Ferreira, Idris Whitlock, Freya Nilsen…).

**Kalibrasyon (güncelleme):** İsimler kurgusal kalır ama örnek carset gerçek **2024/2025**
verisiyle (statsf1.com referans) kalibre edilir — gerçekçi takvim, pist özellikleri,
performans sıralaması ve puan sistemi. Gerçek isimli sezon ayrı bir mod olarak dağıtılmaz
(bkz. ADR-0007).

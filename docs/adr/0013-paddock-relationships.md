# ADR-0013 — Paddock ilişkileri & insan dinamikleri

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-04

## Karar
Paddock'taki **tüm insan aktörler** (pilotlar, takım patronu, personel) arasında **ikili
ilişkiler** modellenir. Her ilişki bir **yakınlık** değeri (−100…+100) + **tür/etiket**
(rakip, müttefik, mentor, gergin, güven) + kısa geçmiş taşır. **Kişilik özellikleri**
(ego, sadakat, mizaç, hırs) ilişkilerin nasıl kurulup değiştiğini sürer.

## Çift yönlü bağ (olay ↔ ilişki)
- **Olay → ilişki:** pist-içi temas (özellikle takım arkadaşıyla), takım emrine uymama,
  favoritizm, basın açıklamaları ilişkiyi değiştirir. Yarış olay günlüğü (`RaceEvent`,
  M5c/M5e) katılımcı id'lerini taşır (`CompetitorId` + `OtherCompetitorId`); kariyer katmanı
  bunu okuyup ilişkileri günceller.
- **İlişki → olay:** bozuk / aşırı-rekabetçi ilişki (yüksek ego) **olay üretir** — ör.
  sıralamada takım arkadaşını **kasıtlı engelleme** (pit-boks oyalama) → impeding cezası +
  feud. (2007 Macaristan tarzı bir tür; isimsiz — ADR-0012.)

## Etkiler (mekanik, salt renk değil)
Moral · sözleşme pazarlığı · takım emri kabulü · veri/kurulum paylaşımı · geliştirme
önceliği · personel ayrılma riski · takım-içi çarpışma olasılığı · mentorluk (veteran genç
gelişimini hızlandırır) · sadakat indirimi.

## Model (özet; M11/M12)
`Relationship` (iki id + `Affinity` + `Kind` + son olaylar), `RelationshipGraph` (kariyer
dünya durumunun parçası, kalıcı), `Personality` (ego/sadakat/mizaç/hırs). Mevcut
`Driver.Morale`/`Reputation` skalerlerini besler; `Team.Principal` string'i ileride id'li
bir aktöre yükselir.

## Determinizm
Deterministik kariyer simülasyonu (tohumlu; `DateTime.Now` yok — §4.2 / ADR-0002). Aynı
tohum + kararlar → aynı ilişki yörüngeleri. Kayıt grafiği birebir korur.

## Milestone dağılımı
M11 (kalıcı grafik), M12 (doğuş + pazarlık ilişkiyi okur), M14 (personel uyumu/ayrılma),
M17 (patron insan yönetimi / favoritizm / takım emri), M18
(evrim / feud / mentorluk / transfere etki), M21 (ilişki haberleri), M22 (Paddock/İlişkiler
ekranı). Simülasyon ilişkiden habersiz kalır (katmanlama).

## Sonuç
Paddock yaşayan bir sosyal ağ olur; sonuçları yalnız hız değil, insan dinamikleri şekillendirir.

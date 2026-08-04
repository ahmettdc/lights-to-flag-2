# ADR-0014 — Hukuk & tahkim (paddock mahkemesi)

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-04

## Karar
Paddock'ta **herhangi bir konuda** dava/tahkim olabilir: regülasyon protestosu & ceza
itirazı, sözleşme anlaşmazlığı, ticari/sponsor anlaşmazlığı, fikri mülkiyet/casusluk (soyut),
personel ayartma. Takım **avukat / hukuk müşaviri tutar** (`Skill` + yıllık retainer + dava
ücreti) — bir **ekonomi kalemi**. Dava **para + itibar** maliyeti taşır.

## ADR-0010 ile ilişki
ADR-0010 zaten bir **itiraz (appeal)** süreci içerir. Hukuk sistemi bunun **derinleşmesidir**:
itirazı avukatlı, masraflı, çok adımlı bir **tahkim/mahkeme** sürecine çevirir ve kapsamı
regülasyonun ötesine (sözleşme/ticari/personel) taşır. ADR-0010'u yeniden yazmaz, genişletir.

## Süreç & determinizm
Takvim üzerinde işler (ADR-0011): açılış → duruşma → karar; sezon boyunca sürer, anlık değil.
Karar olasılığı = f(dava esası, davacı avukat ↔ davalı avukat, yönetişim eğilimi) + tohumlu
çekiliş. Aynı tohum + kararlar → aynı sonuç (§4.2 / ADR-0002).

## Sonuçlar
Para cezası · puan silme · **ceza onama/bozma** (grid/sonuç iadesi) · tazminat/uzlaşma ·
dışlama · sözleşme geçersiz/uygulanır · **tedbir** (ör. transferi bloke etme).

## Hassasiyet
Kurgusal / isimsiz; gerçek dava veya kişi ismi yok (ADR-0003/0007/0012).

## Milestone dağılımı
M11 (kalıcı + takvim duruşmaları), M12 (sözleşme davaları), M13 (**avukat retainer + dava
masrafı + ceza/tazminat**), M14 (IP/casusluk + ayartma), M17 (**ana ajans:** aç/savun/uzlaş,
avukat tut), M18 (uzun davalar + tedbir), M21 (bildirim/karar), M22 (Hukuk/Davalar ekranı).

## Sonuç
Paddock politikası mahkemeye taşınabilir; masraflı, riskli, ama kazançlı bir strateji katmanı.

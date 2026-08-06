# ADR-0011 — Takvim-tabanlı kariyer (Football Manager tarzı)

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-03

## Karar
Kariyer, **oyun-içi takvim üzerinde gün gün ilerleyen** bir zaman çizgisidir (FM modeli).
Round-index'li sezon ilerleyişi (v1) yerine: bir oyun tarihi, tarihe çakılı bir
fikstür/olay takvimi ve **"Devam" (Continue)** ile bir sonraki eylem gerektiren olaya
kadar boş günleri hızlı geçme.

## Semantik
- Saat **günlüktür**; oyuncu "Devam"a basınca sistem bir sonraki olaya/eyleme kadar
  ilerler (tek gün de ilerletilebilir).
- Olaylar tarihe bağlıdır: yarış hafta sonları, test günleri, regülasyon duyuruları
  (ADR-0010), sözleşme son tarihleri, transfer penceresi, yönetim kurulu değerlendirmeleri.
- Tarihler carset'te round/olay başına yazılır: kurgusal örnek makul modern-sezon
  tarihleri, gerçek-sezon modu gerçek tarihleri kullanır.

## Determinizm
Oyun tarihi bir **oyun durumudur**, duvar saati değil. `DateTime.Now` kullanılmaz
(§4.2 / ADR-0002). Aynı tohum + aynı kararlar → bit bit aynı sonuç. Kayıt, güncel oyun
tarihini + takvimi + olay kuyruğunu birebir saklar.

## Model (özet)
`GameDate` (veya `DateOnly` sarmalayıcı), `CalendarEvent` (RaceWeekend / TestDay /
RegulationAnnouncement / ContractDeadline / TransferWindow / BoardReview…), `SeasonCalendar`
(sıralı olay kuyruğu). Kariyer motoru (M11): `AdvanceDay()` / `ContinueToNextEvent()`.

## Milestone dağılımı
Ağırlıkla **M11** (oyun saati + takvim + Continue). Zaman-planlı sistemler: sözleşmeler
(M12), Ar-Ge ilerleme (M14), test günleri (M15), sezon devri/transfer (M18). Arayüz:
Continue + tarihli gelen kutusu + takvim (M21). Kalıcılık: M31.

## Durum (M15 — test günleri uygulandı)
`Carset.TestDays` (tarih + pist) carset'ten yüklenir + doğrulanır; `SeasonCalendar` bildirilen ama o güne
dek üretilmeyen `CalendarEventKind.TestDay` olayını nihayet üretir (liste boşken byte-özdeş). Sezon motoru
bir **tur-arası dikişle** (`SeasonSimulator.RunProgressed` + `IBetweenRounds`) evrilir; test günü R&D
geliştirmesini hızlandırır (`RndProgression`). **Ertelendi:** interaktif günlük tick + Continue-sürücülü
runner (Faz-3 UI).

## Sonuç
Kariyer, yarıştan yarışa atlayan bir tablo değil; günleri akan, olaylar ve son tarihlerle
dolu yaşayan bir zaman çizgisi olur.

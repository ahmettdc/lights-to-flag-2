# ADR-0004 — Tek kariyer modu: Takım Patronu (Pilot Kariyeri ertelendi)

- **Durum:** Revize edildi (Rev 26; önceki "çift oyuncu modu" kararının yerini alır)
- **Tarih:** 2026-08-05

## Karar
Oyun **tek bir kariyer modu** taşır: **Team Principal (Takım Patronu)**. **Driver Career
(Pilot Kariyeri) şimdilik rafa kaldırılır** — silinmez, post-1.0 bir fikir olarak kayıtlı kalır.

## Neden değişti?
İlk karar iki modu (Driver Career + Team Principal) baştan taşımaktı. Kullanıcı odağı tek moda
çekti: derinlik iki moda bölünmek yerine **Takım Patronu** deneyimine yoğunlaşır — pit duvarı,
bütçe, Ar-Ge, transfer ve yönetim kurulu baskısı (ADR-0025). Güncel UI mockup'ı da (`design/`)
tamamen Team Principal; ayrı bir sürücü-kariyeri akışı çizilmemiştir.

## Sonuçlar
- **Kod etkisi yok:** kariyer katmanı (`LTF.Career`) henüz yazılmadı; bu saf bir kapsam kararıdır.
- **M16 (Pilot Kariyeri modu) rafa alınır; M17 (Takım Patronu) tek kariyer yüzeyidir.** Faz 2/3'te
  "pilot ajansı (M16)" kancası taşıyan ADR'ler (0013/0015/0021) Patron-tarafına (M17) daralır.
- Ortak dünya durumu (ADR-0022, Dinamik Dünya) yine M11'de kurulur; ileride Pilot Kariyeri eklenmek
  istenirse aynı yaşayan dünya üstüne bir karar yüzeyi olarak oturur (kariyer katmanı yeniden
  yazılmaz).

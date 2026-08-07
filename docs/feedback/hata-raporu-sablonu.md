# Lights to Flag 2 — Hata & Geri Bildirim Şablonu

> **Sürüm:** v0.9.0 · **Platform:** Windows · **Test tarihi:** `<GG.AA.YYYY>`
>
> Bu şablonu **kopyala → doldur → bana (Claude) yapıştır.** Boş bıraktığın yerlere `—` koyabilirsin.
> Amaç: her sorunu doğru **alana** oturtup, **hata / eksik** ayrımını net yapmak.

## Nasıl doldurulur (30 saniye)

- Her sorunu, ilgili **alan başlığının** (aşağıdaki 1–8) altına bir **rapor bloğu** olarak ekle.
- Başına **Tip** ve **Önem** etiketi koy (lejant aşağıda).
- **Yarış/sonuç** ya da **sayı** hatasında **carset + oyun-içi tarih + kayıt dosyası** ver → aynı durumu bende
  birebir tekrar üretebilirim (oyun deterministiktir).
- Bir alanda sorun yoksa `—` bırak. Aynı türden çok sorun varsa bloğu çoğalt.

### Lejant

**Tip:** 🔴 **Hata** (yanlış çalışıyor / çöküyor) · 🟡 **Eksik** (olmalı ama yok) · 🟠 **Denge** (çalışıyor ama
sayı/zorluk yanlış) · 🔵 **UI/UX** (kozmetik / kafa karıştırıcı) · 💬 **Öneri**

**Önem:** **S1** Kritik (çökme / veri kaybı / ilerleme durur) · **S2** Yüksek · **S3** Orta · **S4** Düşük–kozmetik

### Repro bağlamı (yarış & sayı hataları için)

Kayıtlar burada: `%AppData%\LightsToFlag2\saves\{carset}__autosave.json` (+ `settings.json`).
Oyun seed'i sabittir ve ekranda görünmez; bu yüzden en iyi repro üçlüsü: **carset adı + oyun-içi tarih + kayıt dosyası.**
Kayıt dosyasını bana doğrudan ekleyebilirsin.

---

## 0) Genel izlenim & en can sıkıcı 3 şey

- **Genel his:** `—`
- **En çok rahatsız eden 3 şey:**
  1. `—`
  2. `—`
  3. `—`

---

## Rapor bloğu şablonu (kopyala)

```
### [🔴 Hata · S2] Kısa ve net başlık
- Ekran/sekme: <örn. RACE WEEKEND → QUALIFYING>
- Adımlar: 1) …  2) …  3) …
- Beklenen: …
- Gerçekleşen: …
- Bağlam: carset=<Global Prix Series | Global Prix 2026> · oyun-içi tarih=<…> · round/devre=<…> · tekrar eder mi=<evet/hayır> · kayıt eklendi=<evet/hayır>
- Ekran görüntüsü/video: <link/ek>
- Not: …
```

---

## 1) Ana menü & kariyer kurulumu
_Main Menu · New Career (STEP 1–4 + board negotiation: CAUTIOUS/BALANCED/AGGRESSIVE) · Load Game · Quick Race · Settings_

—

## 2) Genel bakış & pano
_PADDOCK HUB · STANDINGS · CALENDAR · DATABASE · DRIVERS · üst bar (CONTINUE / INBOX / CAP ROOM / BOARD CONF)_

—

## 3) Yarış hafta sonu & canlı yarış
_RACE WEEKEND → STARTING STRATEGY, START RACE / RACE LIVE, PIT WALL (BOX / PUSH / EXTEND / MANAGE), **RACE** sekmesi (track map · timing tower · TEAM RADIO · replay), **QUALIFYING** sekmesi_

—

## 4) Yönetim
_R&D & FAC. · CARS & PU · STAFF · FINANCE (banka/kredi) · BOARD & SPON._

—

## 5) Records / istatistik
_RECORDS → PROFILES · THIS SEASON · ALL-TIME · HALL OF FAME (+ puan grafikleri, head-to-head, track records)_

—

## 6) Kaydet / yükle, sezon dönüşü & inbox
_Autosave · Continue · Load Game · sezon sonu rollover · INBOX durakları (karar bekleyen olaylar)_

—

## 7) Motor / determinizm / denge
_Yarış sonucu, şampiyona matematiği, ekonomi/banka, R&D ilerlemesi, transfer, regülasyon etkileri — "sayılar yanlış geliyor" hisleri buraya_

—

## 8) Genel UI / metin / performans / çökme / ayarlar
_Tema, lokalizasyon metni/yazım, hizalama/taşma, SETTINGS ekranı, donma/çökme/performans_

—

---

## ⛔ Bunları HATA olarak bildirme (v0.9'da bilerek yok / böyle)

Aşağıdakiler kasıtlı; bunları **hata** olarak yazma — istersen **💬 Öneri** olarak ekleyebilirsin:

- Yalnız **Windows**; **imzasız** (SmartScreen uyarısı normal); **installer / otomatik güncelleme yok**.
- **Oyun içi öğretici yok** → sıradaki milestone **M25**.
- **Pilot / sürücü olarak oynanmıyor** — tek mod **Team Principal** (kasıtlı tasarım tercihi).
- **Carset editörü / mod sistemi yok** · **ses yok** · **sınırlı erişilebilirlik**.
- **Haber-söylenti akışı, scout/raporlar, hukuk/tahkim, medya-röportaj, sürücü akademisi yok** (hepsi planlı).
- **Practice (antrenman) oturumu yok** — kariyerde koşulmuyor.
- **Bağımsız "Regulation" ekranı yok** — regülasyon hazırlığı R&D / Board ekranlarına katlandı.
- **Difficulty ayarı şu an motoru ETKİLEMEZ** — determinizmi korumak için bilinçli; ileride bağlanacak.
- Yalnız **koyu (Dark) tema**; UI yalnız **İngilizce** (Türkçe dil paketi henüz yok).
- **Şematik pist haritası jeneriktir** (devreye özel geometri değil, yaklaşık işaretçi konumu).
- SETTINGS'te bazı açılır menüler ham enum ismi gösterir (örn. `VeryFast`) — bilinen kozmetik pürüz.

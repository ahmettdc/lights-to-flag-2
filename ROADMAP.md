# Lights to Flag 2 — Yol Haritası

> **Durum:** Faz 0 ✅ (M0–M2) · **Faz 1: M3 ✅ · M4 ✅ · M5 ✅ (a–e) · M6 ✅** (RNG + tur zamanı; lastik/yakıt/hava; yarış motoru: güvenilirlik + pilot hataları + çarpışma + start + nötralizasyon + zengin telemetri; **trafik: kirli hava / slipstream-DRS / geçiş mücadelesi**, CI yeşil) · **2026 regülasyon çağı ✅** (26a–c: aktif aero X/Z + Manuel Override + enerji/de-rating; carset `regulations` bloğuyla seçilebilir — ADR-0018) · **M7 ✅** (a–c: pit stop süre dağılımı + bileşim rotasyonu; kademeli pit pencereleri + güvenlik aracı altında ucuz stop; on-track cezalar: pist limitleri + güvensiz bırakma) · **M8 ✅** (sıralama Q1/Q2/Q3 + tek-tur/tek-seans → grid; antrenman programları → hafta sonu getirisi) · **M9 ✅** (a–g race-level: RaceFormat demeti; süreli/kısa yarış; pole/lider-tur/en-çok-lider puanları; başarı balastı; çok-sınıf klasman; rolling start + unlap kuralı; sprint puan tablosu; ters-grid/grid-ceza helper) · **M10 ✅** (altın-dosya determinizm hash'i — Win/macOS/Linux birebir; `ltf sweep` denge aracı) · **Faz 1 tamamlandı (M3–M10 + 2026 çağı).** · **Faz 2: M11 ✅ · M12 ✅** (M11 a–e: şampiyona + sezon motoru + takvim/Continue + kalıcı rekorlar + JSON kayıt/yükleme; M12 a–f: ilişki grafiği doğuşu + sözleşme yaşam döngüsü + pazarlık ilişkiyi okur + persistence — ADR-0013; **M13 a–g:** gelir/gider döngüsü + bütçe tavanı yaptırımı + finans persistence + `EconomySweep` çok-sezon denge süpürmesi — ekonomi kendini dengeliyor; **M14 a–n:** hibrit tech tree (4 departman/16 düğüm) + tam 10-tesis modeli + personel + **pist-doğrulama döngüsü** (ADR-0024) + **lastik aşınma fiziği** (nazik araç daha az aşınır → daha hızlı tur) + `ResearchSweep` çok-sezon süpürmesi — araç ölçülebilir/sınırlı/deterministik gelişir; ADR-0016/0020/0024) · Sıradaki: **M15** (test günleri + sezon-içi güncellemeler + bileşen tahsisi/grid cezaları) · Marka kiti + UI mockup teslim alındı (`design/`)
> · **Belge tarihi:** 2026-08-05 · **Belge dili:** Türkçe · **Oyun arayüz dili:** İngilizce
> · **Hedef platform:** Windows + macOS (Linux: yalnızca CI) · **Kapsam:** 7 faz (0–6), 37 kilometre taşı (M0–M36), tek oyunculu 1.0 + çok oyunculu co-op 2.0

---

## 1. Bu belge ne?

Bu belge, **Lights to Flag 1**'den ilham alan yeni bir F1 yönetim/yarış oyununun —
**Lights to Flag 2** — sıfırdan inşa planıdır. Sırayla uygulanacak 7 faz (0–6) ve 37
kilometre taşından (M0–M36) oluşur.

Belgeyi şöyle okuyun: **Bölüm 2** neden böyle yaptığımızı, **Bölüm 4** neyi inşa
ettiğimizi, **Bölüm 5** hangi sırayla yaptığımızı anlatır. Sadece "sırada ne var?"
diye bakıyorsanız doğrudan **Bölüm 9**'a gidin.

### 1.1 "Sıfırdan" kararının kaydı

Bu belge yazıldığında repoda **çalışan bir sürüm zaten vardı** (bundan sonra **v1**).
v1'in kapsamı:

- **.NET 8 + WPF (MVVM)**, sadece Windows
- `LightsToFlag.Core`: carset yükleyici (eski alt-çizgili metin formatı), tur zamanı,
  antrenman, sıralama, yarış simülatörü, kariyer motoru, JSON kayıt/yükleme
- `LightsToFlag.App`: koyu temalı WPF kabuğu, ana menü, yeni kariyer, kariyer merkezi,
  canlı zamanlamalı yarış hafta sonu
- Velopack ile kurulum paketi ve otomatik güncelleme, 3 GitHub Actions iş akışı
- 51 birim testi, ~5.000 satır C#, `carsets/F1 2019/` (8 MB gerçek veri + görsel)

Kullanıcıya "mevcudun üstüne devam edelim mi, yoksa gerçekten sıfırdan mı?" diye açıkça
soruldu ve **"Gerçekten sıfırdan"** kararı verildi. v1 kodu, carset'i, marka kiti ve
fontları **tamamen kaldırılacaktır**. v1'in kendisi git geçmişinde `a83ebcc` commit'inde
korunur; istenirse oradan bakılabilir.

> **Neden bu makul bir karar?** Sırf inatçılık değil — üç somut gerekçesi var:
> 1. v1'in kariyer katmanı tek oyuncu rolü (pilot) varsayımı üzerine kurulu. LTF2'nin
>    kariyeri **Takım Patronu** derinliği (bütçe/Ar-Ge/transfer/yönetim kurulu) üzerine
>    kurulacağı için bu katman zaten baştan yazılacaktı.
> 2. v1'in arayüzü WPF olduğu için Linux CI'da **hiç derlenmiyor** — bu yüzden
>    `LightsToFlag.CI.slnf` filtresi var. Arayüz hataları ancak elde yakalanıyor.
> 3. v1'in simülasyonu **tur bazlı**. İstenen derinlik (slipstream, delta, sektör
>    zamanlaması, gerçek geçiş mücadelesi) sektör bazlı bir modele geçmeyi gerektiriyor.
>
> Yani "sıfırdan" kararı, zaten yeniden yazılacak üç katmanın yeniden yazılmasını
> resmileştiriyor.

---

## 2. Karar günlüğü

Bu kararlar bağlayıcıdır. Değiştirmek isterseniz gerekçesiyle birlikte bu tabloyu
güncelleyin ve `docs/adr/` altına yeni bir kayıt düşün.

### ADR-0001 — Sıfırdan inşa

| | |
|---|---|
| **Karar** | v1 kodu tamamen silinir, mimari baştan kurulur. |
| **Gerekçe** | Kariyer katmanı, arayüz teknolojisi ve simülasyon modeli zaten yeniden yazılacaktı (bkz. 1.1). Yamalamak yerine temiz bir temel. |
| **Feda edilen** | ~5.000 satır çalışan kod ve 51 test. Yeniden kazanılması Faz 0–1 sürecek. |
| **Karşı görüş** | v1 üzerine eklemek çok daha hızlı olurdu; bu bilinçli olarak reddedildi. |

### ADR-0002 — .NET 9 + Avalonia

| | |
|---|---|
| **Karar** | Arayüz **Avalonia UI** (11.x) ile yazılır; hedef çatı **.NET 9**. |
| **Gerekçe** | Aynı C# bilgisi. **Ürün hedefi Windows + macOS** (ADR-0009); Linux ayrıca CI/geliştirme tezgâhı. Kritik kazanç: **tüm çözüm — arayüz dahil — Linux CI'da derlenir** ve `Avalonia.Headless` ile arayüz testleri koşar. v1'de arayüz CI'da hiç test edilmiyordu. |
| **Feda edilen** | WPF'in olgun kontrol/tema ekosistemi ve hazır üçüncü parti bileşenler. Bazı kontroller elle yazılacak. |
| **Alternatifler** | WPF (Windows'a hapsolmak), Electron/TypeScript (motoru C#'tan taşımak), Godot (yönetim ekranları/tablolar için zahmetli). |
| **Tasarım kaynağı** | Arayüzün **görsel tasarımı kullanıcının Claude Design mockup'larından** gelir (HTML/CSS çıktı). Bu çıktı **doğrudan kullanılmaz**; tasarım referansı (mockup) olarak alınıp **Avalonia'ya birebir çevrilir**. Tasarım kullanıcının, uygulama bizim. Blazor Hybrid (HTML'i doğrudan kullanmak) değerlendirildi ve reddedildi — tek .NET yığını, Windows + macOS ve arayüzün Linux CI'da headless test edilmesi kazançları korunuyor. |

### ADR-0003 — Her şey sıfır: içerik ve marka dahil

| | |
|---|---|
| **Karar** | Yeni veri formatı, elle yazılmış **kurgusal** örnek carset, yeni marka kiti (palet + logo), yeni tipografi. |
| **Gerekçe** | Kararın kendisi kullanıcıya ait. Yan fayda önemli: v1'in `carsets/F1 2019/` klasöründe **107 gerçek pilot fotoğrafı** ve gerçek takım/pilot/sponsor isimleri var; lisans durumu belirsiz. Kurgusal içerik bu riski tümden ortadan kaldırır ve oyunu dağıtılabilir kılar. |
| **Dürüst not** | Font "yazmak" gerçekçi değil. Yapılacak olan: yeni bir **OFL lisanslı font seti seçmek** (v1'in Saira Condensed / Chakra Petch / Archivo üçlüsü kullanılmayacak). Logo ve paletin kendisi sıfırdan üretilir (SVG). |
| **Sonuç** | İlk oynanabilir içerik: **"Global Prix Series"** — 10 kurgusal takım, 20 pilot, 20 pist. |

### ADR-0004 — Tek kariyer modu: Takım Patronu (Rev 26'da revize)

| | |
|---|---|
| **Karar** | Oyun **tek kariyer modu** taşır: **Team Principal (Takım Patronu)**. **Pilot Kariyeri rafa kaldırıldı** (post-1.0; silinmedi). |
| **Neden** | Odak tek moda: derinlik Takım Patronu deneyiminde (pit duvarı / bütçe / Ar-Ge / transfer / yönetim kurulu — ADR-0025). Güncel UI mockup'ı da Team-Principal-only. |
| **Etki** | Kod yok (Career yazılmadı). **M16 rafta, M17 tek kariyer yüzeyi**; pilot-ajans kancaları (ADR-0013/0015/0021) M17'ye daralır. Ayrıntı: `docs/adr/0004`. |

### ADR-0005 — Öncelik: yarış derinliği → yönetim → içerik

| | |
|---|---|
| **Karar** | Faz 1 yarış motorunun derinliği, Faz 2 yönetim katmanı, Faz 4 içerik/araçlar. |
| **Gerekçe** | Kullanıcının açık sıralaması. Ayrıca teknik olarak da doğru: yönetim kararlarının (Ar-Ge, strateji, lastik tahsisi) *anlamlı* olması için önce simülasyonun o kararlara tepki vermesi gerekir. |
| **Sonuç** | Faz 1 bitmeden Faz 2'ye geçilmez — Ar-Ge sistemi, simülasyon gelişimi hissetmiyorsa boş bir sayı oyunudur. |

### ADR-0006 — Belge Türkçe, oyun İngilizce

| | |
|---|---|
| **Karar** | Planlama ve karar belgeleri Türkçe; oyun içi metin, kod, commit mesajları ve `README.md` İngilizce. |
| **Gerekçe** | Kullanıcının tercihi. Oyunun İngilizce olması dağıtım ve topluluk açısından da doğru. |
| **Not** | Yerelleştirme altyapısı (M19) baştan kurulur ki Türkçe dil paketi ileride bir çeviri dosyası eklemekten ibaret olsun. |

### ADR-0007 — Mod sistemi ve lisanslı içerik

| | |
|---|---|
| **Karar** | İçerik üç aşamada ilerler: (1) **kurgusal** carset (varsayılan, oyunla gelir) → (2) **lisanslı içerikli modlar** (gerçek takım/pilot isimleri) → (3) **birden çok modu bir arada koşturan kariyer sezonu**. |
| **Gerekçe** | Kullanıcının açık isteği. Kurgusal temel oyunu güvenle dağıtılabilir kılar; lisanslı içerik ayrı bir katman olarak eklenir. |
| **Lisans sınırı** | Gerçek isimli içerik yalnızca **kullanıcı modu** olarak var olur; ana dağıtıma **dahil edilmez**. Bu, telif/lisans riskini oyunun kendisinden ayırır (ADR-0003 ile aynı mantık). |
| **Teknik sonuç** | İçerik formatı **M2'den itibaren katmanlamaya (overlay/layering) hazır** tasarlanır: bir mod, temel carset'in üstüne isim/görsel/ayar bindirebilmeli. Mod sistemi özelliğinin kendisi Faz 4'te (M27–M28) gelir. |

### ADR-0008 — Çok oyunculu co-op (online), 1.0 sonrası

| | |
|---|---|
| **Karar** | Football Manager tarzı, **4 oyuncuya kadar çevrimiçi ortak kariyer**: her oyuncu bir takım yönetir, sezon birlikte ilerler. |
| **Yerleşim** | Yol haritasının **en sonu, ayrı faz (Faz 6)**. Tek oyunculu oyun (Faz 0–5) tamamlanıp **1.0** çıkmadan başlanmaz. |
| **Gerekçe** | Ağ ve senkronizasyon, tek oyunculu oyunun tüm sistemleri oturduktan sonra en düşük riskle eklenir. Deterministik motor (ADR-0002/4.2) burada büyük avantaj: paylaşılacak durum küçük, senkronizasyon doğal. |
| **Feda edilen** | Çok oyunculu 1.0'da yok; 2.0 hedefi. |

### ADR-0009 — Hedef platformlar: Windows + macOS

| | |
|---|---|
| **Karar** | Ürün **Windows ve macOS**'ta yayınlanır. **Linux bir dağıtım hedefi değildir**; yalnızca CI/geliştirme tezgâhı. |
| **Gerekçe** | Kullanıcı isteği. Avalonia (ADR-0002) zaten Win+Mac'i tek yığınla verir; Linux'ta derlenebilmesi ücretsiz bir test kazancıdır, yayın sözü değil. |
| **Sonuç** | CI: Windows + macOS artık ürünün çalışması gereken platformlar olduğu için orada da **test edilir** (yalnızca derlenmez); Linux hızlı tezgâh olarak kalır. Paketleme (M32) yalnızca Windows + macOS; Linux AppImage yok. |

### ADR-0010 — Regülasyon & Yönetişim (FIA)

| | |
|---|---|
| **Karar** | Canlı bir regülasyon/yönetişim katmanı: yönetişim organı kuralları koyar, sezon içi uyumu denetler, sezon sonu değiştirir. Kapsam **teknik + sportif + finansal**; ceza hem **araca** hem **takıma**. |
| **Kontrol** | **FIA (sistem) yönetir + Patron modunda oyuncu oy/lobi yapar** (F1 Komisyonu tarzı). |
| **İhlal** | İki kaynak: oyuncunun **bilinçli risk alması** (sınırı zorlama) + **AI/sistem olayları**. İtiraz süreci var. |
| **Evrim** | Sezon sonu kural değişikliği; hazırlıksız takımlar yeni sezona **performans cezasıyla** başlar (Ar-Ge ile `Readiness`). |
| **Dağılım** | Kesişen sistem: M7 (on-track ceza), M11/M13 (uyum+finans), M14 (hazırlık), M17 (lobi), M18 (değişiklik+geriye düşme), M22 (arayüz). Ayrıntı: `docs/adr/0010`. |

### ADR-0011 — Takvim-tabanlı kariyer (FM tarzı)

| | |
|---|---|
| **Karar** | Kariyer **oyun-içi takvim üzerinde gün gün** ilerler (FM modeli): oyun tarihi + tarihli olay takvimi + **"Devam"** ile sonraki olaya kadar hızlı geçme. Round-index (v1) yerine tarih-güdümlü. |
| **Determinizm** | Oyun tarihi bir oyun durumudur, duvar saati değil (`DateTime.Now` yok — §4.2). Kayıt tarihi + olay kuyruğunu birebir saklar. |
| **Dağılım** | Ağırlıkla M11 (saat+takvim+Continue); M12/M14/M15 (zaman-planlı); M18 (transfer+devir); M21 (Continue+gelen kutusu+takvim). Ayrıntı: `docs/adr/0011`. |

### ADR-0012 — Olay kataloğu & hassasiyet politikası

| | |
|---|---|
| **Karar** | F1 tarihinden ilham alan olaylar **tür olarak** entegre edilir (isim değil); veri-güdümlü `IncidentCatalog`, carset-tunable/moddable. |
| **Hassasiyet** | Ölümlü/ağır trajedi oynanabilir olay olarak modellenmez → soyut DNF/sakatlık; grafik detay veya gerçek kurban ismi yok; kurtarma aracı/görevli çarpması modellenmez. İsimli canlandırmalar yalnızca opsiyonel mod (ADR-0007). |
| **Determinizm** | Her olay şablonu ruloları `DeterministicRandom` fork'larından çeker; aynı tohum → aynı olay dizisi. |
| **Dağılım** | M5b (mekanik ✅), M5c (pilot/çarpışma/start), M5d (çevre→nötralizasyon), M5e (olay günlüğü→M23/M24 + ilişki bağı ADR-0013); F/G→M7/ADR-0010. Ayrıntı: `docs/adr/0012`. |

### ADR-0013 — Paddock ilişkileri & insan dinamikleri

| | |
|---|---|
| **Karar** | Tüm insan aktörler (pilot/patron/personel) arası ikili ilişki: yakınlık + tür + kişilik. Mekanik etki: moral, pazarlık, takım emri, veri paylaşımı, personel ayrılığı, takım-içi çarpışma olasılığı, mentorluk. |
| **Çift yön** | Olay → ilişki (çarpışma/favoritizm; `RaceEvent` katılımcı id'leri) **ve** ilişki → olay (bozuk ilişki → takım-içi kasıtlı engelleme). |
| **Dağılım** | M11/M12 (doğuş+kalıcı), M14 (personel), M17 (patron ajansı), M18 (evrim/feud), M21/M22 (arayüz). Ayrıntı: `docs/adr/0013`. |

### ADR-0014 — Hukuk & tahkim (paddock mahkemesi)

| | |
|---|---|
| **Karar** | Herhangi bir konuda dava (regülasyon/sözleşme/ticari/IP/personel); **avukat tutulur** (retainer + dava ücreti; ekonomi kalemi). ADR-0010 itirazının avukatlı/masraflı derinleşmesi. |
| **Süreç** | Takvimde açılış→duruşma→karar; deterministik (esas × avukat × yönetişim + tohum). Sonuç: ceza/puan/iade/tazminat/tedbir. Kurgusal/isimsiz. |
| **Dağılım** | M11/M12/M13/M14/M17/M18/M21/M22. Ayrıntı: `docs/adr/0014`. |

### ADR-0015 — Pilot gelişimi (potansiyel + yaş eğrisi + antrenman)

| | |
|---|---|
| **Karar** | Potansiyele büyüme + **zirve-sonrası yaş gerilemesi** (fiziksel hızlı, deneyim yavaş) + uzun vadeli **antrenman programı**. M8 serbest seansından ayrı. |
| **Dağılım** | M14 (koç/simülatör altyapısı), M17 (patron: iki pilotun antrenmanı + akademi), M18 (yaş eğrisi), M24 (grafik), ADR-0013 (mentorluk). Faz 1 kod değişmez. Ayrıntı: `docs/adr/0015`. |

### ADR-0016 — R&D geliştirme ağacı (tech tree)

| | |
|---|---|
| **Karar** | R&D dallı bir **tech tree**; sezonlar boyu, aracın **konsept yönünü** belirler (fırsat maliyeti → takımlar farklılaşır). Bütçe/tesis/personel + takvim hızlandırır. |
| **Sinerji** | Regülasyon değişikliği dalları **budayabilir/kilitleyebilir** (Readiness ↔ ağaç). Deterministik. |
| **Dağılım** | M14 (ana), M13/M15/M17/M18/M22. Ayrıntı: `docs/adr/0016`. |

### ADR-0017 — Pilot havuzu & scout (keşif) sistemi

| | |
|---|---|
| **Karar** | Dış-seri (F2/F3/DTM/WEC) + free-agent **pilot havuzu**; **scout ekibi** gizli nitelik/potansiyeli keşfeder (rapor = aralık + güven; kaynak/scout yeteneği kesinliği artırır). |
| **Dağılım** | M18 (transfer/regen kaynağı), M12 (imzalama), M14 (scout işe alım), M17 (yönlendirme), M22 (ekran + bildirim). Deterministik. Ayrıntı: `docs/adr/0017`. |

### ADR-0018 — Regülasyon çağları (DRS ↔ 2026)

| | |
|---|---|
| **Karar** | Motor **çoklu regülasyon çağı** koşar; carset seçer. `DrsEra` (2024/25) ve `ActiveAero2026` (DRS yok → aktif aero X/Z + enerji-bağlı Manuel Override + de-rating). ADR-0010 `regulations` bloğunun ilk somut parçası. |
| **Determinizm** | Enerji/aero/override **RNG'siz aritmetik** → DrsEra byte-özdeş (geriye uyum). Kural adları jenerik; gerçek 2026 ayrı mod. |
| **Dağılım** | 26a ✅ (çağ + Override + enerji), 26b (aktif aero X/Z), 26c (kalibrasyon + carset config). Ayrıntı: `docs/adr/0018`. |

### ADR-0019 — Araç yönetim modülü (Vehicle)

| | |
|---|---|
| **Karar** | Oyuncu **Teknik Direktör** gibi aracı yönetir (FM/MM derinliği); her parça geçmiş, her eylem kalıcı kayıt; Finance/Staff/R&D/Manufacturing/Regulations ile bağlı. Upgrades → ADR-0016/0024 araç-tarafı yüzeyi (çift çalışma yok). |
| **Yeni** | Manufacturing (üretim kuyruğu), Inventory/lojistik, Homologation/BoP, Setup, Livery, Electronics, Development-history. |
| **Dağılım** | M1/M5/M14/M15/M8/M13/M24, **M22 (Faz 3 Vehicle merkezi)**. Alt-sistem H. Ayrıntı: `docs/adr/0019`. |

### ADR-0020 — Tesis & fabrika modeli

| | |
|---|---|
| **Karar** | Tesisler **puan vermez**; geliştirmenin kapasite/hız/doğruluk/kalite/riskini belirler. Level 1–5; personel tamamlayıcı. Rüzgâr tüneli/CFD ATR kotasını artırmaz, verimini belirler (ADR-0010). |
| **Dağılım** | M14 (ana), M13/M15, ADR-0015 (simülatör), M7 (pit crew), ADR-0016, M22. Alt-sistem I. Deterministik. Ayrıntı: `docs/adr/0020`. |

### ADR-0021 — Medya & diyalog motoru (paddock etkileşimi)

| | |
|---|---|
| **Karar** | Mekân-tabanlı **diyalog + medya + delegasyon**. Medya metrikleri (reputation/popularity/credibility/pressure/narrative heat), basın tetikleri, cooldown/hafıza, **bulanık ilişki gösterimi**. ADR-0013 + Rev 15 üstünde birleşir. |
| **Dağılım** | M11/M12/M17, M21 (Paddock Hub), M22, M23 (mekânlar). Alt-sistem J. Deterministik. Ayrıntı: `docs/adr/0021`. |

### ADR-0022 — Dinamik Dünya Sistemi (yaşayan evren)

| | |
|---|---|
| **Karar** | Dünya oyuncudan bağımsız yaşar: **7 ekosistem** (takım/sürücü/personel/üretici/sponsor/regülasyon/global) + haber+söylenti motoru + tarih DB + hall of fame. Mevcut ADR-0015/0017/0010/0018'in derinleşmesi. |
| **Determinizm & hassasiyet** | Tüm evrim **tohumlu**; içerik kurgusal/isimsiz (gerçek isim yalnız mod); global olay soyut (ADR-0012). |
| **Dağılım** | **M18 ana yüzey**, M11/M13/M14/M24, Rev 15/21. Alt-sistem K. Ayrıntı: `docs/adr/0022`. |

### ADR-0023 — Veri boru hattı & modlama mimarisi

| | |
|---|---|
| **Karar** | Ham veri **doğrudan** puana dönüşmez: `ham → doğrula → ana DB → denge katmanı (mod_balance) → carset/mod`. Kaynak/lisans izlenebilir (TracingInsights Apache-2.0 + NOTICE; StatsF1 yalnız doğrulama, scrape yok). ADR-0007 derinleşmesi. |
| **Dağılım** | M2 (format), M10 (denge), M26/M27/M28 (araçlar), ADR-0022. Alt-sistem L. Seed'li → tekrar-oynatılabilir. Ayrıntı: `docs/adr/0023`. |

### ADR-0024 — Araç puanlama modeli & R&D doğrulama

| | |
|---|---|
| **Karar** | R&D üç ADR: **0016 (ne) + 0020 (ne kadar iyi) + 0024 (nasıl ölçülür)**. 12-eksen 500-merkezli araç puanı (M1 Car'ın Faz 2 inceltmesi); **puan anında artmaz** — In Design→…→Approved for Race sonrası kalıcılaşır; pist-doğrulama + korelasyon riski + test planları. |
| **Bağlar** | Sürücü `technical_feedback`/`adaptability` (ADR-0015); cost-cap (ADR-0010); istihbarat **bulanık, casusluk yok** (medya/ağ); AI kişilikleri (ADR-0022); veri dosyaları (ADR-0023). |
| **Dağılım** | **M14 (ana)**, M1/M4/M5b/M15/M8/M13/M22. Alt-sistem M. Deterministik. Ayrıntı: `docs/adr/0024`. |

### ADR-0025 — Yönetim kurulu, sahiplik & baskı

| | |
|---|---|
| **Karar** | Kısa-vade baskı ↔ uzun-vade plan gerilimi. 6 sahiplik tipi, çok-üyeli kurul, **6 bağımsız baskı metriği**, müzakere edilebilir hedefler, takvim-ritmli toplantılar. DWS (ADR-0022) sahipliğinin oyuncu-tarafı yüzeyi. |
| **Dağılım** | **M17 (ana)**, M13/M11/M12/M24, Rev 21 (medya baskısı), Rev 15 (bildirim). Alt-sistem N. Deterministik. Ayrıntı: `docs/adr/0025`. |

---

## 3. Ürün vizyonu

> **Lights to Flag 2**, veriye dayalı (data-driven), deterministik bir motorsporu
> kariyer simülasyonudur. Oyuncu **pit duvarına** geçer — ve sonucu belirleyen şey
> refleks değil, karardır.

**Takım Patronu (Team Principal)** — tek kariyer modu. Bütçe, sponsorlar, personel,
Ar-Ge programları ve pilot transferleri sizin. Yarış günü iki aracın stratejisini pit
duvarından yönetirsiniz. Yönetim kurulunun hedefleri var ve sabrı sonsuz değil
(ADR-0025). *(Pilot Kariyeri şimdilik rafta — post-1.0 fikir; bkz. ADR-0004.)*

**İleride (2.0): Çok oyunculu co-op** — Football Manager tarzı, 4 oyuncuya kadar
çevrimiçi ortak kariyer: her oyuncu bir takımın patronu olur, aynı şampiyonayı
birlikte yaşar. Tek oyunculu 1.0'dan sonra, ayrı bir faz olarak gelir (bkz. Faz 6).

**v1'e göre fark nerede?** v1 "yarışı simüle et, puanı yaz" seviyesindeydi. LTF2'nin
iddiası derinlik:

| | v1 | LTF2 hedefi |
|---|---|---|
| Simülasyon | Tur bazlı | Sektör bazlı, slipstream/kirli hava/delta |
| Strateji | Motor kendi karar veriyor | Oyuncu pit penceresi ve bileşim seçiyor |
| Kurallar | Carset alanlarının çoğu okunuyor ama kullanılmıyor | Hepsi motorda karşılık buluyor |
| Ekonomi | Yok | Sponsor, bütçe, maaş, ödül parası |
| Gelişim | Yok | Sezon içi Ar-Ge, test günleri, tesisler |
| Zaman | Round-index ("sıradaki yarış") | **Takvim-tabanlı, gün gün** (FM tarzı Continue) |
| Regülasyon | Statik | **Canlı FIA katmanı:** sezon-içi ihlal→ceza, sezon-sonu kural değişikliği→hazırlıksız geriler |
| Tarih | Sadece şampiyon adı | Kalıcı rekorlar, şeref listesi, profiller |
| Öğrenme | Yok | Rehberli öğretici + sözlük |

---

## 4. Mimari

### 4.1 Çözüm yapısı

```
LightsToFlag2.sln
├─ src/
│  ├─ LTF.Domain/          net9.0   Saf model. I/O yok, bağımlılık yok.
│  ├─ LTF.Content/         net9.0   Carset formatı: şema, yükleyici, doğrulayıcı.
│  ├─ LTF.Simulation/      net9.0   Antrenman / sıralama / yarış. Deterministik.
│  ├─ LTF.Career/          net9.0   Sezon, kariyer, ekonomi, sözleşme, Ar-Ge, transfer (tek mod: Takım Patronu).
│  ├─ LTF.Persistence/     net9.0   Kayıt/yükleme + şema göçü (migration).
│  ├─ LTF.App/             net9.0   Avalonia arayüz. Tüm platformlar.
│  └─ LTF.Tools/           net9.0   CLI: doğrulayıcı, denge süpürmesi, carset editörü.
└─ tests/
   ├─ LTF.Domain.Tests/
   ├─ LTF.Content.Tests/
   ├─ LTF.Simulation.Tests/          + altın dosya (golden file) testleri
   ├─ LTF.Career.Tests/
   └─ LTF.App.Tests/                 Avalonia.Headless — Linux'ta koşar
```

**Bağımlılık yönü tek yönlüdür:** `App → Career → Simulation → Content → Domain`.
Ters bağımlılık bir koruma testiyle engellenir.

### 4.2 Değişmez kurallar

`Directory.Build.props` ve koruma testleriyle zorlanır:

- `Nullable=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`
- `Domain`, `Content`, `Simulation`, `Career` katmanlarında **UI yok, dosya sistemi
  erişimi yok** (Content yalnızca kendisine verilen akışları/metni ayrıştırır)
- **Simülasyon deterministiktir:** duvar saati yok, `Random.Shared` yok, `DateTime.Now`
  yok, paralel çalıştırmada sıra bağımlılığı yok. Aynı tohum → bit bit aynı sonuç.
- **`InvariantGlobalization` asla `true` yapılmaz.** v1'de bunu açmak "New Career"
  ekranını `CultureNotFoundException` ile çökertti. Tuzak burada yazılı kalsın.
- Kültüre duyarlı ayrıştırma/biçimlendirme her yerde açıkça `CultureInfo.InvariantCulture`
  ile yapılır (veri dosyaları için) veya `CurrentCulture` (kullanıcıya gösterim için) —
  varsayılana güvenilmez.

### 4.3 İçerik (carset) formatı

Eski alt-çizgili tek satırlık metin formatı bırakılır. Yerine sürümlenmiş JSON:

```
content/carsets/<id>/
  carset.json      # Künye: id, ad, sürüm, yazar, schemaVersion
  rules.json       # Seri kuralları (puan tabloları, sıralama biçimi, kotalar…)
  balance.json     # Simülasyon ayar katsayıları
  teams.json
  drivers.json
  circuits.json
  media/           # Görseller
```

Neden JSON: elle okunabilir, git ile diff'lenebilir, `System.Text.Json` kaynak
üreticisiyle (source generation) yansımasız (reflection-free) ve hızlı okunur, şema
sürümlemesi ve göç (migration) doğal. Her dosyanın şeması `docs/content-schema.md`
altında belgelenir ve `LTF.Tools validate` ile denetlenir.

---

## 5. Fazlar ve kilometre taşları

Kilometre taşı başına kabaca 1–3 oturumluk iş öngörülüyor; ⬛ işareti daha büyük
parçaları gösterir.

### Faz 0 — Temel · M0–M2

| | Kilometre taşı | İçerik |
|---|---|---|
| **M0** | Sıfırlama + iskelet | Eski `src/`, `tests/`, `carsets/`, `.sln`, `.slnf` silinir. Yeni çözüm iskeleti, .NET 9, `Directory.Build.props`, `.editorconfig`, CI (Linux + Windows + macOS'ta **build + test**; Linux tezgâh, Win+Mac hedef platform), `docs/adr/` klasörü, **`README.md` İngilizce yeniden yazılır** (v1'i anlatan mevcut metin geçersiz kalacak). |
| **M1** ✅ | Domain modeli | Pilot, takım, pist, kural seti, katsayılar — **artı v1'de olmayanlar:** personel (Technical Director/Chief Aerodynamicist/Chief Strategist/Race Engineer), sözleşme ve maddeleri, finansal kalemler, araç bileşenleri (motor/şanzıman/fren) ve kullanım kotaları, sponsor, tesis, itibar/moral. Hepsi değişmez `record`. Attribute kelime dağarcığı UI mockup'ıyla hizalı. `LTF.Domain` + testler + mimari koruma testi; CI yeşil. |
| **M2** ⬛ | İçerik formatı + ilk carset | JSON şeması + yükleyici + **doğrulayıcı** (hatalı carset'i anlamlı mesajla reddeder). **İki içerik akışı:** (1) yayınla gelen **kurgusal** örnek carset (10 takım, 20 pilot, ~24 pist) — isimler mockup'tan (Talon Racing, Kuro Dynamics…; Mateo Ferreira, Idris Whitlock…), **gerçek 2024/2025 verisiyle kalibre** (statsf1.com referans: takvim, pist özellikleri, performans sıralaması, puanlama). (2) Gerçek isimli **2024/2025 sezonu ayrı bir mod** olarak `content/mods/` altında — **yayınla dağıtılmaz** (ADR-0007). Görseller SVG yer tutucu. **Format baştan ileriye dönük hazır:** mod katmanlaması/overlay (ADR-0007), her round/olay için **`date` alanı** (takvim-tabanlı kariyer, ADR-0011) ve opsiyonel **`regulations` bloğu** (başlangıç kuralları + yönetişim sıkılığı, ADR-0010). Bu üç kanca formatta yer tutar; ilgili özellikler Faz 1–4'te gelir. |

### Faz 1 — Yarış derinliği · M3–M10 ← *1. öncelik*

| | Kilometre taşı | İçerik |
|---|---|---|
| **M3** ⬛ | RNG + sektör bazlı tur zamanı | Tohumlanabilir, çatallanabilir (`Fork`) RNG. Tur zamanı = pist temel süresi × (pilot yeteneği ⊗ araç performansı ⊗ pist karakteri) + gürültü, **sektör sektör**. v1 tur bazlıydı; sektör bazlı olması canlı zamanlamayı, delta'yı ve slipstream'i mümkün kılan temeldir. |
| **M4** ⬛ | Lastik · yakıt · hava | Lastik: bileşim, aşınma eğrisi, sıcaklık penceresi, graining, **performans uçurumu (cliff)**. Yakıt: yük cezası, tüketim. Hava: dinamik değişim, pist ıslaklığı, slick↔ara↔yağmur **geçiş noktaları**, kuruyan pist. |
| **M5** ⬛ | Güvenilirlik + olaylar | Bileşen bazlı arıza (motor/şanzıman/fren/hidrolik), pilot hatası, çarpışma, spin. **Güvenlik aracı, VSC ve kırmızı bayrak** — tam prosedürleriyle (pit yolu kapanması, tur turlama, yeniden start). Olaylar F1 tarihinden ilham alan **genel bir olay kataloğundan** gelir (isim yok, trajedi canlandırması yok — ADR-0012). Alt-adımlar: **M5a ✅** (yarış motoru iskeleti), **M5b ✅** (bileşen sağlığı + motor modları + mekanik arıza/DNF + limp + olay günlüğü), **M5c ✅** (pilot hatası + çarpışma + start/ilk-viraj), **M5d ✅** (nötralizasyon: SC/VSC/kırmızı durum makinesi + restart/bunch + serbest lastik), **M5e ✅** (zengin telemetri: sektör/interval/lastik/yakıt/top speed + tam-telemetri determinizm testi). **M5 tamam.** |
| **M6** ✅ | Trafik ve geçiş | Kirli hava (dirty air), slipstream/DRS bölgeleri, blokaj, **gerçek geçiş mücadelesi**: yakın takipteki hızlı araç kirli havada tutulur; geçiş olasılığı = pist zorluğu (`Circuit.Overtaking`) × pace avantajı × racecraft (atak/defans) × DRS/slipstream; başarı → pozisyon + `Overtake` olayı, başarısızlık → tutulma (zaman kaybı). Ayrı trafik RNG akışı (determinizm). **Turlanan araçlar / mavi bayrak** şematik modelde soyut (alt-tur konum yok) — pozisyonel modelle ileride. |
| **M7** ✅ | Pit + strateji + cezalar | Pit stop süre dağılımı, güvensiz bırakma, yakıt ikmali (kural açıksa). Her rakip için **strateji planı** (undercut/overcut yapabilen). Ceza sistemi: stop-go, drive-through, süre cezası, grid cezası, hatalı start, pist limitleri, sarı bayrak ihlali. **Regülasyon sisteminin on-track yaptırım yüzeyi** (ADR-0010): regülasyon ihlali → uygun yarış cezası. Alt-adımlar: **M7a ✅** (pit stop: süre dağılımı + kademeli spread + yavaş stop + bileşim rotasyonu, ayrı pit RNG akışı), **M7b ✅** (strateji: kademeli pit pencereleri = undercut/overcut temeli + güvenlik aracı altında indirimli fırsatçı stop), **M7c ✅** (on-track cezalar: pist limitleri strike sayacı + güvensiz pit bırakma süre cezası; hatalı start cezası M5c'den). Tam yarış-kontrol ceza türleri (stop-go/drive-through) ve grid cezaları M8/M9 seans-kural yüzeyinde, AI strateji planlayıcı Faz 2'de derinleşir. **M7 çekirdeği tamam.** |
| **M8** ✅ | Seans biçimleri | Sıralama: eleme usulü **Q1/Q2/Q3** (knockout), tek turlu, tek seans → başlangıç gridi (**QualifyingSimulator**, deterministik; grid slotu en derin ulaşılan part'a, sonra o part'taki en iyi tura göre). Antrenman: takım program seçer (kurulum / lastik denemesi / yarış simülasyonu) → deterministik **PracticeSetup** getirisi (sıralama + yarış tur kazancı, yarış-simülasyonu hata azaltımı) sıralamaya ve yarışa taşınır (opsiyonel; getiri yoksa yarış/sıralama bit bit aynı). Sprint shootout = knockout varyantı (part-başı zorunlu bileşim), **M9** sprint hafta sonuyla bağlanır. Takım-içi kasıtlı engelleme (Rev 13) — bu soyutlamada out-lap trafiği olmadığından **Faz 2** ilişki-güdümlü olay olarak gelir. |
| **M9** ✅ | Yarış biçimleri ve kural sadakati | **Race-level biçimler** — tek bir `RaceSimulator.Run` (veya hafta sonu) onurlandırır; hepsi deterministik ve byte-özdeş varsayılana kapılı, tek opsiyonel `RaceFormat` demetiyle. Alt-adımlar: **M9a** (RaceFormat + süreli/lap-override uzunluk), **M9b** (pole/lider-tur/en-çok-lider puanları, LapsLed sayımı), **M9c** (başarı balastı girdisi), **M9d** (çok-sınıf klasman: Team/Competitor `Class` + `ClassPosition`), **M9e** (rolling start + `DriversUnlapUnderSafetyCar` kuralı — lapped tespiti zaman-farkına göre), **M9f** (sprint puan tablosu seçimi), **M9g** (`GridOrder`: ters-grid / parça-ters / grid-ceza primitifi). **Sezon-seviyesi M11'e ertelendi** (sezon/şampiyona motoru gerektirir, yarım stub kurulmadı): ikincil şampiyona puanı, motor-şanzıman kota birikimi + grid cezaları (M9g `GridOrder.WithPenalties` primitifi hazır), playoff/chase, standings-güdümlü otomatik balast, sprint→feature hafta sonu zincirleme + sezon puan toplama. **İnce/ertelenen** (inert alan "her alanın motor etkisi olmalı" kriterini bozar): paylaşımlı pit boksu, quali-on-race-fuel, lastik-başı-bileşim envanteri. |
| **M10** ✅ | Determinizm + denge aracı | **Altın-dosya determinizmi**: kanonik yarışın (SimFixtures carset, seed 2024) tam Digest'inin (klasman + olay + telemetri) SHA-256 hash'i repoya çakılı ve Win/macOS/Linux üçünde birebir eşleşir — commit'ler ve platformlar arası bit-özdeşlik kilitlendi (M5e'nin ertelediği altın dosya). **`ltf sweep <carset> [seasons] [seed]`** (LTF.Tools) + I/O'suz `BalanceSweep` motoru (LTF.Simulation): N sezon başsız simüle eder (her round: sıralama gridi + yarış, puanları toplar, şampiyon), sonra şampiyonluk dağılımı / galibiyet yayılımı / terk oranı / güvenlik aracı sıklığı / ortalama pit sayısı raporlar — katsayı ayarı için. |

> **M9 hakkında önemli not:** Yukarıdaki kural özelliklerinin **tamamı v1'in carset
> formatında zaten alan olarak vardı ama motorda hiç kullanılmıyordu.** Doğrulandı —
> v1'de okunup atılan alanlar: `EnginesPerSeason`, `ChassisPerSeason`, üç balast alanı,
> `ChaseRound/ChaseDriverCount/ChasePoints`, `PointsForPole`, `PointsForLeadingLap`,
> `PointsForLeadingMostLaps`, `SecondaryPoints`, `RefuellingAllowed`,
> `QualifyingOnRaceFuel`, `BothDryCompoundsRequired`, `SharedTeamPitBox`,
> `StopGoPenaltySeconds`, `RollingStart`, `KnockoutQualifying`, `SingleLapQualifying`,
> `ClassCount`, `ClassNames`, `TyresPerCompound`, `DriversUnlapUnderSafetyCar` ve
> katsayılardan `InSeasonCarUpgradeRate`, `InSeasonDriverUpgradeRate`, `ChassisWear`,
> `EngineWear`, `BlockingCoefficient`, `RefuellingTime`, `DamageFixTime`.
> **LTF2'de bir alan varsa motorda karşılığı olacak** — bu bir kabul kriteridir.

### Faz 2 — Kariyer ve yönetim katmanı · M11–M18 ← *2. öncelik*

> Bu faz iki kesişen sistem taşır (aşağıda ayrı blokta): **Takvim-tabanlı kariyer**
> (ADR-0011, gün gün ilerleme) ve **Regülasyon & Yönetişim** (ADR-0010). İkisi de tek
> milestone değil; parçaları aşağıdaki kilometre taşlarına dağılmıştır.

| | Kilometre taşı | İçerik |
|---|---|---|
| **M11** ✅ | Dünya durumu + **takvim-tabanlı** sezon motoru | Sezon/şampiyona motoru + takvim/Continue + kalıcı rekorlar + kayıt/yükleme (`LTF.Career` + `LTF.Persistence`). Alt-adımlar: **M11a** (şampiyona sıralaması — pilot + konstrüktör, `ChampionshipStandings`), **M11b** (tek-sezon motoru — her round quali→grid→yarış, `SeasonSimulator`→`SeasonResult`; deterministik round-seed), **M11c** (**takvim + oyun saati**, ADR-0011: tarihe çakılı olay kuyruğu `SeasonCalendar`/`CalendarEvent`, `AdvanceDay()` / `ContinueToNextEvent()` — "Devam"), **M11d** (**kalıcı tarih ve rekorlar**: sezon sonuçları `DriverCareer` + takım geçmişine işlenir — yarış/galibiyet/podyum/pole/en hızlı tur/şampiyonluk/puan; `CareerRollover`), **M11e** (**kayıt/yükleme**: `CareerState` anlık görüntüsü → `LTF.Persistence.CareerStore` JSON, byte-özdeş round-trip). Sezon boyu **uyum durumu + ihlal/ceza kayıtları** (ADR-0010) ileride burada tutulur. **M9'dan ertelenen sezon-seviyesi kavramlar** (ikincil puan, kota+grid cezaları, chase/playoff, otomatik balast, sprint→feature zincirleme) bu motor üstünde ilerideki alt-adımlara bırakıldı. |
| **M12** ✅ | Sözleşmeler ve insan ilişkileri | Sözleşmeler + **ilişki grafiği doğuşu** (ADR-0013 "doğuş + pazarlık ilişkiyi okur"), Takım-Patronu odaklı, deterministik, kayıt korur. Alt-adımlar: **M12a** (ilişki domain modeli — signed `Affinity` −100…+100, `Personality` ego/sadakat/mizaç/hırs, `Relationship`/`RelationshipGraph`), **M12b** (sözleşme + personality carset JSON'dan yüklenir — `Carset.Contracts` + loader + validator), **M12c** (sözleşme yaşam döngüsü — `SeasonsRemaining` geri sayımı `ContractLedger` + takvimde `ContractDeadline` olayları `SeasonCalendar.ForCareer`, saat yüzeye çıkarır), **M12d** (yarışlar ilişkileri + morali evriltir — `RelationshipEvolution`: takım-içi çarpışma → affinity + moral düşer, personality-modüle, RNG yok), **M12e** (pazarlık ilişkiyi okur — `ContractNegotiation`: affinity/sadakat/itibar → kabul/ret), **M12f** (persistence — `CareerState` sözleşme + grafik + moral/itibar snapshot, byte-özdeş round-trip). **Ertelenenler:** personel uyumu/ayrılma (M14), favoritizm/takım-emri (M17), ilişki→olay/feud/mentorluk/transfer evrimi (M18), haber/ekranlar (M21/M22), tam hukuk/scout/kurul (M17/M14). Simülasyon ilişkiden habersiz kalır. |
| **M13** ✅ | Ekonomi | Sezon devrinde gelir/gider döngüsü + bütçe tavanı yaptırımı, deterministik, kayıt korur. Alt-adımlar: **M13a** (ekonomi kuralları — `EconomyRules`: pozisyona-göre ödül tablosu + TV geliri + yarış-başı işletme + kaza maliyeti; loader + validator), **M13b** (sponsor + personel carset JSON'dan yüklenir — `Team.Sponsors`/`Staff`), **M13c** (sezon geliri — `EconomyLedger`: ödül `ConstructorStanding.Position` + TV + sponsor `PerRaceFee`/`PerPointBonus`/objektif bonus → `Finances`), **M13d** (sezon gideri + net — sürücü/personel maaşları + işletme + kaza; `Balance += gelir − gider`), **M13e** (**bütçe tavanı yaptırımı**, ADR-0010: cap-içi harcama > `CostCap` → `CostCapPenalty` para cezası + puan silme + aero-test kısıtı; sürücü maaşı cap-dışı; `SeasonSettlement`), **M13f** (persistence — `TeamHistoryRecord`'a `Finances`, byte-özdeş round-trip), **M13g** (**çok-sezon denge süpürmesi** `EconomySweep` + `ltf sweep` ekonomi özeti: dengeli ekonomi bakiyeyi sabit tutar, gideri karşılamayan takım iflas eder; flagship carset ekonomi içeriği — 10 takım ödül/finans/sponsor/personel, süpürmede kimse iflas etmiyor / para birikmiyor). **Ertelenenler:** R&D fonlama harcaması (M14), kurul baskısı + hukuk ajansı (M17), yaşayan sponsor ekosistemi (M18), tesis yatırım/bakım (M14). |
| **M14** ✅ | Ar-Ge ve personel | **GENİŞ M14**: departman-bazlı hibrit tech tree + tam 10-tesis modeli + personel + pist-doğrulama döngüsü + lastik aşınma fiziği, deterministik, kayıt korur. Alt-adımlar: **M14a** (lastik aşınma ← araç nezaketi — `BalanceCoefficients.TyreGentlenessWearInfluence` `TyreModel`'e girer → nazik araç daha az aşınır → daha hızlı tur; varsayılan 0 → byte-özdeş, altın-hash korunur), **M14b** (`Facilities` 4→**10 tesis** `FacilityLevel` 1–5 — ADR-0020), **M14c–e** (R&D domain: `CarAxis` 12-eksen → `CarAxisMap` 5-derece, `NodeSize`, `TechNode`/`TechTree`, `ValidationState`/`DevelopmentProject`/`ConceptDirection`/`ResearchState`, `ResearchRules`), **M14f–g** (carset JSON loader + validator — tech tree + research + staffPool + rules), **M14h–k** (`ResearchLedger.DevelopSeason` — ilerleme hızı tesis+personelden ölçekli, bütçe harcar, **6-durumlu pist-doğrulama** döngüsü ADR-0024: `DataReview`'de tohumlu gerçekleşme → onay/rework/abandon, yalnız **ApprovedForRace** aracı kalıcı yükseltir; `StaffLedger` hire/release; regülasyon-hazırlık biriktirici), **M14l** (persistence — `TeamResearchRecord` plain-int + `CareerState.Research`, byte-özdeş round-trip), **M14m** (`ResearchSweep` çok-sezon: araç ölçülebilir/sınırlı/deterministik gelişir + daha donanımlı takım önde), **M14n** (flagship 4 departman + 16 düğüm + 10 takım tesis + `staffPool` + `ltf sweep` R&D özeti + `FlagshipResearchTests`; flagship lastik nezaket etkisini açar → `tyre_management` ekseni hissedilir). **Ertelenenler:** 12-eksen/500-merkezli araç inceltme + LapTimeModel genişleme (M1/Faz-2), günlük/haftalık tick (Faz-3 UI), sezon-içi güncelleme (M15), regülasyon budaması + hazırlıksız-ceza (M18), AI R&D + rakip istihbaratı (ADR-0021/0022), R&D ağaç arayüzü (M22), R&D harcamasını cost-cap'e katma (M17/M18). |
| **M15** | Test ve bileşen yönetimi | **Takvimdeki test günleri**, sezon içi güncellemelerin devreye alınması, bileşen tahsis takibi ve grid cezaları. |
| ~~**M16**~~ | **Pilot Kariyeri modu — RAFTA (post-1.0)** | **Ertelendi** (ADR-0004). Oyun tek kariyer modu taşır: **Takım Patronu (M17)**. Sürücü-kariyeri fikri kayıtlı; ileride aynı yaşayan dünya (ADR-0022) üstüne bir karar yüzeyi olarak eklenebilir (kariyer katmanı yeniden yazılmadan). |
| **M17** ⬛ | **Takım Patronu modu** (tek kariyer modu) | Uçtan uca: pit duvarından iki araç yönetimi, transfer, bütçe, **yönetim kurulu hedefleri, baskı ve kovulma riski** (ADR-0025). **Regülasyon politik katmanı** (ADR-0010): önerilen kural değişikliklerinde **oy/lobi**. |
| **M18** ⬛ | Sezon devri | Yaşlanma, gelişim/gerileme eğrileri, emeklilik, **yeni nesil pilot üretimi (regen)**, **transfer penceresi** (belirli tarihlerde; AI takımlar da hamle yapar). **Regülasyon değişikliklerini uygula + hazırlıksız takımları geriye düşür** (Readiness'e göre performans cezası — ADR-0010). |

#### Alt-sistem A — Takvim-tabanlı kariyer (ADR-0011)
Football Manager tarzı gün-gün zaman çizgisi. Oyun tarihi bir oyun durumudur (determinizm
korunur, `DateTime.Now` yok). **M11** temeli kurar (oyun saati, `SeasonCalendar`,
Continue/AdvanceDay); zaman-planlı sistemler M12 (sözleşme son tarihleri), M14 (Ar-Ge
ilerleme), M15 (test günleri), M18 (transfer/devir); arayüzü M21 (Continue + tarihli gelen
kutusu + takvim); kalıcılığı M31.

#### Alt-sistem B — Regülasyon & Yönetişim / FIA (ADR-0010)
Üç sütun: **(1) aktif kurallar** (teknik/sportif/finansal + `GoverningBody` denetim
sıkılığı) — domain M11 zamanında genişletilir; **(2) sezon içi uyum & yaptırım** —
ihlal iki kaynaktan (oyuncunun bilinçli riski + AI olayları), ceza araç+takım, on-track
M7, teknik/finansal M11/M13, itiraz süreci; **(3) evrim & hazırlık** — sezon sonu kural
değişikliği, `Readiness` (M14), hazırlıksız geriye düşer (M18); **politik katman** patron
modunda oy/lobi (M17); arayüz M22. Carset formatı opsiyonel `regulations` bloğu taşır (M2).

#### Alt-sistem C — Paddock ilişkileri & insan dinamikleri (ADR-0013)
Pilot/patron/personel arası ikili ilişkiler (yakınlık + tür + kişilik). **Çift yönlü:** olaylar
ilişkiyi değiştirir (çarpışma/favoritizm → `RaceEvent` katılımcı id'leri), bozuk ilişki olay
üretir (takım-içi kasıtlı engelleme). Mekanik etki: moral, pazarlık, takım emri, veri paylaşımı,
personel ayrılığı, takım-içi çarpışma olasılığı, mentorluk. Dağılım: M11/M12 (doğuş+kalıcı), M14
(personel uyumu), M17 (patron ajansı), M18 (evrim/feud), M21/M22 (arayüz). Simülasyon ilişkiden habersiz.

#### Alt-sistem D — Hukuk & tahkim (ADR-0014)
Herhangi bir konuda dava (regülasyon/sözleşme/ticari/IP/personel); avukat tutulur (retainer + dava
ücreti; ekonomi kalemi). ADR-0010 itirazının avukatlı/masraflı derinleşmesi. Takvimde
açılış→duruşma→karar; deterministik. Sonuç: ceza/puan/iade/tazminat/tedbir. Kurgusal/isimsiz.
Dağılım: M11/M12/M13/M14/M17/M18/M21/M22.

#### Alt-sistem E — Pilot gelişimi (ADR-0015)
Potansiyele büyüme + zirve-sonrası yaş gerilemesi (fiziksel hızlı, deneyim yavaş) + uzun vadeli
antrenman programı (M8 serbest seansından ayrı). Dağılım: M14 (koç/simülatör altyapısı), M17
(patron: iki pilotun antrenmanı + akademi), M18 (yaş eğrisi + büyüme), M24 (grafik), ADR-0013 mentorluk. Faz 1 kod
değişmez (Career yazar, Simulation okur).

#### Alt-sistem F — R&D geliştirme ağacı (ADR-0016)
Dallı tech tree; sezonlar boyu, aracın konsept yönünü belirler (fırsat maliyeti → farklılaşma).
Bütçe/tesis/personel + takvim hızlandırır. Regülasyon değişikliği dalları budayabilir (Readiness ↔
ağaç). **Hibrit yapı (Rev 30):** departman-bazlı ağaç (Aerodinamik/Şasi/PU/Dayanıklılık) +
**Minor/Major/Ultimate** düğüm boyutları; kategoriler ADR-0024 12-eksenine eşlenir (front/rear-DF/DRS
değil); ekonomi $ bütçe + CFD kotası (Resource Points yok); UI M22'de FM-yoğun **gezinilebilir
tech-tree görünümü** (radyal gösteri değil). Dağılım: M14 (ana), M13 (finansman), M15 (güncelleme),
M18 (taşıma/budama), M17 (yatırım), M22 (arayüz).

#### Alt-sistem G — Pilot havuzu & scout (ADR-0017)
Dış-seri (F2/F3/DTM/WEC) + free-agent havuzu; scout ekibi gizli nitelik/potansiyeli keşfeder
(rapor = aralık + güven; kaynak/scout yeteneği kesinliği artırır). Dağılım: M18 (transfer/regen
kaynağı), M12 (imzalama), M14 (scout işe alım), M17 (yönlendirme), M22 (ekran + bildirim). Deterministik.

#### Alt-sistem H — Araç yönetim modülü / Vehicle (ADR-0019)
Oyuncu Teknik Direktör gibi aracı yönetir; her parça geçmiş, her eylem kalıcı kayıt. Upgrades ADR-0016
tech tree'nin + ADR-0024 doğrulamanın araç-tarafı yüzeyi (çift çalışma yok). Yeni: Manufacturing üretim
kuyruğu, Inventory/lojistik, Homologation/BoP, Setup, Livery, Electronics. Dağılım: M1/M5/M14/M15/M8/M13,
**M22 (Faz 3 tam Vehicle merkezi)**, M24. M2 opsiyonel bileşen/tedarikçi verisi.

#### Alt-sistem I — Tesisler & fabrika (ADR-0020)
Tesisler puan vermez; geliştirmenin kapasite/hız/doğruluk/kalite/riskini belirler (Level 1–5, personel
tamamlayıcı). Rüzgâr tüneli/CFD ATR kotasını artırmaz, verimini + korelasyonunu belirler. Dağılım: M14
(ana), M13 (yatırım/bakım), M15 (kalite→güvenilirlik), ADR-0015 (simülatör), M7 (pit crew), M22.

#### Alt-sistem J — Medya & diyalog (ADR-0021)
Mekân-tabanlı diyalog + medya (reputation/popularity/credibility/pressure/narrative heat) + delegasyon.
Basın tetikte; cooldown/hafıza; ilişki ekranı bulanık (kesin puan yok). Rev 15 bildirim merkezi burada
Paddock Hub'a evrilir. Dağılım: M11/M12/M17, M21 (Hub), M22, M23 (hafta sonu mekânları).

#### Alt-sistem K — Dinamik Dünya (ADR-0022)
Oyuncudan bağımsız yaşayan evren: 7 ekosistem (takım/sürücü/personel/üretici/sponsor/regülasyon/global) +
haber+söylenti motoru + tarih DB + hall of fame. Tümü tohumlu; içerik kurgusal, global olay soyut. ADR-0015/
0017/0010/0018'in derinleşmesi. Dağılım: **M18 ana yüzey**, M11/M13/M14/M24, Rev 15/21. M2 opsiyonel dünya durumu.

#### Alt-sistem L — Veri boru hattı & modlama (ADR-0023)
Ham veri doğrudan puana dönüşmez: ham → doğrula → ana DB → denge katmanı (`mod_balance`) → carset/mod.
Kaynak/lisans izlenebilir (TracingInsights Apache-2.0 + NOTICE; StatsF1 yalnız doğrulama). Seed'li olaylar →
tekrar-oynatılabilir. Dağılım: M2 (format), M10 (denge), M26/M27/M28 (araçlar), ADR-0022.

#### Alt-sistem M — R&D derinliği / araç puanlama (ADR-0024)
R&D üç parça: ADR-0016 (ne) + ADR-0020 (ne kadar iyi) + ADR-0024 (nasıl ölçülür). 12-eksen 500-merkezli araç
puanı (M1 Car inceltmesi); **puan ancak "Approved for Race" sonrası artar** (pist-doğrulama + korelasyon +
test planları). Bulanık rakip istihbaratı (casusluk yok), AI kişilikleri, veri dosyaları. Dağılım: **M14
(ana)**, M1/M4/M5b/M15/M8/M13/M22.

#### Alt-sistem N — Yönetim kurulu & baskı (ADR-0025)
Kısa-vade baskı ↔ uzun-vade plan gerilimi. 6 sahiplik tipi, çok-üyeli kurul, 6 bağımsız baskı metriği,
müzakere edilebilir hedefler, takvim-ritmli toplantılar (ADR-0011). DWS (ADR-0022) sahipliğinin oyuncu-tarafı
yüzeyi. Dağılım: **M17 ana yüzey**, M13/M11/M12/M24, Rev 21 (medya baskısı), Rev 15 (bildirim).

### Faz 3 — Arayüz · M19–M25 — *Avalonia, oyun dili İngilizce*

> **Arayüz kaynağı (`design/`):** güncel tam UI mockup (`design/mockups/ui.dc.html`) + ekran-ekran
> spec (`design/ui/uidesign.md`) + 10-ekranlık **UI eksik listesi** (`design/ui/uieksikler.md`).
> Mockup **Team-Principal-only** (tek mod — ADR-0004) ve Faz 2 sistemlerini yansıtır (12-eksen araç
> puanı/ADR-0024, tesisler/ADR-0020, Paddock Hub/ADR-0021, board & cap room/ADR-0025). Ekran →
> milestone: **M19** kabuk (top bar + sidebar + tasarım sistemi: durum renkleri / kart / rozet /
> tooltip / onay / boş-yükleniyor-hata) · **M20** kariyer başlatma + ayarlar · **M21** Paddock Hub +
> Drivers + Standings + Database + Calendar · **M22** R&D & Tesisler + Cars & PU + Staff + Finance +
> Board & Sponsors + Transfer · **M23** yarış hafta sonu + canlı kontrol paneli · **M24** yarış-sonrası
> rapor + istatistik · **M25** öğretici. ADR-0002 gereği Avalonia'da yeniden üretilir (HTML shipped değil).

| | Kilometre taşı | İçerik |
|---|---|---|
| **M19** ⬛ | Kabuk + marka kiti | Navigasyon, tema sistemi. **Bildirim merkezi** (rozet + açılır panel; kategori/önem/derin bağlantı; tüm sistemlerden beslenir — Rev 15). **Marka kiti hazır** (`design/`): palet (Track Black/Lights Out Red/Flag White), SVG logo, fontlar (Saira Condensed/Chakra Petch/Archivo). Yerelleştirme altyapısı (ileride TR dil paketi). **Görsel tasarım kullanıcının Claude Design mockup'ından gelir** (`design/mockups/ui.dc.html`); HTML/CSS doğrudan kullanılmaz, Avalonia'ya birebir çevrilir (ADR-0002). Faz 3 ekranları (M20–M25) bu mockup'ı takip eder. |
| **M20** | Menü + kariyer başlatma | Ana menü, yeni **Takım Patronu kariyeri** akışı (takım seçimi + yönetim kurulu hedef müzakeresi), carset seçimi, kayıt-yükleme ekranı. |
| **M21** ⬛ | Kariyer merkezi | Pano, **takvim + FM tarzı "Devam" (Continue)** butonu (ADR-0011: tarihli olaylara kadar ilerlet), puan durumu, takım/pilot listeleri, **tarihli gelen kutusu / haber akışı** (sözleşme teklifleri, yönetim kurulu mesajları, regülasyon duyuruları, basın). Bu akış kabuktaki **bildirim merkezi** rozeti/panelinde toplanır (kategori/önem/derin bağlantı; eylem-gerektiren bildirim Continue'yu durdurur — Rev 15). |
| **M22** ⬛ | Yönetim ekranları | Finans, Ar-Ge, personel, tesisler, sözleşmeler (Takım Patronu). **Regülasyon & Uyum ekranı** (ADR-0010): aktif kurallar, kendi uyum/risk durumun, bekleyen değişiklikler, `Readiness` + Patron modunda **oylama** arayüzü. |
| **M23** ⬛ | Yarış hafta sonu | Canlı zamanlama kulesi, **şematik pist haritası + hareketli araç işaretçileri** (mockup SVG referansı; 3B değil — Motorsport/Football Manager tarzı, motor telemetriden beslenir), sektör renkleri ve delta'lar, strateji paneli (pit çağrısı, bileşim seçimi), telsiz mesajları, hız kontrolü / atlama / tekrar. |
| **M24** ⬛ | **İstatistik ve rekorlar** | Pilot ve takım profilleri, sezon istatistikleri, **tüm zamanların rekorları**, şeref listesi (hall of fame), kafa kafaya karşılaştırma, kariyer grafikleri, pist rekorları. |
| **M25** | **Öğretici** | Rehberli ilk hafta sonu, bağlama duyarlı ipuçları, terimler sözlüğü (undercut, graining, VSC…), yeni oyuncu için "önerilen ayar" profili. |

### Faz 4 — İçerik ve araçlar · M26–M29 ← *3. öncelik*

İçerik hattı (ADR-0007): kurgusal (M2, hazır) → **mod sistemi + lisanslı kullanıcı
modları** (M27) → **birleşik-mod kariyeri** (M28).

| | Kilometre taşı | İçerik |
|---|---|---|
| **M26** ⬛ | Carset editörü | Avalonia tabanlı editör: takım/pilot/pist/kural düzenleme, canlı doğrulama, önizleme, yeni carset oluşturma sihirbazı. |
| **M27** ⬛ | Mod sistemi + lisanslı modlar | **Katmanlama/overlay motoru**: bir mod temel carset'in üstüne isim/görsel/ayar bindirir; yükleme sırası, çakışma çözümü, mod paketleme/kurma. Böylece **lisanslı içerikli kullanıcı modları** (gerçek takım/pilot isimleri) mümkün olur — bunlar **kullanıcı içeriğidir, oyunla dağıtılmaz** (ADR-0007). |
| **M28** ⬛ | Birleşik-mod kariyer sezonu | Birden çok mod/seriyi **tek bir şampiyonada bir arada** koşturan kariyer sezonu (kullanıcının açık isteği). Farklı içerik paketlerinin aynı takvim/puanlama altında birleştirilmesi, sınıflandırma ve çakışma kuralları. Faz 1'in çok-sınıf motoruna (M9) dayanır. |
| **M29** | Yeni carsetler + modlama dokümanı | En az iki carset daha — biri **çok sınıflı** bir seri (M9'u gerçek içerikle sınamak için). Şema referansı, carset/mod yazma kılavuzu, denge ipuçları. İsteğe bağlı: eski LTF metin formatından içe aktarıcı. |

### Faz 5 — Cila ve dağıtım · M30–M33

| | Kilometre taşı | İçerik |
|---|---|---|
| **M30** | Ses ve erişilebilirlik | Menü/yarış sesleri, geçiş animasyonları, klavye navigasyonu, kontrast ve arayüz ölçekleme. |
| **M31** | Sağlamlık | Performans profillemesi (uzun kariyerlerde kayıt boyutu ve simülasyon hızı), **kayıt şeması göçü**, çökme kaydı ve raporlama. |
| **M32** | Paketleme | Windows (Velopack, otomatik güncelleme) + macOS (.app/dmg + notarization notları). Linux dağıtım hedefi değil (ADR-0009). |
| **M33** | **1.0 — tek oyunculu** | Kapalı beta, geri bildirim turu, son denge geçişi, sürüm notları. Tek oyunculu oyun burada tamamlanır. |

### Faz 6 — Çok oyunculu co-op (online) · M34–M36 — *1.0 sonrası, hedef 2.0*

FM tarzı, 4 oyuncuya kadar çevrimiçi ortak kariyer (ADR-0008). Tek oyunculu 1.0
tamamlanmadan başlanmaz. Deterministik motor sayesinde paylaşılacak durum küçük.

| | Kilometre taşı | İçerik |
|---|---|---|
| **M34** ⬛ | Ağ temeli | Host/istemci modeli, ortak dünya durumu senkronizasyonu, lobi, kimlik/oturum, bağlantı yönetimi. Belirleyici simülasyon → yalnızca kararlar ve tohum senkronize edilir, sonuç her yerde aynı çıkar. |
| **M35** ⬛ | Co-op kariyer | 4 oyuncuya kadar, her biri bir takım yönetir; sezon/tur senkronizasyonu, ortak takvim, eşzamanlı yarış günü akışı, oyuncular arası pazarlık/transfer. |
| **M36** ⬛ | Çok oyunculu cila | Yeniden bağlanma, host göçü (host düşerse oyun sürsün), izleyici modu, sohbet, senkron kayıt/yükleme. Hedef: **2.0**. |

---

## 6. "Bitti" tanımı

Bir faz, aşağıdaki kriterleri **kanıtlanabilir** şekilde karşılamadan sonraki faza
geçilmez.

**Faz 0 bitti:** `dotnet build` ve `dotnet test` Linux + Windows + macOS CI'da yeşil ·
örnek carset doğrulayıcıdan geçiyor · README yeni mimariyi doğru anlatıyor.

**Faz 1 bitti:** Carset formatındaki **her kural alanının** motorda karşılığı var
(bir test bunu denetler) · aynı tohum bit bit aynı sonucu üretiyor (altın dosya) ·
1000 sezonluk süpürme makul dağılım veriyor (en iyi takım her yıl kazanmıyor, en kötü
takım hiç podyum görmüyor değil, terk oranı %5–15 bandında) · tüm testler Linux CI'da
yeşil.

**Faz 2 bitti:** **Takım Patronu modunda başsız (headless)** olarak 10 sezonluk kariyer
sonuna kadar oynanabiliyor · ekonomi kendi kendini dengeliyor (takımlar toplu iflas
etmiyor, para birikip anlamsızlaşmıyor) · Ar-Ge yatırımı süpürme raporunda ölçülebilir
performans farkı yaratıyor · kayıt/yükleme durumu bit bit koruyor.
Ayrıca (ADR-0011) tam bir sezon **gün gün ilerleyerek** (tarihli olaylar üzerinden)
oynanabiliyor ve kayıt güncel tarihi + olay kuyruğunu birebir koruyor. (ADR-0010) bir
regülasyon ihlali doğru cezayı **araç+takıma** veriyor; sezon-içi bütçe aşımı para+puan
cezası doğuruyor; sezon-sonu kural değişikliği hazırlıksız (AI) takımın yeni-sezon araç
derecesini süpürmede ölçülebilir düşürüyor; Patron modunda oyuncu bir oylamayı etkileyebiliyor.

**Faz 3 bitti:** Arayüzden hiç konsola düşmeden tam bir sezon oynanabiliyor · headless
arayüz testleri her ekranı açıp kapatıyor · öğretici yeni bir oyuncuyu ilk yarışın
sonuna götürüyor · istatistik ekranları 10 sezonluk kariyerde doğru veri gösteriyor.

**Faz 4 bitti:** Editörle sıfırdan yazılmış bir carset oyunda sorunsuz oynanıyor ·
çok sınıflı carset doğru sonuç üretiyor · bir mod, temel carset'in üstüne katman olarak
biniyor (isim/görsel değişiyor, oyun bozulmuyor) · birleşik-mod kariyeri birden çok
seriyi tek şampiyonada koşturabiliyor.

**Faz 5 bitti (1.0):** Windows ve macOS'ta kurulup çalışıyor (her ikisinde CI testleri
yeşil) · eski kayıtlar yeni sürümde açılıyor · sürüm notları yazılı. Tek oyunculu oyun tamam.

**Faz 6 bitti (2.0):** 4 oyuncu aynı çevrimiçi kariyeri baştan sona oynayabiliyor ·
oyuncular arası durum bit bit tutarlı (aynı tohum + aynı kararlar → aynı sonuç) · bir
oyuncu düşüp yeniden bağlandığında oyun bozulmuyor.

---

## 7. Riskler ve önlemler

| Risk | Belirti | Önlem |
|---|---|---|
| **Kapsam patlaması** | Faz 1 bitmeden Faz 2 özelliklerine başlamak | Her faz sonunda tek soru: "bu hâliyle oynanabilir mi?" Faz atlanmaz. |
| **Denge tutmaması** | Aynı takım her sezon şampiyon; herkes terk ediyor | M10'daki süpürme aracı **Faz 2'ye geçmeden** çalışır durumda olmalı. Sayısal hedefler Bölüm 6'da yazılı. |
| **Avalonia öğrenme eğrisi** | M19 şişip iki katına çıkması | M19 kasten küçük tutulur; tema ve kontrol seti erken sabitlenir, sonra dokunulmaz. |
| **İçerik yükü** | 20 pisti elle yazmanın sıkıcılığı, M2'nin takılması | Üretici betikle iskelet üretilir, elle sadece dengelenir. Görseller SVG yer tutucu — sanat işi Faz 5'e ertelenir. |
| **Determinizmin sessizce bozulması** | Testler bazen geçip bazen kalması | Koruma testleri M3'te kurulur ve **hiç gevşetilmez**. Altın dosyalar M10'da sabitlenir. |
| **Tek mod odağı** | İki moda bölünmek derinliği dağıtırdı | Pilot Kariyeri rafa alındı (ADR-0004); odak Takım Patronu deneyiminde. Ortak dünya (ADR-0022) M11'de kurulur; Pilot ileride aynı dünya üstüne eklenebilir. |
| **Ağ/senkron karmaşıklığı (Faz 6)** | Oyuncular arası durum sapması (desync), host düşünce oyunun ölmesi | Faz 6 tek oyunculu 1.0 oturmadan başlamaz. Deterministik motor sayesinde tam durum değil yalnızca kararlar+tohum senkronize edilir; desync bir testle yakalanır (aynı girdi → aynı durum). |
| **Lisanslı mod dağıtımı** | Gerçek isimli içeriğin oyunla dağıtılıp telif riski doğurması | Lisanslı içerik yalnızca kullanıcı modu; ana dağıtıma asla girmez (ADR-0007). |
| **v1'in geri özlenmesi** | "Keşke silmeseydik" | v1 `a83ebcc` commit'inde duruyor; formüllere bakmak için `git show` yeterli. |

---

## 8. Çalışma düzeni

- **Kilometre taşı = bir dal + bir PR.** Dal adı: `claude/m<NN>-<kısa-ad>`.
- Her PR **yeşil CI** ile kapanır. Kırmızı CI ile birleştirme yok.
- Her kilometre taşı, kendi testleriyle birlikte gelir. Test yazılmadan "bitti" denmez.
- Mimari kararlar `docs/adr/` altına numaralı dosya olarak yazılır.
- Her **faz** sonunda `README.md` ve bu belge güncellenir (durum satırı + işaretlenen
  kutular).
- Commit mesajları ve kod yorumları İngilizce; planlama belgeleri Türkçe.

---

## 9. İlk adım: M0

Sıradaki iş **M0 — sıfırlama ve iskelet**. Somut adımlar:

**Silinecek:**
- `src/` (tamamı: `LightsToFlag.Core`, `LightsToFlag.App`)
- `tests/` (tamamı)
- `carsets/` (F1 2019 dahil — 8 MB)
- `LightsToFlag.sln`, `LightsToFlag.CI.slnf`

**Kurulacak:**
- `LightsToFlag2.sln` + Bölüm 4.1'deki yedi proje ve beş test projesi (boş iskelet)
- `Directory.Build.props`: .NET 9, `Nullable`, `TreatWarningsAsErrors`,
  `InvariantGlobalization` uyarı yorumu
- `.editorconfig`
- `.github/workflows/ci.yml`: **tam çözüm** Linux'ta derlenir ve test edilir
  (artık filtreye gerek yok — Avalonia'nın kazancı bu)
- `.github/workflows/build-matrix.yml`: Windows + macOS'ta **build + test** (ürün hedef
  platformları; ADR-0009)
- `docs/adr/0001-*.md` … `0009-*.md`: Bölüm 2'deki dokuz karar ayrı dosyalara taşınır
- `README.md`: İngilizce, sıfırdan yazılır (mevcut metin v1'i anlatıyor, geçersiz kalacak)

**Doğrulama:**
```bash
dotnet build LightsToFlag2.sln -c Release
dotnet test  LightsToFlag2.sln -c Release
```
İkisi de Linux'ta yeşil olmalı — arayüz projesi dahil.

---

*Bu belge yaşayan bir dokümandır. Her faz sonunda güncellenir.*

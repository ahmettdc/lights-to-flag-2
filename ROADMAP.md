# Lights to Flag 2 — Yol Haritası

> **Durum:** Faz 0 / M0 bekliyor · **Belge tarihi:** 2026-08-03 · **Belge dili:** Türkçe
> **Oyun arayüz dili:** İngilizce · **Kapsam:** 7 faz (0–6), 37 kilometre taşı (M0–M36),
> tek oyunculu 1.0 + çok oyunculu co-op 2.0

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
> 1. v1'in kariyer katmanı tek oyuncu rolü (pilot) varsayımı üzerine kurulu. Çift mod
>    (pilot + takım patronu) istendiği için bu katman zaten baştan yazılacaktı.
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
| **Gerekçe** | Aynı C# bilgisi, ama üç platformda çalışır. Kritik kazanç: **tüm çözüm — arayüz dahil — Linux CI'da derlenir** ve `Avalonia.Headless` ile arayüz testleri koşar. v1'de arayüz CI'da hiç test edilmiyordu. |
| **Feda edilen** | WPF'in olgun kontrol/tema ekosistemi ve hazır üçüncü parti bileşenler. Bazı kontroller elle yazılacak. |
| **Alternatifler** | WPF (Windows'a hapsolmak), Electron/TypeScript (motoru C#'tan taşımak), Godot (yönetim ekranları/tablolar için zahmetli). |
| **Tasarım kaynağı** | Arayüzün **görsel tasarımı kullanıcının Claude Design mockup'larından** gelir (HTML/CSS çıktı). Bu çıktı **doğrudan kullanılmaz**; tasarım referansı (mockup) olarak alınıp **Avalonia'ya birebir çevrilir**. Tasarım kullanıcının, uygulama bizim. Blazor Hybrid (HTML'i doğrudan kullanmak) değerlendirildi ve reddedildi — tek .NET yığını, üç platform ve arayüzün Linux CI'da headless test edilmesi kazançları korunuyor. |

### ADR-0003 — Her şey sıfır: içerik ve marka dahil

| | |
|---|---|
| **Karar** | Yeni veri formatı, elle yazılmış **kurgusal** örnek carset, yeni marka kiti (palet + logo), yeni tipografi. |
| **Gerekçe** | Kararın kendisi kullanıcıya ait. Yan fayda önemli: v1'in `carsets/F1 2019/` klasöründe **107 gerçek pilot fotoğrafı** ve gerçek takım/pilot/sponsor isimleri var; lisans durumu belirsiz. Kurgusal içerik bu riski tümden ortadan kaldırır ve oyunu dağıtılabilir kılar. |
| **Dürüst not** | Font "yazmak" gerçekçi değil. Yapılacak olan: yeni bir **OFL lisanslı font seti seçmek** (v1'in Saira Condensed / Chakra Petch / Archivo üçlüsü kullanılmayacak). Logo ve paletin kendisi sıfırdan üretilir (SVG). |
| **Sonuç** | İlk oynanabilir içerik: **"Global Prix Series"** — 10 kurgusal takım, 20 pilot, 20 pist. |

### ADR-0004 — Çift oyuncu modu, baştan

| | |
|---|---|
| **Karar** | Kariyer katmanı **iki modu** taşıyacak şekilde kurulur: *Driver Career* (pilot) ve *Team Principal* (takım patronu). Yeni kariyerde seçilir. |
| **Gerekçe** | Ortak dünya durumu (takımlar, pilotlar, sözleşmeler, finans, takvim) + moda özel karar yüzeyi. Sonradan eklemek kariyer katmanını yeniden yazmak demek. |
| **Feda edilen** | Faz 2 belirgin şekilde uzuyor (M16 ve M17 ayrı kilometre taşları). |

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

---

## 3. Ürün vizyonu

> **Lights to Flag 2**, veriye dayalı (data-driven), deterministik bir motorsporu
> kariyer simülasyonudur. Oyuncu ya direksiyonun arkasına geçer ya da pit duvarına —
> ve her ikisinde de sonucu belirleyen şey refleks değil, karardır.

**Pilot Kariyeri (Driver Career)** — Alt sıradaki bir takımda koltuk bulursunuz.
Antrenman programını seçer, sıralamada riski ayarlar, yarışta lastik ve pit
kararlarını verirsiniz. İyi giderseniz sözleşme teklifleri gelir; takım arkadaşınızla
rekabet, itibarınız ve moraliniz hangi kapıların açılacağını belirler.

**Takım Patronu (Team Principal)** — Bütçe, sponsorlar, personel, Ar-Ge programları ve
pilot transferleri sizin. Yarış günü iki aracın stratejisini pit duvarından
yönetirsiniz. Yönetim kurulunun hedefleri var ve sabrı sonsuz değil.

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
│  ├─ LTF.Career/          net9.0   Sezon, kariyer, ekonomi, sözleşme, Ar-Ge, iki mod.
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
| **M0** | Sıfırlama + iskelet | Eski `src/`, `tests/`, `carsets/`, `.sln`, `.slnf` silinir. Yeni çözüm iskeleti, .NET 9, `Directory.Build.props`, `.editorconfig`, üç platformlu CI (Linux tam çözüm + Windows + macOS derleme), `docs/adr/` klasörü, **`README.md` İngilizce yeniden yazılır** (v1'i anlatan mevcut metin geçersiz kalacak). |
| **M1** ⬛ | Domain modeli | Pilot, takım, pist, kural seti, katsayılar — **artı v1'de olmayanlar:** personel (tasarımcı/mühendis/mekanik), sözleşme ve maddeleri, finansal kalemler, araç bileşenleri (motor/şanzıman/fren) ve kullanım kotaları, sponsor, tesis, itibar/moral. Hepsi `sealed record`, değişmez (immutable). |
| **M2** ⬛ | İçerik formatı + ilk carset | JSON şeması + yükleyici + **doğrulayıcı** (hatalı carset'i anlamlı mesajla reddeder). **"Global Prix Series"**: 10 kurgusal takım, 20 pilot, 20 pist elle yazılır (üretici betikle iskelet + elle dengeleme). Görseller SVG yer tutucu. **Format baştan mod katmanlamasına (overlay) hazır tasarlanır** — bir mod, temel carset'in üstüne isim/görsel/ayar bindirebilsin (bkz. ADR-0007; özellik Faz 4'te). |

### Faz 1 — Yarış derinliği · M3–M10 ← *1. öncelik*

| | Kilometre taşı | İçerik |
|---|---|---|
| **M3** ⬛ | RNG + sektör bazlı tur zamanı | Tohumlanabilir, çatallanabilir (`Fork`) RNG. Tur zamanı = pist temel süresi × (pilot yeteneği ⊗ araç performansı ⊗ pist karakteri) + gürültü, **sektör sektör**. v1 tur bazlıydı; sektör bazlı olması canlı zamanlamayı, delta'yı ve slipstream'i mümkün kılan temeldir. |
| **M4** ⬛ | Lastik · yakıt · hava | Lastik: bileşim, aşınma eğrisi, sıcaklık penceresi, graining, **performans uçurumu (cliff)**. Yakıt: yük cezası, tüketim. Hava: dinamik değişim, pist ıslaklığı, slick↔ara↔yağmur **geçiş noktaları**, kuruyan pist. |
| **M5** ⬛ | Güvenilirlik + olaylar | Bileşen bazlı arıza (motor/şanzıman/fren/hidrolik), pilot hatası, çarpışma, spin. **Güvenlik aracı, VSC ve kırmızı bayrak** — tam prosedürleriyle (pit yolu kapanması, tur turlama, yeniden start). |
| **M6** ⬛ | Trafik ve geçiş | Kirli hava (dirty air), slipstream/DRS bölgeleri, blokaj, turlanan araçlar, gerçek geçiş mücadelesi (bir tur boyunca süren düellolar). |
| **M7** ⬛ | Pit + strateji + cezalar | Pit stop süre dağılımı, güvensiz bırakma, yakıt ikmali (kural açıksa). Her rakip için **strateji planı** (undercut/overcut yapabilen). Ceza sistemi: stop-go, drive-through, süre cezası, grid cezası, hatalı start, pist limitleri, sarı bayrak ihlali. |
| **M8** | Seans biçimleri | Sıralama: eleme usulü **Q1/Q2/Q3**, tek turlu, tek seans, sprint shootout. Antrenman: oyuncu programı seçer (kurulum çalışması / lastik denemesi / yarış simülasyonu) ve seçim yarışa yansır. |
| **M9** ⬛ | Yarış biçimleri ve kural sadakati | Sprint hafta sonu, ters grid, süreli yarış, **çok sınıflı yarış**, başarı balastı, ikincil puan tablosu, pol/en hızlı tur/lider tur puanları, motor-şanzıman **tahsis kotaları** ve aşım cezaları, playoff ("chase") formatı, paylaşımlı pit boksu. |
| **M10** ⬛ | Determinizm + denge aracı | Altın dosya testleri (aynı tohum → aynı sonuç, bit bit). **`LTF.Tools sweep`**: 1000 sezon başsız simüle eder, şampiyonluk dağılımı / galibiyet yayılımı / terk oranı / güvenlik aracı sıklığı / ortalama pit sayısı raporlar. Katsayılar bu rapora bakarak ayarlanır. |

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

| | Kilometre taşı | İçerik |
|---|---|---|
| **M11** ⬛ | Dünya durumu + sezon motoru | Kalıcı dünya: takımlar, pilotlar, personel, sözleşmeler, takvim. Sezon simülasyonu, puan durumları, **kalıcı tarih ve rekorlar** (v1 yalnızca şampiyon adını saklıyordu: her yarış sonucu, her rekor, her pilot istatistiği tutulur). |
| **M12** ⬛ | Sözleşmeler ve insan ilişkileri | Pazarlık mekaniği, sözleşme maddeleri (performans primi, çıkış maddesi, 1. pilot statüsü), itibar, moral, takım arkadaşı rekabeti, takım-pilot ilişkisi. |
| **M13** ⬛ | Ekonomi | Sponsorlar (hedefli anlaşmalar, bonuslar), ödül parası, TV geliri, maaşlar, parça/seyahat gideri, kaza maliyeti, isteğe bağlı **bütçe tavanı**. |
| **M14** ⬛ | Ar-Ge ve personel | Gelişim programları (aerodinamik / motor / şasi / güvenilirlik), kaynak dağıtımı, tesis yükseltmeleri (rüzgâr tüneli, simülatör), personel işe alımı ve yetenekleri. Sezon içi gelişimin **simülasyonda gerçekten hissedilmesi** (Faz 1'e bağlanır). |
| **M15** | Test ve bileşen yönetimi | Test günleri, sezon içi güncellemelerin devreye alınması, bileşen tahsis takibi ve grid cezaları. |
| **M16** ⬛ | **Pilot Kariyeri modu** | Uçtan uca: koltuk arayışı, sezon hedefleri, hafta sonu kararları, yarış içi strateji tercihleri, sezon değerlendirmesi, kariyer ilerleyişi. |
| **M17** ⬛ | **Takım Patronu modu** | Uçtan uca: pit duvarından iki araç yönetimi, transfer kararları, bütçe planlaması, yönetim kurulu hedefleri ve kovulma riski. |
| **M18** ⬛ | Sezon devri | Yaşlanma, gelişim/gerileme eğrileri, emeklilik, **yeni nesil pilot üretimi (regen)**, transfer piyasası (yapay zekâ takımları da hamle yapar), sezonlar arası kural değişiklikleri. |

### Faz 3 — Arayüz · M19–M25 — *Avalonia, oyun dili İngilizce*

| | Kilometre taşı | İçerik |
|---|---|---|
| **M19** ⬛ | Kabuk + yeni marka kiti | Navigasyon, tema sistemi, **yeni palet**, **SVG logo**, yeni OFL font seçimi, yerelleştirme altyapısı (ileride Türkçe dil paketi eklenebilsin diye). **Görsel tasarım kullanıcının Claude Design mockup'larından gelir**; HTML/CSS çıktı doğrudan kullanılmaz, tasarım referans alınıp Avalonia'ya birebir çevrilir (bkz. ADR-0002). Tüm Faz 3 ekranları (M20–M25) bu mockup'ları takip eder. |
| **M20** | Menü + kariyer başlatma | Ana menü, yeni kariyer akışı (**mod seçimi**: Driver / Team Principal), carset seçimi, kayıt-yükleme ekranı. |
| **M21** ⬛ | Kariyer merkezi | Pano, takvim, puan durumu, takım/pilot listeleri, **gelen kutusu / haber akışı** (sözleşme teklifleri, yönetim kurulu mesajları, basın). |
| **M22** ⬛ | Yönetim ekranları | Finans, Ar-Ge, personel, tesisler, sözleşmeler (ağırlıklı olarak Patron modu; Pilot modunda kısıtlı görünüm). |
| **M23** ⬛ | Yarış hafta sonu | Canlı zamanlama kulesi, **pist haritası**, sektör renkleri ve delta'lar, strateji paneli (pit çağrısı, bileşim seçimi), telsiz mesajları, hız kontrolü / atlama / tekrar. |
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
| **M32** | Paketleme | Windows (Velopack, otomatik güncelleme), Linux (AppImage), macOS (.app + notarization notları). |
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

**Faz 0 bitti:** `dotnet build` ve `dotnet test` üç platformda da yeşil · örnek carset
doğrulayıcıdan geçiyor · README yeni mimariyi doğru anlatıyor.

**Faz 1 bitti:** Carset formatındaki **her kural alanının** motorda karşılığı var
(bir test bunu denetler) · aynı tohum bit bit aynı sonucu üretiyor (altın dosya) ·
1000 sezonluk süpürme makul dağılım veriyor (en iyi takım her yıl kazanmıyor, en kötü
takım hiç podyum görmüyor değil, terk oranı %5–15 bandında) · tüm testler Linux CI'da
yeşil.

**Faz 2 bitti:** Her iki modda da **başsız (headless)** olarak 10 sezonluk kariyer
sonuna kadar oynanabiliyor · ekonomi kendi kendini dengeliyor (takımlar toplu iflas
etmiyor, para birikip anlamsızlaşmıyor) · Ar-Ge yatırımı süpürme raporunda ölçülebilir
performans farkı yaratıyor · kayıt/yükleme durumu bit bit koruyor.

**Faz 3 bitti:** Arayüzden hiç konsola düşmeden tam bir sezon oynanabiliyor · headless
arayüz testleri her ekranı açıp kapatıyor · öğretici yeni bir oyuncuyu ilk yarışın
sonuna götürüyor · istatistik ekranları 10 sezonluk kariyerde doğru veri gösteriyor.

**Faz 4 bitti:** Editörle sıfırdan yazılmış bir carset oyunda sorunsuz oynanıyor ·
çok sınıflı carset doğru sonuç üretiyor · bir mod, temel carset'in üstüne katman olarak
biniyor (isim/görsel değişiyor, oyun bozulmuyor) · birleşik-mod kariyeri birden çok
seriyi tek şampiyonada koşturabiliyor.

**Faz 5 bitti (1.0):** Üç platformda kurulup çalışıyor · eski kayıtlar yeni sürümde
açılıyor · sürüm notları yazılı. Tek oyunculu oyun tamam.

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
| **Çift modun ikiye katlaması** | M16/M17'nin tahmin edilenden uzun sürmesi | Ortak dünya durumu M11'de doğru kurulursa mod farkı yalnızca karar yüzeyidir. M11 aceleye getirilmez. |
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
- `.github/workflows/build-matrix.yml`: Windows + macOS derleme doğrulaması
- `docs/adr/0001-*.md` … `0008-*.md`: Bölüm 2'deki sekiz karar ayrı dosyalara taşınır
- `README.md`: İngilizce, sıfırdan yazılır (mevcut metin v1'i anlatıyor, geçersiz kalacak)

**Doğrulama:**
```bash
dotnet build LightsToFlag2.sln -c Release
dotnet test  LightsToFlag2.sln -c Release
```
İkisi de Linux'ta yeşil olmalı — arayüz projesi dahil.

---

*Bu belge yaşayan bir dokümandır. Her faz sonunda güncellenir.*

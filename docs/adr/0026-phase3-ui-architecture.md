# ADR-0026 — Faz 3 UI mimarisi (Avalonia MVVM kabuk)

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-06

## Bağlam
Faz 3 (M19–M25) arayüzü Avalonia'da yazılır (ADR-0002). M19 kalıcı **kabuğu** kurar
(üst bar / sidebar / durum çubuğu + içerik bölgesi + bildirim merkezi + tema/marka +
yerelleştirme altyapısı). Bu ADR, kabuğun ve onu izleyen ekranların dayanacağı UI
mimari kararlarını kaydeder.

## Karar

**1. MVVM tabanı = CommunityToolkit.Mvvm.** Kaynak-üreteçli `[ObservableProperty]` /
`[RelayCommand]` ve `ObservableObject` tabanı; tek `ViewModelBase` üstüne oturur.
Yansıma yok, derleme-zamanı üretim → compiled-binding (`x:DataType`) ve headless test ile
uyumlu. (Alternatif: elle yazılmış minimal `INotifyPropertyChanged` tabanı — boilerplate
azaltmak için toolkit seçildi.)

**2. Tema/kaynak mimarisi.** Marka teması Fluent'in üstüne biner: `App.axaml`
kaynakları **Colors → Brushes → Typography → ControlTheme** sırasıyla birleştirir
(fırçalar renk anahtarlarını okur), `Application.Styles` ise `FluentTheme`'den **sonra**
`StyleInclude`'ları alır. Dark-only olduğu için marka fırçaları `StaticResource`. Fontlar
(Saira Condensed / Chakra Petch / Archivo, OFL) ve marka logo/ikon kiti `avares://` ile
gömülür. Durum kuralı (`≥80` yeşil / `≥70` amber / else kırmızı) tek bir
`StatusColorConverter`'da (`const` eşikler) — saf birim test edilir.

**3. Yerelleştirme = kod-içi pack (`ILocalizer`), .resx değil.** Derleme-kontrollü
`StringKeys` sabitleri + `EnglishStrings` pack; eksik anahtar görünür `!key!` sentinel'i
döndürür; bir kapsama testi her anahtarın çözüldüğünü kanıtlar. Kültür yalnızca
**görüntü** için okunur (`CurrentUICulture`/`CurrentCulture`); **`InvariantGlobalization`
asla açılmaz** (v1 çökme tuzağı — ROADMAP §4.2). İleride bir Türkçe pack aynı anahtar
kümesiyle View'lara dokunmadan eklenir; `{loc:Tr}` markup uzantısı da mevcuttur.

**4. Headless test = gerçek App + Skia offscreen.** `[AvaloniaFact]` testleri gerçek
`App`'i (tema/font/kaynak sözlükleriyle) `UseSkia().UseHeadless(UseHeadlessDrawing=false)`
ile kurar — headless stub font yöneticisi gömülü fontlar için glyph üretemediğinden Skia
şart. Testler görsel ağacı ve kaynak-çözümü doğrular (piksel değil), üç OS'te de koşar.

**5. Katmanlama gardiyanı motor tarafına genişletildi.** Yeni `LTF.Architecture.Tests`,
motor assembly'lerinin (Content/Simulation/Career/Persistence) `Avalonia*` veya `LTF.App`'e
referans **vermediğini** doğrular. `LTF.App` alt katmanlara referans verebilir; motor UI'dan
habersiz kalır (ADR-0002 / ROADMAP §4.2).

**6. Görsel tasarım kullanıcının mockup'ından, uygulama Avalonia'da.** `design/mockups/
ui.dc.html` otoriter görsel referanstır; HTML/CSS doğrudan kullanılmaz, Avalonia'ya birebir
çevrilir (ADR-0002 teyidi). Kabuk Team-Principal-only (ADR-0004).

## Sonuçlar
- Kabuk oyun-durumu tutmaz: session read-only, Continue M19'da inert (per-event runner M21);
  hiçbir duvar-saati/tohumsuz-RNG oyun durumuna akmaz. Motor projeleri değişmediği için tüm
  golden/byte-özdeş/sweep/round-trip testleri M19 boyunca yeşil kalır.
- Feda edilen: CommunityToolkit bir paket bağımlılığı ekler (yalnız App; motor almaz).
- Ertelenen: TR dil paketi (altyapı hazır); Light tema (Dark-only); domain ekran gövdeleri
  (M20–M25); per-event Continue runner (M21).

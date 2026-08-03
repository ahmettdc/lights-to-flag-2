# ADR-0002 — .NET 9 + Avalonia

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-03

## Karar
Arayüz **Avalonia UI 11.x** ile, hedef çatı **.NET 9** olarak yazılır.

## Gerekçe
Aynı C# bilgisi, birden çok platform tek yığınla. **Ürün hedefi Windows + macOS**
(bkz. ADR-0009); Linux ayrıca bir *CI/geliştirme tezgâhı* olarak kullanılır. Kritik
kazanç: tüm çözüm — arayüz dahil — Linux CI'da derlenir ve headless UI testleri
koşabilir. v1'de WPF arayüzü CI'da hiç test edilmiyordu (bu yüzden `LightsToFlag.CI.slnf`
filtresi vardı).

## Tasarım kaynağı
Arayüzün görsel tasarımı kullanıcının **Claude Design** mockup'larından (HTML/CSS)
gelir. Bu çıktı doğrudan kullanılmaz; tasarım referansı alınıp Avalonia'ya birebir
çevrilir (tasarım kullanıcının, uygulama bizim).

## Sonuçlar
- Feda edilen: WPF'in olgun kontrol/tema ekosistemi; bazı kontroller elle yazılır.
- Reddedilen alternatifler: WPF (Windows'a hapsolmak), Electron/TypeScript (motoru
  taşımak), Godot (yönetim tabloları için zahmetli), Blazor Hybrid (WebView, farklı
  test hikâyesi — tek .NET/Avalonia yığını tercih edildi).

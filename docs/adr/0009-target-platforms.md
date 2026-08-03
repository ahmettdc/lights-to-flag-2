# ADR-0009 — Hedef platformlar: Windows + macOS

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-03

## Karar
Ürün **Windows ve macOS**'ta yayınlanır. **Linux bir dağıtım hedefi değildir**; yalnızca
CI/geliştirme tezgâhı olarak kullanılır.

## Bağlam
Kullanıcı: "Linux yazılımda olmasa da olur ama Windows ve macOS'ta çalışsın."

## Gerekçe
Avalonia (ADR-0002) zaten Windows + macOS'u tek .NET yığınıyla verir; bu karar mimariyi
değiştirmez. Linux'ta da derlenebilmesi **ücretsiz bir test kazancıdır** — hızlı ve ucuz
CI koşumu — ama oyunun Linux'ta yayınlanacağı anlamına gelmez.

## Sonuçlar
- **CI:** Windows ve macOS artık ürünün çalışması gereken platformlar olduğu için, CI'da
  yalnızca derlenmezler, **test de edilirler** (`build-matrix.yml` build+test). Linux
  (`ci.yml`) hızlı tezgâh olarak kalır.
- **Paketleme (M32):** yalnızca Windows (Velopack) + macOS (.app/dmg). Linux AppImage yok.
- **"Bitti" tanımı (Faz 5):** "Windows ve macOS'ta kurulup çalışıyor; her ikisinde CI
  testleri yeşil."
- İleride Linux bir hedef olmak istenirse: Avalonia sayesinde ek maliyet düşük — yalnızca
  bir paketleme hattı eklemek gerekir.

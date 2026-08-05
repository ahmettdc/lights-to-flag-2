# ADR-0001 — Sıfırdan inşa

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-03

## Karar
Çalışan v1 kodu (Core motoru + WPF arayüzü, ~5.000 satır, 51 test) tamamen silinir
ve mimari baştan kurulur.

## Bağlam
v1'de M0–M10 tamamlanmıştı. Kullanıcıya "mevcudun üstüne devam mı, sıfırdan mı?"
açıkça soruldu; **sıfırdan** kararı verildi.

## Gerekçe
Zaten yeniden yazılacak üç katman vardı: (1) kariyer katmanı v1'in pilot rolü
varsayımına bağlıydı, LTF2 ise Takım Patronu derinliğini istiyor; (2) WPF arayüzü Linux CI'da derlenmiyordu;
(3) simülasyon tur bazlıydı, istenen derinlik sektör bazlı model gerektiriyor.
Sıfırdan karar, bu üç yeniden yazımı resmileştiriyor.

## Sonuçlar
- ~5.000 satır kod ve 51 test feda edilir; yeniden kazanımı Faz 0–1 sürer.
- v1 git geçmişinde `a83ebcc` commit'inde korunur; formüllere `git show` ile bakılabilir.
- Reddedilen alternatif: v1 üstüne eklemek (daha hızlı ama borç taşırdı).

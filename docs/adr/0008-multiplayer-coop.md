# ADR-0008 — Çok oyunculu co-op (online), 1.0 sonrası

- **Durum:** Kabul edildi
- **Tarih:** 2026-08-03

## Karar
Football Manager tarzı, **4 oyuncuya kadar çevrimiçi ortak kariyer**: her oyuncu bir
takım yönetir, sezon birlikte ilerler.

## Yerleşim
Yol haritasının **en sonu, ayrı faz (Faz 6)**. Tek oyunculu oyun (Faz 0–5)
tamamlanıp **1.0** çıkmadan başlanmaz. Hedef: **2.0**.

## Gerekçe
Ağ ve senkronizasyon, tek oyunculu oyunun tüm sistemleri oturduktan sonra en düşük
riskle eklenir. Deterministik motor (ADR-0002) burada büyük avantaj: paylaşılacak
durum küçük — yalnızca kararlar ve tohum senkronize edilir, sonuç her yerde aynı çıkar.

## Sonuç
Çok oyunculu 1.0'da yoktur.

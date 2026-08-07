# ADR-0029 — Banka & kredi sistemi (dinamik dünyayla entegre · kredi puanı · imzada-donan faiz · kademeli icra)

- **Durum:** Kabul edildi — **Faz-A motoru indi** (arayüz M22'ye ertelendi)
- **Tarih:** 2026-08-07

## Bağlam
Kullanıcı isteği: takım **faizle kredi** çekebilsin, **ödemezse icra** olsun — alacaklılar peşine
düşsün. İkinci istek bunu LTF2'nin var olan **dinamik dünyasına** bağladı ve üç soru sordu:
**(1)** faiz oranları dünyayla birlikte değişecek mi, **(2)** kredi puanı nasıl planlanacak,
**(3)** kredi miktarı (limit) dünyayla değişecek mi.

Zemin (keşiften): kredi/borç/puan altsistemi **yoktu** — yeşil alan. Tek kanca `Finances.Balance`
(`long`, belge: *"eksiye düşebilir — sonuçlarına ekonomi katmanı karar verir"*). Para yalnız
`team with { Finances = … }` ile değişir; sezon yazarı `EconomyLedger.SettleSeason` (LTF.Career) —
`Balance + gelir − gider − ceza`, **clamp yok, iflas yok**. Eksi bakiye bugün yalnız board
`FinancialPressure`'ını yükseltir (`BoardReview`). `EconomyRules` `carset.Rules.Economy`'de asılı;
`EconomySweep`/`FlagshipEconomyTests` sınırlı bakiye iddia eder (iflas eden takım = 0) — kredi katmanı
**inert** iken bunları bozmamalı.

**Kullanıcıyla kilitlenen iki karar:**
- **Faiz modeli:** *teklif edilen* oran **dinamik** (her sezon kredi durumundan yeniden hesaplanır),
  ama **imza atılınca o kredinin oranı ömrü boyunca donar**. Sonraki krediler o anki teklifi alır.
  ("İmzadan önce değişken, imzadan sonra sabit.")
- **İcra terminal sertliği:** **ağır ama hayatta-kalınır** — idari puan silme + zorunlu varlık
  tasfiyesi, ama **kovulma / oyun-sonu yok** (kariyer devam eder).

## Karar

**Mimari köşe taşı: kredi puanı, teklif faizi ve kredi limiti asla persist edilmez** — persist
edilen dünya durumunun saf, talep-anında bir fonksiyonudur. Yalnız `Loan`'lar saklanır. Böylece üçü
de sıfır kayıt-format maliyetiyle dünyayla daima eşzamanlı.

### 1. Kredi puanı (kredi puanı) — deterministik, evrilen dünyadan türetilir
`CreditProfile.For(team, standings?, board?, bank, economy)` → 0–100 puan, beş sinyalin ağırlıklı
harmanı (tam-sayı aritmetiği, RNG yok):
- **Pist başarısı** — `ConstructorStanding.Position` (iyi konum ⇒ gelecek ödül ⇒ güvenli). Ağırlık 25.
- **Ödeme gücü** — `Finances.Balance`'ın bir yıllık gelire oranı. Ağırlık 30.
- **Varlık gidişatı** — `Car.Overall` (güçlü araç ⇒ gelecek sonuç ⇒ gelir). Ağırlık 15.
- **Yönetişim istikrarı** — `board.Metrics.BoardConfidence` + `FiringRisk` (kovulmaya yakın patron
  kredi riskidir). Ağırlık 15. **Board yalnız oyuncu takımında var (M17 Core);** yoksa bu sinyal
  düşer ve kalan ağırlıklar yeniden normalize edilir.
- **Geri ödeme geçmişi** — kredilerdeki kaçırılan taksitler (en güçlü gerçek-banka sinyali). Ağırlık 15.

Her girdi her sezon yeniden okunduğundan **puan dünyayla paralel hareket eder** — şampiyon, nakit-zengin,
board-destekli takım tırmanır; çöken düşer. Puan → 5-bantlı `CreditTier` (Poor→Excellent) etiketi (UI).

### 2. Faiz (faiz) — dinamik teklif, kredi-başına donmuş (kullanıcının modeli)
- **Teklif oranı** (şimdi kote edilen) = `baseRate + riskPremium(puan)`. `baseRate` bir carset
  banka-config sabiti; `riskPremium` puanla ters ölçekler (yüksek puan ⇒ baz oran; düşük puan ⇒
  tefeci primi). Talep-anında türetilir ⇒ **alacağın teklif dünyayla değişir.**
- **İmzada** kredi bu teklifi kendi `Loan.AnnualRatePercent`'ine yakalar ve **ömrü boyunca dondurur**;
  faiz her sezon kredinin *kendi donmuş oranından* işler. Kredi kötüyken çekilen sonraki kredi daha
  yüksek donmuş oran taşır.

### 3. Kredi limiti (kredi miktarı) — dinamik, evrilen dünyadan türetilir
`limit = borçlanma-kapasitesi × puanÇarpanı`. Kapasite = takımın hizmet edilebilir yıllık gelirinin
(`PrizeMoney + SponsorIncome + Economy.TvIncome`) `MaxLoanToRevenuePercent` katı; `puanÇarpanı` puanla
%50 (puan 0) → %100 (puan 100) ölçekler. **Boş alan (headroom)** = `limit − TotalDebt`. Talep-anında
türetilir ⇒ **çekebileceğin miktar dünyayla büyür/küçülür.**

### 4. Kredi defteri — `BankLedger`
- `Borrow(team, profile, amount, term, id)` → `BorrowResult`: miktar pozitif + term pozitif +
  miktar ≤ headroom ise onaylar; bakiyeyi kredilendirir ve **teklif oranını dondurur**. Aksi halde
  `LoanDecision` (InvalidAmount / InvalidTerm / ExceedsHeadroom) ile reddeder, takımı değiştirmez.
- `SettleSeason(carset)` → `BankSettlement`: her kredi için donmuş oranından faiz işletir, düz-çizgi
  taksiti (faiz + anapara payı; son sezon kalanı ister) bakiyeden düşer, anaparayı azaltır; bakiye
  taksiti karşılamıyorsa **kaçırılan ödeme** kaydeder — ödenmeyen faiz + gecikme cezası açık bakiyeye
  **kapitalize olur** (borç büyür). Tamamen ödenen kredi defterden düşer. Kaçırmalar `MissedPayment`
  listesi olarak yüzeye çıkar (icra kancası).

### 5. İcra (icra) — kademeli alacaklı takibi, "ağır ceza · devam" — `BankEnforcement`
`Enforce(carset, missed)` → `EnforcementOutcome`. Her kredinin **kümülatif** kaçırma sayısına göre
deterministik merdiven (`EnforcementStep`):
1. **Warning** (ilk kaçırma) → board confidence düşer + financial pressure yükselir + eylem-gerektiren
   gelen-kutusu uyarısı ("alacaklılar peşinde"; **Rev 15** → Continue'yu durdurur — M21e halt'ı).
2. **AssetSeizure** (`AssetSeizureAfterMisses`, vars. 2) → **zorunlu varlık satışı**: en yüksek
   maaşlı personel serbest bırakılır, geliri (≈ yıllık maaş) en büyük krediyi **öder** + board darbesi.
3. **Insolvency** (`InsolvencyAfterMisses`, vars. 4; terminal) → **bir kereye mahsus idari puan silme**
   (mevcut `ConstructorPenalties.Apply`'a beslenen `CostCapPenalty`) + ağır tasfiye + azami baskı.

**Kovulma yok:** enforcement `FiringRisk`'e asla dokunmaz — puan çökse de kariyer devam eder. Terminal
puan cezası **eşiği geçtiğin sezon bir kez** işler (her terminal sezon değil), böylece sportif hasar
tek ağır darbe kalır; tasfiye + baskı borç sürdükçe her sezon işlemeye devam eder.

### 6. Determinizm / golden / byte-özdeşlik (sabit kısıtlar)
- **Kredisiz inert:** `bank` bloğu yok + kredi yok ⇒ `BankLedger`/`BankEnforcement` para kımıldatmaz ⇒
  `EconomyLedger`/`EconomySweep`/`FlagshipEconomyTests` sınırları tutar. **AI takımlar asla borçlanmaz.**
- `RaceSimulator`'a dokunulmaz ⇒ golden digest değişmez. Puan/oran/limit persist edilmez ⇒ kayıt
  formatı değişmez; `Loans` varsayılan-boş ⇒ mevcut carsetler byte-özdeş; krediler **tüm-`Finances`
  yakalama** yoluyla `CareerState`'te otomatik round-trip (M13f/M18h/M21f kalıbı) — `CareerState.cs`
  değişmez. Tüm aritmetik + eşik, **RNG yok**.
- `Finances` yapısal eşitlik kazandı (`Loans.SequenceEqual`) — derleyici-üretimi record eşitliği listeyi
  referansla karşılaştırırdı; iki boş-kredili finans yanlışlıkla farklı görünürdü. Round-trip
  sadakati için değer semantiği gerekir.

### 7. Katman haritası + fazlama
- **Domain (`LTF.Domain`):** `Loan` record; `Finances.Loans` (+ `TotalDebt`); `RulesSet.Bank`
  (`BankRules`, `IsActive` = `MaxLoanToRevenuePercent > 0`). Varsayılan boş/kapalı ⇒ inert.
- **İçerik (`LTF.Content`):** opsiyonel `rules.bank` bloğu (`BankJson` DTO + `MapBank` `?? d.X`
  kalıbı; validator negatif-değer reddi). Blok yoksa varsayılan inert ⇒ byte-özdeş.
- **Career (`LTF.Career`):** `CreditProfile` (saf: puan/oran/limit) + `BankLedger` (Borrow/SettleSeason)
  + `BankEnforcement` (icra) + `BankSweep` (çok-sezon deterministik süpürme; `ltf sweep` banka bloğu).
- **Persistence:** değişiklik yok — `Loan` STJ-dostu record, tüm-`Finances` yakalamasıyla round-trip.
- **Arayüz — Faz B (M22 Finance):** kredi çek (headroom içinde miktar/term, *o anki* teklif oranı +
  taksit önizleme) + açık-borç tablosu + geri-ödeme planı + kredi puanı/tier + icra uyarıları;
  M21 bildirim merkezine besler.

## Sonuç
Faz-A motoru (domain + içerik + `CreditProfile`/`BankLedger`/`BankEnforcement`/`BankSweep` + persistence
round-trip + `ltf sweep` banka bloğu) indi; tüm testler yeşil, golden + byte-özdeşlik korunur. Kredi
çekme arayüzü + finans ekranı **M22**'ye ertelendi.

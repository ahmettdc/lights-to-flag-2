# ADR-0016 — R&D geliştirme ağacı (tech tree)

- **Durum:** Kabul edildi (Rev 30: hibrit yapı — departman + düğüm boyutları)
- **Tarih:** 2026-08-04

## Karar
R&D bir **geliştirme ağacı (tech tree)**: dallanan düğümler; bir düğümü açmak araç
derecelerini (aero/motor/şasi/güvenilirlik) yükseltir ve ileri düğümlerin kilidini açar.
**Her sezon kullanılır** ve ağaç sezonlar arası kalıcı, derinleşerek ilerler. Dallar **araç
konseptini/yönünü** belirler (ör. yüksek downforce ↔ düşük sürtünme); **fırsat maliyeti** var
— her şey alınamaz, takımlar farklılaşır, tekrar oynanabilirlik artar.

## Kaynak & determinizm
Düğüm ilerlemesi **bütçe** (M13) + **tesis** (rüzgâr tüneli/simülatör, M14) + **personel
yeteneği** (M14) ile hızlanır; takvime göre gün gün işler (ADR-0011). Deterministik (tohumlu;
§4.2 / ADR-0002).

## Regülasyon sinerjisi (ADR-0010)
Sezon sonu kural değişikliği ağacın bazı dallarını **budayabilir/kilitleyebilir**; yeni
kurallara uygun dala yatırım yapmış takımlar avantajlı başlar, hazırlıksızlar geriye düşer
(Readiness ↔ ağaç uyumu). "Regülasyon hazırlığı" ağaçta ayrı bir dal olabilir.

## Model (özet; M14)
`TechNode` (id, kategori, maliyet, süre, önkoşullar, etki), `TechTree` (düğüm grafiği +
açılmış/ilerleyen durum, kariyer dünya durumunda kalıcı), `ResearchProgram` (aktif yatırımlar).
Etki `Car` derecelerine ve bir **konsept-yönü etiketine** işler.

## Hibrit yapı (Rev 30 — F1M ağaç yapısı + FM yoğunluğu)
Kullanıcının referans görseli (F1-Manager departman ağacı) doğrultusunda ağaç **yapısı**
benimsenir; **görsel dil FM-yoğun** kalır (`design/ui`: "F1 Manager değil Football Manager
gibi"). Somut kararlar:
- **Departman yapısı:** ağaç departmanlara bölünür (Aerodinamik, Şasi, Güç Ünitesi,
  Dayanıklılık…); her departman bir alt-ağaç (merkez + dallar). ADR-0024'ün 12 ekseni bu
  departmanlara/kategorilere dağılır — kategoriler görselin "Front/Rear DF + DRS"si **değil**,
  bizim hız-rejimi eksenlerimiz (düşük/orta/yüksek aero, floor, drag_efficiency…); **DRS ekseni
  yok** (2026/ADR-0018 aktif-aero ile tutarlı).
- **Düğüm boyutları:** `TechNode.Size` = **Minor / Major / Ultimate** (artan maliyet + etki +
  önkoşul derinliği); görselin düğüm-boyut anahtarıyla eşleşir.
- **Ekonomi korunur:** düğüm maliyeti **$ bütçe (M13) + CFD/rüzgâr-tüneli kotası + tesis slotu**
  (ADR-0020) ile ödenir; ayrı bir **"Resource Points" para birimi eklenmez** (mevcut ekonomi +
  committed mockup ile tutarlı).
- **Departman verimi / kalite kontrol:** görselin "Department Efficiency / Quality Control"
  dalları = ADR-0020 tesis modifiyeleri (verim çarpanı, kırılma/kusur riski); ağaçta departman
  meta-dalı olarak yüzeye çıkar, ayrı sistem değil.
- **UI (M22):** R&D ekranı, mevcut FM-yoğun proje-listesi + tesis + pist-doğrulama paneline
  **ek olarak** departman-bazlı **gezinilebilir tech-tree / bağımlılık görünümü** sunar —
  gösterişli radyal hero değil, yoğun bir bağımlılık grafiği (düğüm seç → yan panelde etki /
  maliyet / önkoşul / doğrulama). "FM-değil-F1M" direktifiyle uzlaşır.

## Milestone dağılımı
M14 (ana), M13 (finansman), M15 (sezon-içi güncellemeler ağaçtan), M18 (sezon devrinde taşıma +
regülasyon budaması), M17 (yatırım/konsept kararları), M22 (Ar-Ge ağacı arayüzü), ADR-0015
(pilot gelişim ağacıyla simetri).

## Durum (M14 — uygulandı)
Tech tree domain'de (`TechTree`/`Department`/`TechNode`; `CarAxis` → `CarAxisMap` ile beş canlı
dereceye; `NodeSize` Minor/Major/Ultimate), carset JSON'dan yüklenir + doğrulanır, kayıtta korunur
(`CareerState.Research`). `ResearchLedger.DevelopSeason` her sezon düğümleri ilerletir; onaylanan
düğüm aracın derecesini kalıcı yükseltir (fırsat maliyeti: bütçe + prereq). Flagship 4 departman +
16 düğümlü ağaç taşır; `ResearchSweep` çok sezonda aracın **ölçülebilir/sınırlı/deterministik**
geliştiğini gösterir. **Ertelendi:** sezon-içi güncelleme (M15), regülasyon budaması + sezon devri
(M18), yatırım/konsept kararları + arayüz (M17/M22).

## Sonuç
Ar-Ge boş bir sayı oyunu değil; her sezon oynanan, aracın kimliğini belirleyen bir ağaç olur.

# ADR-0016 — R&D geliştirme ağacı (tech tree)

- **Durum:** Kabul edildi
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

## Milestone dağılımı
M14 (ana), M13 (finansman), M15 (sezon-içi güncellemeler ağaçtan), M18 (sezon devrinde taşıma +
regülasyon budaması), M17 (yatırım/konsept kararları), M22 (Ar-Ge ağacı arayüzü), ADR-0015
(pilot gelişim ağacıyla simetri).

## Sonuç
Ar-Ge boş bir sayı oyunu değil; her sezon oynanan, aracın kimliğini belirleyen bir ağaç olur.

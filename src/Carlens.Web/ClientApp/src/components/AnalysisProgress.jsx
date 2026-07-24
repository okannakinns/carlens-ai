import {
  BrainCircuit,
  Check,
  CircleDot,
  FileSearch,
  ScanLine
} from "lucide-react";
import { AnimatePresence, motion } from "framer-motion";

const stages = [
  {
    key: "Queued",
    label: "İstek alındı",
    detail: "Analiz sırasına eklendi",
    icon: CircleDot
  },
  {
    key: "ReadingListing",
    label: "İlan okunuyor",
    detail: "Teknik veriler ve piyasa örnekleri toplanıyor",
    icon: FileSearch
  },
  {
    key: "AnalyzingVehicle",
    label: "Usta analizi",
    detail: "Fiyat, kilometre, model riskleri ve görseller değerlendiriliyor",
    icon: BrainCircuit
  },
  {
    key: "Completed",
    label: "Rapor hazır",
    detail: "Sonuçlar tek raporda birleştirildi",
    icon: Check
  }
];

export default function AnalysisProgress({ analysis, visible }) {
  const isManual = analysis?.listing?.inputType === "Manual";
  const visibleStages = isManual
    ? stages.filter((stage) => stage.key !== "ReadingListing")
    : stages;
  const currentIndex =
    analysis?.progressStage === "Failed"
      ? visibleStages.length - 1
      : Math.max(
          0,
          visibleStages.findIndex(
            (stage) => stage.key === analysis?.progressStage
          )
        );

  return (
    <AnimatePresence>
      {visible ? (
        <motion.section
          className="progress-section"
          id="analysis-workspace"
          initial={{ opacity: 0, y: 36 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -24 }}
          transition={{ duration: 0.45 }}
        >
          <div className="section-shell progress-layout">
            <div className="scan-visual" aria-hidden="true">
              <ScanLine size={60} strokeWidth={1.4} />
              <motion.span
                className="scan-visual__beam"
                animate={{ top: ["10%", "82%", "10%"] }}
                transition={{
                  duration: 2.8,
                  repeat: Infinity,
                  ease: "easeInOut"
                }}
              />
              <span className="scan-visual__ring scan-visual__ring--one" />
              <span className="scan-visual__ring scan-visual__ring--two" />
            </div>

            <div className="progress-copy">
              <p className="section-kicker">Canlı analiz</p>
              <h2>
                {isManual
                  ? "Araç ustanın tezgâhında"
                  : "İlan ustanın tezgâhında"}
              </h2>
              <p>
                {analysis?.listing?.title ??
                  (isManual
                    ? "Fotoğraflar ve araç bilgileri hazırlanıyor."
                    : "Araç bilgileri kaynaktan okunuyor.")}
              </p>

              <ol className="stage-list">
                {visibleStages.map((stage, index) => {
                  const Icon = stage.icon;
                  const isComplete = index < currentIndex;
                  const isActive = index === currentIndex;

                  return (
                    <li
                      className={[
                        "stage-list__item",
                        isComplete ? "is-complete" : "",
                        isActive ? "is-active" : ""
                      ]
                        .filter(Boolean)
                        .join(" ")}
                      key={stage.key}
                    >
                      <span className="stage-list__icon">
                        {isComplete ? (
                          <Check size={18} aria-hidden="true" />
                        ) : (
                          <Icon size={18} aria-hidden="true" />
                        )}
                      </span>
                      <span>
                        <strong>{stage.label}</strong>
                        <small>{stage.detail}</small>
                      </span>
                    </li>
                  );
                })}
              </ol>
            </div>
          </div>
        </motion.section>
      ) : null}
    </AnimatePresence>
  );
}

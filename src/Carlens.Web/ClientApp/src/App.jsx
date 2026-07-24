import {
  lazy,
  Suspense,
  useCallback,
  useEffect,
  useRef,
  useState
} from "react";
import { AnimatePresence } from "framer-motion";
import AnalysisProgress from "./components/AnalysisProgress";
import AppFooter from "./components/AppFooter";
import Hero from "./components/Hero";
import RecentAnalyses from "./components/RecentAnalyses";
import {
  createAnalysis,
  createManualAnalysis,
  getAnalyses,
  getAnalysis
} from "./lib/api";
import { normalizeUrl } from "./lib/formatters";

const terminalStatuses = new Set(["Completed", "Failed"]);
const AnalysisReport = lazy(() => import("./components/AnalysisReport"));
const wait = (milliseconds) =>
  new Promise((resolve) => window.setTimeout(resolve, milliseconds));

export default function App() {
  const [listingUrl, setListingUrl] = useState("");
  const [submittedUrl, setSubmittedUrl] = useState("");
  const [analyses, setAnalyses] = useState([]);
  const [activeAnalysis, setActiveAnalysis] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isHistoryLoading, setIsHistoryLoading] = useState(true);
  const [error, setError] = useState("");
  const runIdRef = useRef(0);
  const analysesRef = useRef([]);

  useEffect(() => {
    analysesRef.current = analyses;
  }, [analyses]);

  const loadHistory = useCallback(async (showLoading = false) => {
    if (showLoading) {
      setIsHistoryLoading(true);
    }

    try {
      const result = await getAnalyses();
      setAnalyses(Array.isArray(result) ? result : []);
    } catch {
      setError(
        "Analiz servisine ulaşılamadı. API ve Worker servislerini kontrol edin."
      );
    } finally {
      setIsHistoryLoading(false);
    }
  }, []);

  useEffect(() => {
    loadHistory(true);
  }, [loadHistory]);

  const scrollTo = useCallback((id) => {
    window.setTimeout(() => {
      document
        .getElementById(id)
        ?.scrollIntoView({ behavior: "smooth", block: "start" });
    }, 80);
  }, []);

  const handleReportReady = useCallback(() => {
    scrollTo("analysis-result");
  }, [scrollTo]);

  const pollUntilComplete = useCallback(async (analysisId, runId) => {
    for (let attempt = 0; attempt < 120; attempt += 1) {
      await wait(attempt === 0 ? 500 : 1800);

      if (runIdRef.current !== runId) {
        return null;
      }

      const latest = await getAnalysis(analysisId);
      setActiveAnalysis(latest);

      if (terminalStatuses.has(latest.status)) {
        return latest;
      }
    }

    throw new Error(
      "Analiz beklenenden uzun sürüyor. Son analizler bölümünden durumu takip edebilirsiniz."
    );
  }, []);

  const finishRun = useCallback(
    async (analysis) => {
      setIsSubmitting(false);
      await loadHistory();

      if (analysis?.status === "Completed") {
        scrollTo("analysis-result");
        return;
      }

      if (analysis?.status === "Failed") {
        setError(
          analysis.errorMessage ??
            "Analiz tamamlanamadı. Araç bilgilerini kontrol edip tekrar deneyin."
        );
      }
    },
    [loadHistory, scrollTo]
  );

  const resumeAnalysis = useCallback(
    async (analysis) => {
      const runId = ++runIdRef.current;
      setActiveAnalysis(analysis);
      setError("");

      if (terminalStatuses.has(analysis.status)) {
        setIsSubmitting(false);

        try {
          const detailedAnalysis = await getAnalysis(analysis.analysisId);
          setActiveAnalysis(detailedAnalysis);
          scrollTo("analysis-result");
        } catch (requestError) {
          setError(requestError.message);
        }

        return;
      }

      setIsSubmitting(true);
      scrollTo("analysis-workspace");

      try {
        const finalAnalysis = await pollUntilComplete(
          analysis.analysisId,
          runId
        );

        if (finalAnalysis) {
          await finishRun(finalAnalysis);
        }
      } catch (requestError) {
        setIsSubmitting(false);
        setError(requestError.message);
      }
    },
    [finishRun, pollUntilComplete, scrollTo]
  );

  const handleSubmit = async (submission) => {
    const isUrlAnalysis = submission.inputType === "Url";
    const trimmedUrl = isUrlAnalysis
      ? submission.listingUrl.trim()
      : "";
    const vehicle = submission.vehicle;

    if (isUrlAnalysis && !trimmedUrl) {
      setError("İlan bağlantısını girin.");
      return;
    }

    const runId = ++runIdRef.current;
    setSubmittedUrl(trimmedUrl);
    setIsSubmitting(true);
    setError("");
    setActiveAnalysis({
      analysisId: null,
      status: "Pending",
      progressStage: "Queued",
      listing: isUrlAnalysis
        ? {
            inputType: "Url",
            listingUrl: trimmedUrl
          }
        : {
            inputType: "Manual",
            listingUrl: null,
            title: [
              vehicle.modelYear,
              vehicle.brand,
              vehicle.series,
              vehicle.model
            ]
              .filter(Boolean)
              .join(" "),
            brand: vehicle.brand,
            series: vehicle.series,
            model: vehicle.model,
            modelYear: vehicle.modelYear,
            mileage: vehicle.mileage,
            price: vehicle.price,
            totalImageCount: submission.images.length
          }
    });
    scrollTo("analysis-workspace");

    try {
      const created = isUrlAnalysis
        ? await createAnalysis(trimmedUrl)
        : await createManualAnalysis(vehicle, submission.images);
      setActiveAnalysis(created);
      const finalAnalysis = await pollUntilComplete(
        created.analysisId,
        runId
      );

      if (finalAnalysis) {
        await finishRun(finalAnalysis);
      }
    } catch (requestError) {
      if (isUrlAnalysis && requestError.status === 409) {
        const existing = analysesRef.current.find(
          (analysis) =>
            normalizeUrl(analysis.listing?.listingUrl ?? "") ===
            normalizeUrl(trimmedUrl)
        );

        if (existing) {
          await resumeAnalysis(existing);
          return;
        }
      }

      setIsSubmitting(false);
      setActiveAnalysis(null);
      setError(requestError.message);
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

  const handleHistorySelect = (analysis) => {
    if (analysis.listing?.listingUrl) {
      setListingUrl(analysis.listing.listingUrl);
    } else if (submittedUrl) {
      setListingUrl(submittedUrl);
    }

    resumeAnalysis(analysis);
  };

  const showProgress =
    Boolean(activeAnalysis) &&
    !terminalStatuses.has(activeAnalysis.status);
  const showReport =
    activeAnalysis?.status === "Completed" && Boolean(activeAnalysis.report);

  return (
    <>
      <Hero
        listingUrl={listingUrl}
        onListingUrlChange={setListingUrl}
        onSubmit={handleSubmit}
        isSubmitting={isSubmitting}
        error={error}
      />

      <main>
        <div id="analysis-workspace">
          <AnalysisProgress
            analysis={activeAnalysis}
            visible={showProgress}
          />
        </div>

        <AnimatePresence mode="wait">
          {showReport ? (
            <Suspense
              fallback={
                <div className="report-loading" role="status">
                  Rapor hazırlanıyor.
                </div>
              }
            >
              <AnalysisReport
                analysis={activeAnalysis}
                key={activeAnalysis.analysisId}
                onReady={handleReportReady}
              />
            </Suspense>
          ) : null}
        </AnimatePresence>

        <RecentAnalyses
          analyses={analyses}
          loading={isHistoryLoading}
          onSelect={handleHistorySelect}
        />
      </main>

      <AppFooter />
    </>
  );
}

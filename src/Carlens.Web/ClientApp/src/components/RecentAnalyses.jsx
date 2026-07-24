import {
  ArrowUpRight,
  CalendarDays,
  CircleDollarSign,
  Gauge,
  History
} from "lucide-react";
import { motion } from "framer-motion";
import {
  formatCurrency,
  formatDate,
  formatNumber,
  hasNumericValue,
  translateRecommendation,
  translateStatus
} from "../lib/formatters";

export default function RecentAnalyses({ analyses, loading, onSelect }) {
  return (
    <section className="recent-section" id="recent-analyses">
      <div className="section-shell">
        <div className="section-heading">
          <div>
            <p className="section-kicker">Geçmiş</p>
            <h2>Son analizler</h2>
          </div>
          <History size={25} aria-hidden="true" />
        </div>

        {loading ? (
          <div className="history-empty">Analizler yükleniyor.</div>
        ) : analyses.length === 0 ? (
          <div className="history-empty">
            İlk analiziniz burada görünecek.
          </div>
        ) : (
          <div className="analysis-history">
            {analyses.slice(0, 8).map((analysis, index) => (
              <motion.button
                className="history-item"
                type="button"
                onClick={() => onSelect(analysis)}
                key={analysis.analysisId}
                initial={{ opacity: 0, y: 18 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ delay: Math.min(index * 0.05, 0.25) }}
              >
                <span
                  className={`status-dot status-dot--${analysis.status.toLowerCase()}`}
                  aria-hidden="true"
                />
                <span className="history-item__main">
                  <strong>
                    {analysis.listing?.title ??
                      analysis.listing?.listingUrl ??
                      "Araç ilanı"}
                  </strong>
                  <small>
                    {translateStatus(analysis.status)} ·{" "}
                    {formatDate(analysis.createdAtUtc)}
                  </small>
                </span>
                <span className="history-item__metric">
                  <CalendarDays size={15} aria-hidden="true" />
                  {analysis.listing?.modelYear ?? "-"}
                </span>
                <span className="history-item__metric">
                  <Gauge size={15} aria-hidden="true" />
                  {hasNumericValue(analysis.listing?.mileage)
                    ? `${formatNumber(analysis.listing.mileage)} km`
                    : "Veri yok"}
                </span>
                <span className="history-item__price">
                  <small>
                    {analysis.report
                      ? translateRecommendation(
                          analysis.report.recommendation
                        )
                      : translateStatus(analysis.status)}
                  </small>
                  <strong>
                    <CircleDollarSign size={15} aria-hidden="true" />
                    {formatCurrency(
                      analysis.report?.estimatedMarketPrice ??
                        analysis.listing?.price
                    )}
                  </strong>
                </span>
                <ArrowUpRight
                  className="history-item__arrow"
                  size={19}
                  aria-hidden="true"
                />
              </motion.button>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

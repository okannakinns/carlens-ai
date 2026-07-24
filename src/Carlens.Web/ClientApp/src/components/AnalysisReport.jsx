import { useEffect } from "react";
import {
  AlertTriangle,
  BadgeCheck,
  CalendarDays,
  CarFront,
  CheckCircle2,
  CircleDollarSign,
  ExternalLink,
  Fuel,
  Gauge,
  Images,
  MapPin,
  Milestone,
  ScanSearch,
  ShieldAlert,
  ThumbsUp,
  Wrench
} from "lucide-react";
import { motion } from "framer-motion";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  PolarAngleAxis,
  RadialBar,
  RadialBarChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from "recharts";
import {
  formatCurrency,
  formatDate,
  formatNumber,
  hasNumericValue,
  translateFuelType,
  translatePriceAssessment,
  translateRecommendation,
  translateTransmission
} from "../lib/formatters";

const recommendationTone = {
  Buy: "positive",
  ConsiderAfterInspection: "caution",
  Avoid: "negative"
};

const sectionMotion = {
  initial: { opacity: 0, y: 24 },
  whileInView: { opacity: 1, y: 0 },
  viewport: { once: true, amount: 0.16 },
  transition: { duration: 0.45 }
};

function Stat({ icon: Icon, label, value }) {
  return (
    <div className="vehicle-stat">
      <Icon size={18} aria-hidden="true" />
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function ReportCard({ icon: Icon, eyebrow, title, children, tone = "neutral" }) {
  return (
    <motion.article
      className={`report-card report-card--${tone}`}
      {...sectionMotion}
    >
      <div className="report-card__heading">
        <span className="report-card__icon">
          <Icon size={22} aria-hidden="true" />
        </span>
        <div>
          <span>{eyebrow}</span>
          <h3>{title}</h3>
        </div>
      </div>
      <div className="report-card__copy">{children}</div>
    </motion.article>
  );
}

function ReportPoints({ items, icon: Icon = CheckCircle2, compact = false }) {
  const normalizedItems = Array.isArray(items)
    ? items
    : String(items ?? "")
        .split(/\r?\n/)
        .filter(Boolean);

  return (
    <ul className={`report-points${compact ? " report-points--compact" : ""}`}>
      {normalizedItems.map((item, index) => (
        <li key={`${item}-${index}`}>
          <span className="report-points__icon">
            <Icon size={compact ? 14 : 16} aria-hidden="true" />
          </span>
          <span>{item}</span>
        </li>
      ))}
    </ul>
  );
}

function PriceIntelligence({ listing, report }) {
  const isManual = listing.inputType === "Manual";
  const enteredPrice = Number(listing.price) || 0;
  const chartData = [
    {
      name: isManual ? "Girilen" : "İlan",
      value: enteredPrice,
      color: "#ef594c"
    },
    {
      name: "Piyasa",
      value: Number(report.estimatedMarketPrice) || 0,
      color: "#12a594"
    }
  ].filter((item) => item.value > 0);

  const rangeMin = Number(report.estimatedMarketPriceMin) || 0;
  const rangeMax = Number(report.estimatedMarketPriceMax) || 0;
  const rangeSpan = Math.max(rangeMax - rangeMin, 1);
  const askingPosition = Math.max(
    0,
    Math.min(100, ((enteredPrice - rangeMin) / rangeSpan) * 100)
  );

  return (
    <motion.article className="price-panel" {...sectionMotion}>
      <div className="panel-heading">
        <div>
          <span>Piyasa zekâsı</span>
          <h3>{translatePriceAssessment(report.priceAssessment)}</h3>
        </div>
        <CircleDollarSign size={27} aria-hidden="true" />
      </div>

      <div className="price-panel__numbers">
        <div>
          <span>Tahmini piyasa</span>
          <strong>{formatCurrency(report.estimatedMarketPrice)}</strong>
        </div>
        <div>
          <span>{isManual ? "Girilen fiyat" : "İlan fiyatı"}</span>
          <strong>{formatCurrency(listing.price)}</strong>
        </div>
      </div>

      {rangeMin > 0 && rangeMax > 0 ? (
        <div className="market-range">
          <div className="market-range__labels">
            <span>{formatCurrency(rangeMin)}</span>
            <span>{formatCurrency(rangeMax)}</span>
          </div>
          <div className="market-range__track">
            {enteredPrice > 0 ? (
              <span
                className="market-range__asking"
                style={{ left: `${askingPosition}%` }}
                title={`${isManual ? "Girilen fiyat" : "İlan fiyatı"}: ${formatCurrency(listing.price)}`}
              />
            ) : null}
          </div>
          <small>
            {listing.comparableCount > 0
              ? `${listing.comparableCount} benzer ilan üzerinden makul piyasa aralığı`
              : "Canlı piyasa örneği olmadan AI tahmini"}
          </small>
        </div>
      ) : null}

      {chartData.length > 0 ? (
        <div className="price-chart" aria-label="Fiyat ve piyasa tahmini grafiği">
          <ResponsiveContainer width="100%" height={180}>
            <BarChart
              data={chartData}
              layout="vertical"
              margin={{ top: 8, right: 8, bottom: 4, left: 8 }}
            >
              <CartesianGrid horizontal={false} stroke="#dfe3df" />
              <XAxis
                type="number"
                tickFormatter={(value) => `${Math.round(value / 1000)}B`}
                axisLine={false}
                tickLine={false}
                fontSize={11}
              />
              <YAxis
                dataKey="name"
                type="category"
                axisLine={false}
                tickLine={false}
                width={44}
                fontSize={12}
              />
              <Tooltip
                formatter={(value) => [formatCurrency(value), "Fiyat"]}
                cursor={{ fill: "#f1f4f1" }}
              />
              <Bar dataKey="value" radius={[0, 4, 4, 0]} barSize={22}>
                {chartData.map((entry) => (
                  <Cell fill={entry.color} key={entry.name} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      ) : null}

      <ReportPoints
        items={report.priceEvaluation}
        icon={CircleDollarSign}
        compact
      />
    </motion.article>
  );
}

function ConfidencePanel({ score }) {
  const safeScore = Math.max(0, Math.min(100, Number(score) || 0));

  return (
    <motion.article className="confidence-panel" {...sectionMotion}>
      <div className="panel-heading">
        <div>
          <span>Analiz güveni</span>
          <h3>Veri kapsamı</h3>
        </div>
        <BadgeCheck size={27} aria-hidden="true" />
      </div>
      <div className="confidence-chart">
        <ResponsiveContainer width="100%" height={190}>
          <RadialBarChart
            innerRadius="72%"
            outerRadius="100%"
            data={[{ value: safeScore, fill: "#f2c84b" }]}
            startAngle={210}
            endAngle={-30}
          >
            <PolarAngleAxis
              type="number"
              domain={[0, 100]}
              angleAxisId={0}
              tick={false}
            />
            <RadialBar
              dataKey="value"
              background={{ fill: "#26302b" }}
              cornerRadius={4}
            />
          </RadialBarChart>
        </ResponsiveContainer>
        <div className="confidence-chart__value">
          <strong>%{safeScore}</strong>
          <span>güven</span>
        </div>
      </div>
      <p>
        Nihai karar için araç başında ekspertiz ve servis kaydı doğrulaması
        gerekir.
      </p>
    </motion.article>
  );
}

export default function AnalysisReport({ analysis, onReady }) {
  const { listing, report, usage } = analysis;

  useEffect(() => {
    onReady?.();
  }, [onReady]);

  if (!listing || !report) {
    return null;
  }

  const tone = recommendationTone[report.recommendation] ?? "caution";
  const isManual = listing.inputType === "Manual";
  const coverImage =
    listing.imageUrls?.[0] ?? "/assets/carlens-workshop-hero.webp";

  return (
    <motion.section
      className="report"
      id="analysis-result"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.5 }}
    >
      <div className={`decision-band decision-band--${tone}`}>
        <div className="section-shell decision-band__inner">
          <div>
            <span className="decision-band__label">Ustanın kararı</span>
            <h2>{translateRecommendation(report.recommendation)}</h2>
          </div>
          <p>{report.summary}</p>
          {listing.listingUrl ? (
            <a
              className="source-link"
              href={listing.listingUrl}
              target="_blank"
              rel="noreferrer"
            >
              <span>İlanı aç</span>
              <ExternalLink size={17} aria-hidden="true" />
            </a>
          ) : null}
        </div>
      </div>

      <div className="vehicle-overview">
        <div className="vehicle-overview__media">
          <img
            src={coverImage}
            alt={listing.title ?? (isManual ? "Manuel girilen araç" : "İlan aracı")}
          />
          <span className="vehicle-overview__image-count">
            <Images size={16} aria-hidden="true" />
            {listing.totalImageCount} fotoğraf
          </span>
        </div>
        <div className="vehicle-overview__content">
          <p className="section-kicker">
            {isManual
              ? "Manuel girilen araç"
              : `İlan #${listing.externalListingId ?? "-"}`}
          </p>
          <h2>{listing.title ?? (isManual ? "Araç analizi" : "Araç ilanı")}</h2>
          <p className="vehicle-overview__model">
            {[listing.brand, listing.series, listing.model]
              .filter(Boolean)
              .join(" ")}
          </p>

          <div className="vehicle-stats">
            <Stat
              icon={CalendarDays}
              label="Model yılı"
              value={listing.modelYear ?? "Yok"}
            />
            <Stat
              icon={Gauge}
              label="Kilometre"
              value={
                hasNumericValue(listing.mileage)
                  ? `${formatNumber(listing.mileage)} km`
                  : "Veri yok"
              }
            />
            <Stat
              icon={Fuel}
              label="Yakıt"
              value={translateFuelType(listing.fuelType)}
            />
            <Stat
              icon={Milestone}
              label="Vites"
              value={translateTransmission(listing.transmissionType)}
            />
            <Stat
              icon={MapPin}
              label="Konum"
              value={listing.location ?? "Belirtilmemiş"}
            />
            <Stat
              icon={CircleDollarSign}
              label={isManual ? "Girilen fiyat" : "İlan fiyatı"}
              value={formatCurrency(listing.price)}
            />
          </div>
        </div>
      </div>

      <div className="report-body section-shell">
        <div className="report-intelligence-grid">
          <PriceIntelligence listing={listing} report={report} />
          <ConfidencePanel score={report.confidenceScore} />
        </div>

        <div className="report-section-grid">
          <ReportCard
            icon={Gauge}
            eyebrow="Kullanım yükü"
            title="Kilometre değerlendirmesi"
            tone="cyan"
          >
            <ReportPoints items={report.mileageEvaluation} icon={Gauge} />
          </ReportCard>

          <ReportCard
            icon={Wrench}
            eyebrow="Model bilgisi"
            title="Bilinen kronik riskler"
            tone="yellow"
          >
            <ReportPoints items={report.knownIssues} icon={Wrench} />
          </ReportCard>

          <ReportCard
            icon={ThumbsUp}
            eyebrow="Karar gerekçesi"
            title="Neden alınır, neden alınmaz?"
            tone={tone}
          >
            <ReportPoints items={report.buyReasoning} icon={ThumbsUp} />
          </ReportCard>

          <ReportCard
            icon={ShieldAlert}
            eyebrow={isManual ? "Araç bulguları" : "İlan bulguları"}
            title="Riskler ve sorulacaklar"
            tone="red"
          >
            <ReportPoints items={report.riskNotes} icon={AlertTriangle} />
          </ReportCard>

          <ReportCard
            icon={CheckCircle2}
            eyebrow="Araç başında"
            title="Ekspertiz kontrol listesi"
            tone="green"
          >
            <ReportPoints
              items={report.inspectionChecklist}
              icon={CheckCircle2}
            />
          </ReportCard>
        </div>

        {listing.imageUrls?.length > 1 ? (
          <motion.section className="gallery-section" {...sectionMotion}>
            <div className="section-heading">
              <div>
                <p className="section-kicker">
                  {isManual ? "Araç fotoğrafları" : "İlan galerisi"}
                </p>
                <h2>İncelenen araç</h2>
              </div>
              <span>
                {isManual
                  ? "AI değerlendirmesine dahil edilen araç görselleri."
                  : "AI, maliyet dengesi için galeriden temsilî görselleri seçti."}
              </span>
            </div>
            <div className="vehicle-gallery">
              {listing.imageUrls.slice(0, 12).map((url, index) => (
                <figure key={url}>
                  <img
                    src={url}
                    alt={`${isManual ? "Araç" : "İlan"} fotoğrafı ${index + 1}`}
                    loading="lazy"
                  />
                  <figcaption>{String(index + 1).padStart(2, "0")}</figcaption>
                </figure>
              ))}
            </div>
          </motion.section>
        ) : null}

        <div className="report-meta">
          <span>
            <ScanSearch size={16} aria-hidden="true" />
            {usage.analyzedImageCount} görsel analiz edildi
          </span>
          <span>
            <CarFront size={16} aria-hidden="true" />
            {listing.comparableCount} piyasa örneği
          </span>
          <span>
            <AlertTriangle size={16} aria-hidden="true" />
            AI raporu ekspertiz yerine geçmez
          </span>
          <time dateTime={analysis.completedAtUtc}>
            {formatDate(analysis.completedAtUtc)}
          </time>
        </div>
      </div>
    </motion.section>
  );
}

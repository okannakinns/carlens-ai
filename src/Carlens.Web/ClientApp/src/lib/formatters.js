const currencyFormatter = new Intl.NumberFormat("tr-TR", {
  style: "currency",
  currency: "TRY",
  maximumFractionDigits: 0
});

const numberFormatter = new Intl.NumberFormat("tr-TR", {
  maximumFractionDigits: 0
});

const dateFormatter = new Intl.DateTimeFormat("tr-TR", {
  dateStyle: "medium",
  timeStyle: "short",
  timeZone: "Europe/Istanbul"
});

export function formatCurrency(value) {
  return hasNumericValue(value)
    ? currencyFormatter.format(Number(value))
    : "Veri yok";
}

export function formatNumber(value) {
  return hasNumericValue(value)
    ? numberFormatter.format(Number(value))
    : "Veri yok";
}

export function hasNumericValue(value) {
  return (
    value !== null &&
    value !== undefined &&
    value !== "" &&
    Number.isFinite(Number(value))
  );
}

export function formatDate(value) {
  if (!value) {
    return "Bekleniyor";
  }

  return dateFormatter.format(new Date(value));
}

export function translateStatus(status) {
  return {
    Pending: "Sırada",
    Processing: "İşleniyor",
    Completed: "Tamamlandı",
    Failed: "Başarısız"
  }[status] ?? status;
}

export function translateRecommendation(recommendation) {
  return {
    Buy: "Alınabilir",
    ConsiderAfterInspection: "Ekspertiz sonrası değerlendir",
    Avoid: "Uzak dur"
  }[recommendation] ?? "Temkinli değerlendir";
}

export function translatePriceAssessment(assessment) {
  return {
    BelowMarket: "Piyasanın altında",
    Fair: "Piyasa bandında",
    AboveMarket: "Piyasanın üstünde",
    InsufficientData: "Veri yetersiz"
  }[assessment] ?? "Veri yetersiz";
}

export function translateFuelType(value) {
  return {
    Gasoline: "Benzin",
    Diesel: "Dizel",
    LPG: "LPG",
    Hybrid: "Hibrit",
    Electric: "Elektrik",
    Unknown: "Belirtilmemiş"
  }[value] ?? value ?? "Belirtilmemiş";
}

export function translateTransmission(value) {
  return {
    Manual: "Manuel",
    Automatic: "Otomatik",
    SemiAutomatic: "Yarı otomatik",
    Unknown: "Belirtilmemiş"
  }[value] ?? value ?? "Belirtilmemiş";
}

export function normalizeUrl(value) {
  try {
    const url = new URL(value);
    url.hash = "";
    url.search = "";
    return url.toString().replace(/\/$/, "").toLocaleLowerCase("tr-TR");
  } catch {
    return value.trim().replace(/\/$/, "").toLocaleLowerCase("tr-TR");
  }
}

let csrfTokenPromise;

async function getCsrfToken(signal) {
  if (!csrfTokenPromise) {
    csrfTokenPromise = fetch("/api/security/csrf-token", {
      headers: { Accept: "application/json" },
      signal
    })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error("Güvenlik doğrulaması başlatılamadı.");
        }

        const body = await response.json();

        if (!body?.token) {
          throw new Error("Güvenlik doğrulama anahtarı alınamadı.");
        }

        return body.token;
      })
      .catch((error) => {
        csrfTokenPromise = undefined;
        throw error;
      });
  }

  return csrfTokenPromise;
}

async function request(path, options = {}) {
  const isMultipart =
    typeof FormData !== "undefined" && options.body instanceof FormData;
  const method = options.method?.toUpperCase() ?? "GET";
  const requiresCsrfToken = !["GET", "HEAD", "OPTIONS"].includes(method);
  const csrfToken = requiresCsrfToken
    ? await getCsrfToken(options.signal)
    : null;
  const response = await fetch(path, {
    headers: {
      Accept: "application/json",
      ...(options.body && !isMultipart
        ? { "Content-Type": "application/json" }
        : {}),
      ...(csrfToken ? { "X-CSRF-TOKEN": csrfToken } : {}),
      ...options.headers
    },
    ...options
  });

  const contentType = response.headers.get("content-type") ?? "";
  const body = contentType.includes("application/json")
    ? await response.json()
    : null;

  if (!response.ok) {
    const validationMessage = Object.values(body?.errors ?? {})
      .flat()
      .find(Boolean);
    const error = new Error(
      validationMessage ??
        body?.detail ??
        body?.title ??
        "İstek tamamlanamadı."
    );
    error.status = response.status;
    error.problem = body;
    throw error;
  }

  return body;
}

export function getAnalyses(signal) {
  return request("/api/analyses", { signal });
}

export function getAnalysis(id, signal) {
  return request(`/api/analyses/${id}`, { signal });
}

export function createAnalysis(listingUrl, signal) {
  return request("/api/analyses", {
    method: "POST",
    body: JSON.stringify({ listingUrl }),
    signal
  });
}

export async function createManualAnalysis(vehicle, images, signal) {
  const form = new FormData();
  const fields = {
    brand: vehicle.brand,
    series: vehicle.series,
    model: vehicle.model,
    modelYear: vehicle.modelYear,
    price: vehicle.price,
    mileage: vehicle.mileage,
    fuelType: vehicle.fuelType,
    transmissionType: vehicle.transmissionType,
    location: vehicle.location,
    description: vehicle.description,
    damageInformation: vehicle.damageInformation
  };

  Object.entries(fields).forEach(([name, value]) => {
    if (value !== null && value !== undefined && String(value).trim() !== "") {
      form.append(name, String(value).trim());
    }
  });

  const optimizedImages = await Promise.all(
    images.map((image) => optimizeVehicleImage(image))
  );

  optimizedImages.forEach((image) => {
    form.append("images", image, image.name);
  });

  return request("/api/analyses/manual", {
    method: "POST",
    body: form,
    signal
  });
}

async function optimizeVehicleImage(file) {
  if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
    throw new Error("Yalnızca JPEG, PNG veya WebP fotoğraf yükleyebilirsiniz.");
  }

  let bitmap;

  try {
    bitmap = await createImageBitmap(file, { imageOrientation: "from-image" });
  } catch {
    throw new Error(
      `${file.name} açılamadı. JPEG, PNG veya WebP formatında tekrar deneyin.`
    );
  }

  const maximumDimension = 1280;
  const scale = Math.min(
    1,
    maximumDimension / Math.max(bitmap.width, bitmap.height)
  );
  const width = Math.max(1, Math.round(bitmap.width * scale));
  const height = Math.max(1, Math.round(bitmap.height * scale));
  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = height;

  const context = canvas.getContext("2d", { alpha: false });

  if (!context) {
    bitmap.close();
    throw new Error("Fotoğraf bu tarayıcıda optimize edilemedi.");
  }

  context.fillStyle = "#ffffff";
  context.fillRect(0, 0, width, height);
  context.drawImage(bitmap, 0, 0, width, height);
  bitmap.close();

  const blob = await new Promise((resolve, reject) => {
    canvas.toBlob(
      (result) =>
        result
          ? resolve(result)
          : reject(new Error("Fotoğraf optimize edilemedi.")),
      "image/jpeg",
      0.82
    );
  });

  const baseName = file.name.replace(/\.[^.]+$/, "") || "arac-fotografi";
  return new File([blob], `${baseName}.jpg`, {
    type: "image/jpeg",
    lastModified: file.lastModified
  });
}

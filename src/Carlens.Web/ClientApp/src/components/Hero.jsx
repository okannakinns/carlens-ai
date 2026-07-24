import { useEffect, useLayoutEffect, useRef, useState } from "react";
import {
  ArrowRight,
  CarFront,
  Check,
  ChevronDown,
  ImagePlus,
  Link2,
  Minus,
  Plus,
  ShieldCheck,
  Upload,
  X
} from "lucide-react";
import { AnimatePresence, motion } from "framer-motion";
import AppHeader from "./AppHeader";

const emptyVehicle = {
  brand: "",
  series: "",
  model: "",
  modelYear: "",
  price: "",
  mileage: "",
  fuelType: "",
  transmissionType: "",
  location: "",
  description: "",
  damageInformation: ""
};

export default function Hero({
  listingUrl,
  onListingUrlChange,
  onSubmit,
  isSubmitting,
  error
}) {
  const [inputMode, setInputMode] = useState("Url");
  const [vehicle, setVehicle] = useState(emptyVehicle);
  const [images, setImages] = useState([]);
  const [previews, setPreviews] = useState([]);
  const [isDragging, setIsDragging] = useState(false);
  const [localError, setLocalError] = useState("");
  const imageInputRef = useRef(null);

  useEffect(() => {
    const nextPreviews = images.map((file) => ({
      file,
      url: URL.createObjectURL(file)
    }));
    setPreviews(nextPreviews);

    return () => {
      nextPreviews.forEach((preview) => URL.revokeObjectURL(preview.url));
    };
  }, [images]);

  const changeMode = (mode) => {
    if (isSubmitting) {
      return;
    }

    setInputMode(mode);
    setLocalError("");
  };

  const updateVehicle = (field, value) => {
    setVehicle((current) => ({ ...current, [field]: value }));
  };

  const addImages = (fileList) => {
    const candidates = Array.from(fileList ?? []);
    const supportedTypes = new Set(["image/jpeg", "image/png", "image/webp"]);
    const invalidType = candidates.find((file) => !supportedTypes.has(file.type));
    const oversized = candidates.find((file) => file.size > 12 * 1024 * 1024);

    if (invalidType) {
      setLocalError("Yalnızca JPEG, PNG veya WebP fotoğraf yükleyebilirsiniz.");
      return;
    }

    if (oversized) {
      setLocalError("Bir fotoğrafın orijinal boyutu 12 MB'ı geçemez.");
      return;
    }

    setImages((current) => {
      const merged = [...current];

      candidates.forEach((file) => {
        const isDuplicate = merged.some(
          (item) =>
            item.name === file.name &&
            item.size === file.size &&
            item.lastModified === file.lastModified
        );

        if (!isDuplicate && merged.length < 5) {
          merged.push(file);
        }
      });

      return merged;
    });

    if (images.length + candidates.length > 5) {
      setLocalError("En fazla 5 fotoğraf yükleyebilirsiniz.");
    } else {
      setLocalError("");
    }

    if (imageInputRef.current) {
      imageInputRef.current.value = "";
    }
  };

  const removeImage = (index) => {
    setImages((current) => current.filter((_, itemIndex) => itemIndex !== index));
    setLocalError("");
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    setLocalError("");

    if (inputMode === "Url") {
      if (!listingUrl.trim()) {
        setLocalError("Arabam.com ilan bağlantısını girin.");
        return;
      }

      onSubmit({
        inputType: "Url",
        listingUrl: listingUrl.trim()
      });
      return;
    }

    if (images.length < 1 || images.length > 5) {
      setLocalError("Araç için 1 ile 5 arasında fotoğraf yükleyin.");
      return;
    }

    if (!vehicle.fuelType || !vehicle.transmissionType) {
      setLocalError("Yakıt ve vites bilgilerini seçin.");
      return;
    }

    onSubmit({
      inputType: "Manual",
      vehicle,
      images
    });
  };

  const visibleError = localError || error;

  return (
    <section
      className={`hero hero--${inputMode.toLowerCase()}`}
      id="top"
    >
      <div className="hero__image" aria-hidden="true" />
      <div className="hero__shade" aria-hidden="true" />
      <AppHeader />

      <motion.div
        className="hero__content"
        layout
        initial={{ opacity: 0, y: 24 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{
          duration: 0.65,
          ease: [0.22, 1, 0.36, 1],
          layout: { duration: 0.52, ease: [0.22, 1, 0.36, 1] }
        }}
      >
        <div className="hero__eyebrow">
          <ShieldCheck size={17} aria-hidden="true" />
          <span>Bağımsız araç analizi</span>
        </div>
        <h1>Carlens AI</h1>
        <p className="hero__lead">
          İlan bağlantısını inceletin veya araç bilgilerini fotoğraflarla
          birlikte manuel girin.
        </p>

        <div className="input-mode" role="group" aria-label="Analiz giriş türü">
          <button
            className={inputMode === "Url" ? "is-active" : ""}
            type="button"
            aria-pressed={inputMode === "Url"}
            onClick={() => changeMode("Url")}
            disabled={isSubmitting}
          >
            <Link2 size={18} aria-hidden="true" />
            <span>İlan bağlantısı</span>
          </button>
          <button
            className={inputMode === "Manual" ? "is-active" : ""}
            type="button"
            aria-pressed={inputMode === "Manual"}
            onClick={() => changeMode("Manual")}
            disabled={isSubmitting}
          >
            <CarFront size={18} aria-hidden="true" />
            <span>Manuel giriş</span>
          </button>
        </div>

        <motion.form
          layout="position"
          onSubmit={handleSubmit}
          transition={{ layout: { duration: 0.52, ease: [0.22, 1, 0.36, 1] } }}
        >
          <AnimatedHeight>
            <AnimatePresence mode="popLayout" initial={false}>
              {inputMode === "Url" ? (
                <motion.div
                  className="analysis-form"
                  key="url-form"
                  initial={{ opacity: 0, y: 12 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -8 }}
                  transition={{ duration: 0.24 }}
                >
                  <div className="analysis-form__field">
                    <Link2 size={22} aria-hidden="true" />
                    <label className="sr-only" htmlFor="listing-url">
                      Arabam.com ilan bağlantısı
                    </label>
                    <input
                      id="listing-url"
                      type="url"
                      inputMode="url"
                      autoComplete="url"
                      placeholder="Arabam.com ilan bağlantısını yapıştırın"
                      value={listingUrl}
                      onChange={(event) =>
                        onListingUrlChange(event.target.value)
                      }
                      disabled={isSubmitting}
                      required
                    />
                  </div>
                  <SubmitButton isSubmitting={isSubmitting} />
                </motion.div>
              ) : (
                <motion.div
                  className="manual-analysis-form"
                  key="manual-form"
                  initial={{ opacity: 0, y: 12 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -8 }}
                  transition={{ duration: 0.24 }}
                >
                <div className="manual-fields">
                  <TextField
                    id="vehicle-brand"
                    label="Marka"
                    value={vehicle.brand}
                    onChange={(value) => updateVehicle("brand", value)}
                    placeholder="Örn. Volkswagen"
                    required
                    disabled={isSubmitting}
                  />
                  <TextField
                    id="vehicle-series"
                    label="Seri"
                    value={vehicle.series}
                    onChange={(value) => updateVehicle("series", value)}
                    placeholder="Örn. Golf"
                    disabled={isSubmitting}
                  />
                  <TextField
                    id="vehicle-model"
                    label="Model / versiyon"
                    value={vehicle.model}
                    onChange={(value) => updateVehicle("model", value)}
                    placeholder="Örn. 1.6 TDI Comfortline"
                    required
                    disabled={isSubmitting}
                  />
                  <NumberField
                    id="vehicle-year"
                    label="Model yılı"
                    value={vehicle.modelYear}
                    onChange={(value) => updateVehicle("modelYear", value)}
                    min="1900"
                    max={new Date().getFullYear() + 1}
                    step="1"
                    placeholder="2020"
                    required
                    disabled={isSubmitting}
                  />
                  <NumberField
                    id="vehicle-mileage"
                    label="Kilometre"
                    value={vehicle.mileage}
                    onChange={(value) => updateVehicle("mileage", value)}
                    min="0"
                    max="2000000"
                    step="1000"
                    placeholder="85000"
                    required
                    disabled={isSubmitting}
                  />
                  <NumberField
                    id="vehicle-price"
                    label="Fiyat beklentisi"
                    value={vehicle.price}
                    onChange={(value) => updateVehicle("price", value)}
                    min="1"
                    step="10000"
                    placeholder="İsteğe bağlı"
                    disabled={isSubmitting}
                  />
                  <SelectField
                    id="vehicle-fuel"
                    label="Yakıt"
                    value={vehicle.fuelType}
                    onChange={(value) => updateVehicle("fuelType", value)}
                    required
                    disabled={isSubmitting}
                    options={[
                      ["1", "Benzin"],
                      ["2", "Dizel"],
                      ["3", "LPG"],
                      ["4", "Hibrit"],
                      ["5", "Elektrik"]
                    ]}
                  />
                  <SelectField
                    id="vehicle-transmission"
                    label="Vites"
                    value={vehicle.transmissionType}
                    onChange={(value) =>
                      updateVehicle("transmissionType", value)
                    }
                    required
                    disabled={isSubmitting}
                    options={[
                      ["1", "Manuel"],
                      ["2", "Otomatik"],
                      ["3", "Yarı otomatik"]
                    ]}
                  />
                  <TextField
                    id="vehicle-location"
                    label="Konum"
                    value={vehicle.location}
                    onChange={(value) => updateVehicle("location", value)}
                    placeholder="Örn. İstanbul"
                    disabled={isSubmitting}
                  />
                </div>

                <div className="manual-notes">
                  <label>
                    <span>Araç notları</span>
                    <textarea
                      value={vehicle.description}
                      onChange={(event) =>
                        updateVehicle("description", event.target.value)
                      }
                      placeholder="Bakım geçmişi, son yapılan işlemler, bildiğiniz sorunlar"
                      rows="3"
                      disabled={isSubmitting}
                    />
                  </label>
                  <label>
                    <span>Boya, değişen ve tramer</span>
                    <textarea
                      value={vehicle.damageInformation}
                      onChange={(event) =>
                        updateVehicle(
                          "damageInformation",
                          event.target.value
                        )
                      }
                      placeholder="Varsa bildiğiniz hasar ve kaporta bilgileri"
                      rows="3"
                      disabled={isSubmitting}
                    />
                  </label>
                </div>

                <div
                  className={`image-uploader${isDragging ? " is-dragging" : ""}`}
                  onDragEnter={(event) => {
                    event.preventDefault();
                    setIsDragging(true);
                  }}
                  onDragOver={(event) => event.preventDefault()}
                  onDragLeave={() => setIsDragging(false)}
                  onDrop={(event) => {
                    event.preventDefault();
                    setIsDragging(false);
                    addImages(event.dataTransfer.files);
                  }}
                >
                  <input
                    ref={imageInputRef}
                    id="vehicle-images"
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    multiple
                    onChange={(event) => addImages(event.target.files)}
                    disabled={isSubmitting || images.length >= 5}
                  />
                  <label htmlFor="vehicle-images">
                    <span className="image-uploader__icon">
                      <Upload size={22} aria-hidden="true" />
                    </span>
                    <span>
                      <strong>Fotoğraf ekle</strong>
                      <small>{images.length}/5 seçildi</small>
                    </span>
                  </label>

                  {previews.length > 0 ? (
                    <div className="image-previews">
                      {previews.map((preview, index) => (
                        <figure key={`${preview.file.name}-${index}`}>
                          <img
                            src={preview.url}
                            alt={`Araç fotoğrafı ${index + 1}`}
                          />
                          <button
                            type="button"
                            title="Fotoğrafı kaldır"
                            aria-label={`${index + 1}. fotoğrafı kaldır`}
                            onClick={() => removeImage(index)}
                            disabled={isSubmitting}
                          >
                            <X size={15} aria-hidden="true" />
                          </button>
                        </figure>
                      ))}
                    </div>
                  ) : (
                    <div className="image-uploader__empty">
                      <ImagePlus size={18} aria-hidden="true" />
                      <span>1-5 araç fotoğrafı</span>
                    </div>
                  )}
                </div>

                  <SubmitButton isSubmitting={isSubmitting} />
                </motion.div>
              )}
            </AnimatePresence>
          </AnimatedHeight>
        </motion.form>

        <div className="hero__message" aria-live="polite">
          {visibleError ? (
            <span className="form-error">{visibleError}</span>
          ) : null}
        </div>
      </motion.div>

      <a className="hero__next" href="#analysis-workspace">
        <span>Usta raporu</span>
        <span className="hero__next-line" aria-hidden="true" />
      </a>
    </section>
  );
}

function AnimatedHeight({ children }) {
  const contentRef = useRef(null);
  const [height, setHeight] = useState(null);

  useLayoutEffect(() => {
    const content = contentRef.current;

    if (!content) {
      return undefined;
    }

    const updateHeight = () => {
      setHeight(content.getBoundingClientRect().height);
    };
    const resizeObserver = new ResizeObserver(updateHeight);

    updateHeight();
    resizeObserver.observe(content);

    return () => resizeObserver.disconnect();
  }, []);

  return (
    <motion.div
      className="analysis-form-stage"
      initial={false}
      animate={height === null ? undefined : { height }}
      transition={{
        height: {
          duration: 0.58,
          ease: [0.22, 1, 0.36, 1]
        }
      }}
    >
      <div ref={contentRef}>{children}</div>
    </motion.div>
  );
}

function SubmitButton({ isSubmitting }) {
  return (
    <button
      className="primary-action"
      type="submit"
      disabled={isSubmitting}
    >
      <span>{isSubmitting ? "Hazırlanıyor" : "Analiz et"}</span>
      <ArrowRight size={20} aria-hidden="true" />
    </button>
  );
}

function TextField({
  id,
  label,
  value,
  onChange,
  placeholder,
  required = false,
  disabled = false
}) {
  return (
    <label className="manual-field" htmlFor={id}>
      <span>{label}</span>
      <input
        id={id}
        type="text"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        required={required}
        disabled={disabled}
      />
    </label>
  );
}

function NumberField({
  id,
  label,
  value,
  onChange,
  placeholder,
  min,
  max,
  step = "1",
  required = false,
  disabled = false
}) {
  const numericValue = Number(value);
  const numericMin = min === undefined ? Number.NEGATIVE_INFINITY : Number(min);
  const numericMax = max === undefined ? Number.POSITIVE_INFINITY : Number(max);
  const numericStep = Number(step);
  const isAtMinimum =
    value !== "" && Number.isFinite(numericValue) && numericValue <= numericMin;
  const isAtMaximum =
    value !== "" && Number.isFinite(numericValue) && numericValue >= numericMax;

  const adjustValue = (direction) => {
    const fallbackValue =
      direction > 0
        ? Math.max(Number.isFinite(numericMin) ? numericMin : 0, numericStep)
        : Number.isFinite(numericMin)
          ? numericMin
          : 0;
    const currentValue =
      value === "" || !Number.isFinite(numericValue)
        ? fallbackValue - direction * numericStep
        : numericValue;
    const nextValue = Math.min(
      numericMax,
      Math.max(numericMin, currentValue + direction * numericStep)
    );

    onChange(String(nextValue));
  };

  return (
    <div className="manual-field">
      <span id={`${id}-label`}>{label}</span>
      <div className="number-control">
        <input
          id={id}
          aria-labelledby={`${id}-label`}
          type="number"
          inputMode="numeric"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder={placeholder}
          min={min}
          max={max}
          step={step}
          required={required}
          disabled={disabled}
        />
        <button
          type="button"
          title={`${label} değerini azalt`}
          aria-label={`${label} değerini azalt`}
          onClick={() => adjustValue(-1)}
          disabled={disabled || isAtMinimum}
        >
          <Minus size={15} aria-hidden="true" />
        </button>
        <button
          type="button"
          title={`${label} değerini artır`}
          aria-label={`${label} değerini artır`}
          onClick={() => adjustValue(1)}
          disabled={disabled || isAtMaximum}
        >
          <Plus size={15} aria-hidden="true" />
        </button>
      </div>
    </div>
  );
}

function SelectField({
  id,
  label,
  value,
  onChange,
  options,
  required = false,
  disabled = false
}) {
  const [isOpen, setIsOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);
  const rootRef = useRef(null);
  const triggerRef = useRef(null);
  const selectedIndex = options.findIndex(
    ([optionValue]) => optionValue === value
  );
  const selectedLabel =
    selectedIndex >= 0 ? options[selectedIndex][1] : "Seçin";

  useEffect(() => {
    const closeOnOutsideClick = (event) => {
      if (!rootRef.current?.contains(event.target)) {
        setIsOpen(false);
      }
    };

    document.addEventListener("pointerdown", closeOnOutsideClick);
    return () =>
      document.removeEventListener("pointerdown", closeOnOutsideClick);
  }, []);

  const openMenu = () => {
    if (disabled) {
      return;
    }

    setActiveIndex(selectedIndex >= 0 ? selectedIndex : 0);
    setIsOpen(true);
  };

  const selectOption = (index) => {
    onChange(options[index][0]);
    setActiveIndex(index);
    setIsOpen(false);
    triggerRef.current?.focus();
  };

  const handleKeyDown = (event) => {
    if (disabled) {
      return;
    }

    if (event.key === "Escape") {
      setIsOpen(false);
      return;
    }

    if (event.key === "Tab") {
      setIsOpen(false);
      return;
    }

    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();

      if (!isOpen) {
        openMenu();
        return;
      }

      const direction = event.key === "ArrowDown" ? 1 : -1;
      setActiveIndex(
        (current) => (current + direction + options.length) % options.length
      );
      return;
    }

    if (event.key === "Home" || event.key === "End") {
      event.preventDefault();
      setActiveIndex(event.key === "Home" ? 0 : options.length - 1);
      setIsOpen(true);
      return;
    }

    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();

      if (isOpen) {
        selectOption(activeIndex);
      } else {
        openMenu();
      }
    }
  };

  return (
    <div className="manual-field">
      <span id={`${id}-label`}>{label}</span>
      <div className="manual-select" ref={rootRef}>
        <button
          className={`manual-select__trigger${value ? " has-value" : ""}`}
          ref={triggerRef}
          type="button"
          aria-controls={`${id}-options`}
          aria-activedescendant={
            isOpen ? `${id}-option-${activeIndex}` : undefined
          }
          aria-expanded={isOpen}
          aria-haspopup="listbox"
          aria-labelledby={`${id}-label`}
          aria-required={required}
          id={id}
          role="combobox"
          onClick={() => (isOpen ? setIsOpen(false) : openMenu())}
          onKeyDown={handleKeyDown}
          disabled={disabled}
        >
          <span>{selectedLabel}</span>
          <ChevronDown size={17} aria-hidden="true" />
        </button>

        <AnimatePresence>
          {isOpen ? (
            <motion.ul
              className="manual-select__menu"
              id={`${id}-options`}
              role="listbox"
              aria-labelledby={`${id}-label`}
              initial={{ opacity: 0, y: -6, scale: 0.98 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: -4, scale: 0.98 }}
              transition={{ duration: 0.16, ease: [0.22, 1, 0.36, 1] }}
            >
              {options.map(([optionValue, optionLabel], index) => (
                <li
                  className={`${index === activeIndex ? "is-active" : ""}${
                    optionValue === value ? " is-selected" : ""
                  }`}
                  id={`${id}-option-${index}`}
                  role="option"
                  aria-selected={optionValue === value}
                  key={optionValue}
                  onClick={() => selectOption(index)}
                  onMouseEnter={() => setActiveIndex(index)}
                >
                  <span>{optionLabel}</span>
                  {optionValue === value ? (
                    <Check size={16} aria-hidden="true" />
                  ) : null}
                </li>
              ))}
            </motion.ul>
          ) : null}
        </AnimatePresence>
      </div>
    </div>
  );
}

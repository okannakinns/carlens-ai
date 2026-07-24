import { ScanSearch } from "lucide-react";

export default function BrandMark({ inverse = false }) {
  return (
    <a className={`brand ${inverse ? "brand--inverse" : ""}`} href="#top">
      <span className="brand__mark" aria-hidden="true">
        <ScanSearch size={23} strokeWidth={2.2} />
      </span>
      <span>Carlens AI</span>
    </a>
  );
}

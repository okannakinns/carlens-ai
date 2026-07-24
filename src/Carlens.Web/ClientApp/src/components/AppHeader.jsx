import { Clock3 } from "lucide-react";
import BrandMark from "./BrandMark";

export default function AppHeader() {
  const showHistory = () => {
    document
      .getElementById("recent-analyses")
      ?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  return (
    <header className="site-header">
      <BrandMark inverse />
      <button
        className="icon-text-button icon-text-button--glass"
        type="button"
        onClick={showHistory}
      >
        <Clock3 size={18} aria-hidden="true" />
        <span>Son analizler</span>
      </button>
    </header>
  );
}

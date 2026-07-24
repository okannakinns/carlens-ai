import BrandMark from "./BrandMark";

export default function AppFooter() {
  return (
    <footer className="site-footer">
      <div className="section-shell site-footer__inner">
        <BrandMark />
        <p>
          Carlens AI raporu ön değerlendirmedir. Satın alma kararı öncesinde
          bağımsız ekspertiz yaptırın.
        </p>
        <span>© {new Date().getFullYear()}</span>
      </div>
    </footer>
  );
}

import { Button } from "@/components/ui/button";

export function CtaFooter() {
  return (
    <footer id="lien-he" aria-label="CTA cuối trang và footer" className="bg-surface-dark py-16 text-white">
      <div className="mx-auto max-w-6xl px-6 text-center">
        <h2 className="font-heading text-3xl">
          Sẵn sàng bắt đầu cùng Furina?
        </h2>
        <div className="mt-6 flex flex-wrap justify-center gap-4">
          <Button className="bg-accent text-accent-foreground" size="lg">
            Đặt lịch ngay
          </Button>
          <Button
            variant="outline"
            size="lg"
            className="border-white text-white hover:bg-white/10"
          >
            Dành cho phòng khám
          </Button>
        </div>
        <div className="mt-10 flex flex-wrap justify-center gap-6 text-sm text-[#F5F5F4]">
          <span>© 2026 Furina</span>
          <a href="mailto:hello@furina.app">hello@furina.app</a>
          <a href="#">Facebook</a>
          <a href="#">Instagram</a>
        </div>
      </div>
    </footer>
  );
}

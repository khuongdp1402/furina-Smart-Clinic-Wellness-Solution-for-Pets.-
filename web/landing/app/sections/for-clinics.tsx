import { Button } from "@/components/ui/button";

const REASONS = [
  "Quản lý tiếp nhận, kho dược phẩm, phòng nội trú trong một hệ thống",
  "Báo cáo tài chính trực quan theo thời gian thực",
  "Triển khai nhanh — không cần đội IT riêng",
];

export function ForClinics() {
  return (
    <section
      id="phong-kham"
      aria-label="Dành cho phòng khám"
      className="mx-auto max-w-6xl px-6 py-16"
    >
      <div className="grid items-center gap-10 md:grid-cols-2">
        <div>
          <h2 className="font-heading text-3xl text-ink">
            Dành cho phòng khám
          </h2>
          <ul className="mt-6 space-y-3">
            {REASONS.map((reason) => (
              <li key={reason} className="flex gap-3 text-ink">
                <span className="text-primary">✓</span>
                {reason}
              </li>
            ))}
          </ul>
          <Button className="mt-8 bg-primary text-primary-foreground" size="lg">
            Bắt đầu miễn phí
          </Button>
        </div>
        <div
          role="img"
          aria-label="Xem trước demo Furina cho phòng khám (video ngắn sắp có)"
          className="flex h-64 items-center justify-center rounded-xl border border-dashed border-ink/20 bg-white/50 text-ink/50"
        >
          Demo nhanh / video ngắn — sắp có
        </div>
      </div>
    </section>
  );
}

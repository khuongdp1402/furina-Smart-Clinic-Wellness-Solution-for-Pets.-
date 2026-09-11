import { Button } from "@/components/ui/button";

export function Hero() {
  return (
    <section
      aria-label="Hero"
      className="mx-auto flex max-w-6xl flex-col items-center gap-8 px-6 py-20 text-center"
    >
      <h1 className="font-heading text-4xl text-ink md:text-6xl">
        Furina — Chăm sóc thú cưng, trọn vẹn yêu thương
      </h1>
      <p className="max-w-2xl text-lg text-ink/80">
        Hệ sinh thái số cho phòng khám thú y: tiếp nhận, y bạ điện tử, lịch
        hẹn và cộng đồng người yêu thú cưng — tất cả trong một nền tảng.
      </p>
      <div className="flex flex-wrap justify-center gap-4">
        <Button className="bg-accent text-accent-foreground" size="lg">
          Đặt lịch ngay
        </Button>
        <Button variant="outline" size="lg">
          Dành cho phòng khám: Bắt đầu miễn phí
        </Button>
      </div>
    </section>
  );
}

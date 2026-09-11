export function ClinicMap() {
  return (
    <section aria-label="Bản đồ tìm phòng khám gần nhất" className="mx-auto max-w-6xl px-6 py-16">
      <h2 className="font-heading text-3xl text-ink">
        Tìm phòng khám gần nhất
      </h2>
      <p className="mt-2 text-ink/80">
        Bản đồ tương tác — sắp ra mắt khi dịch vụ định vị phòng khám hoàn tất.
      </p>
      <div
        role="img"
        aria-label="Xem trước bản đồ phòng khám (chưa có dữ liệu thật)"
        className="mt-8 flex h-72 items-center justify-center rounded-xl border border-dashed border-ink/20 bg-white/50 text-ink/50"
      >
        Xem trước bản đồ (PostGIS) — đang phát triển
      </div>
    </section>
  );
}

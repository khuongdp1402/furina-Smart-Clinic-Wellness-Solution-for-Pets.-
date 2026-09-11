const PAIRS = [
  {
    problem: "Chủ nuôi khó tìm phòng khám uy tín gần nhà, phải hỏi khắp nơi.",
    solution: "Furina có bản đồ tìm phòng khám gần nhất, đánh giá minh bạch.",
  },
  {
    problem: "Hồ sơ khám bệnh của thú cưng rải rác, mất khi đổi phòng khám.",
    solution: "Y bạ điện tử (SOAP note) lưu trọn vẹn, theo thú cưng suốt đời.",
  },
  {
    problem: "Phòng khám truyền thống đặt lịch qua điện thoại, dễ trùng giờ.",
    solution: "Đặt lịch đa kênh, chống trùng lịch tự động, nhắc lịch chủ động.",
  },
  {
    problem: "Không có cộng đồng để chủ nuôi chia sẻ kinh nghiệm chăm sóc.",
    solution: "Furina Feed — cộng đồng người yêu thú cưng ngay trong app.",
  },
];

export function ProblemSolution() {
  return (
    <section
      aria-label="Vấn đề và giải pháp"
      className="mx-auto max-w-6xl px-6 py-16"
    >
      <h2 className="font-heading text-3xl text-ink">
        Vấn đề bạn gặp, Furina đã giải quyết
      </h2>
      <div className="mt-10 grid gap-6 md:grid-cols-2">
        {PAIRS.map((pair) => (
          <div
            key={pair.problem}
            className="rounded-xl border border-black/5 bg-white/50 p-6 shadow-sm"
          >
            <p className="text-sm font-medium text-danger">{pair.problem}</p>
            <p className="mt-2 text-ink">{pair.solution}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const FEATURES = [
  {
    title: "EMR số hoá (SOAP note)",
    description: "Y bạ điện tử chuẩn SOAP, tra cứu lịch sử khám tức thì.",
  },
  {
    title: "Đặt lịch đa kênh",
    description: "Web, app, chatbot — chống trùng lịch, nhắc lịch tự động.",
  },
  {
    title: "Furina Feed cộng đồng",
    description: "Chia sẻ kinh nghiệm chăm sóc thú cưng cùng cộng đồng.",
  },
  {
    title: "Chatbot 24/7",
    description: "Trả lời câu hỏi chăm sóc cơ bản mọi lúc, không cần chờ.",
  },
];

export function Features() {
  return (
    <section id="tinh-nang" aria-label="Tính năng nổi bật" className="bg-white/40 py-16">
      <div className="mx-auto max-w-6xl px-6">
        <h2 className="font-heading text-3xl text-ink">Tính năng nổi bật</h2>
        <div className="mt-10 grid gap-6 md:grid-cols-2 lg:grid-cols-4">
          {FEATURES.map((feature) => (
            <Card key={feature.title} className="rounded-xl shadow-sm">
              <CardHeader>
                <CardTitle className="font-heading text-lg text-ink">
                  {feature.title}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-ink/80">{feature.description}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </section>
  );
}

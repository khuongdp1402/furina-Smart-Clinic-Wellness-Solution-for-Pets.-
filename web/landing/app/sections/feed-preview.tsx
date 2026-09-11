import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";

const SAMPLE_POSTS = [
  {
    author: "Chị Lan, chủ mèo Miu",
    excerpt: "Miu vừa tiêm phòng xong, bé rất ngoan! Cảm ơn phòng khám ạ.",
  },
  {
    author: "Anh Khoa, chủ chó Bún",
    excerpt: "Chia sẻ kinh nghiệm chăm sóc chó con mới nhận nuôi...",
  },
  {
    author: "Phòng khám Thú Cưng Vui Vẻ",
    excerpt: "Lịch tiêm phòng dại miễn phí cuối tuần này!",
  },
];

export function FeedPreview() {
  return (
    <section aria-label="Furina Feed" className="bg-white/40 py-16">
      <div className="mx-auto max-w-6xl px-6">
        <div className="flex items-center gap-3">
          <h2 className="font-heading text-3xl text-ink">Furina Feed</h2>
          <Badge className="bg-highlight text-ink">Xem trước</Badge>
        </div>
        <p className="mt-2 text-ink/80">
          Bài viết mẫu — cộng đồng thật sẽ hoạt động khi Furina Feed ra mắt.
        </p>
        <div className="mt-8 grid gap-6 md:grid-cols-3">
          {SAMPLE_POSTS.map((post) => (
            <Card key={post.author} className="rounded-xl shadow-sm">
              <CardHeader>
                <CardTitle className="text-sm text-ink/70">
                  {post.author}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-ink">{post.excerpt}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </section>
  );
}

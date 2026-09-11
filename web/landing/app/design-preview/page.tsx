import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";

export default function DesignPreviewPage() {
  return (
    <main className="mx-auto max-w-3xl space-y-8 p-8">
      <h1 className="font-heading text-3xl text-ink">Design token preview</h1>
      <p className="text-ink">
        Trang nội bộ để xác minh design token (US-38 AC-1) — không phải
        trang login thật (Tenant Admin Portal chưa tồn tại tới Sprint 5).
      </p>

      <section className="space-y-3">
        <h2 className="font-heading text-xl text-ink">Buttons</h2>
        <div className="flex flex-wrap gap-3">
          <Button className="bg-primary text-primary-foreground">
            Primary
          </Button>
          <Button className="bg-accent text-accent-foreground">Accent CTA</Button>
          <Button variant="outline">Outline</Button>
          <Button className="bg-danger text-white">Danger</Button>
          <Button className="bg-success text-white">Success</Button>
        </div>
      </section>

      <section className="space-y-3">
        <h2 className="font-heading text-xl text-ink">Badges</h2>
        <div className="flex flex-wrap gap-3">
          <Badge className="bg-highlight text-ink">Highlight</Badge>
          <Badge className="bg-primary text-primary-foreground">Primary</Badge>
        </div>
      </section>

      <Card className="rounded-xl shadow-sm">
        <CardHeader>
          <CardTitle className="font-heading text-ink">Sample card</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <p className="text-ink">
            Card dùng bo góc rounded-xl và shadow nhẹ theo token.
          </p>
          <Input placeholder="Nhập email..." />
        </CardContent>
      </Card>

      <section className="rounded-xl bg-surface-dark p-6">
        <h2 className="font-heading text-xl text-white">Dark surface</h2>
        <p className="mt-2 text-[#F5F5F4]">
          Nền tối dùng cho 2 cổng admin (ca đêm) — kiểm tra contrast ở
          contrast-check.mjs.
        </p>
      </section>
    </main>
  );
}

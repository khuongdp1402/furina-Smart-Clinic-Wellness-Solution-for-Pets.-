"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";

export function SiteNav() {
  return (
    <header className="sticky top-0 z-10 border-b border-black/5 bg-surface/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <Link href="/" className="font-heading text-xl text-ink">
          Furina
        </Link>
        <nav className="hidden gap-6 text-sm text-ink md:flex">
          <a href="#tinh-nang">Tính năng</a>
          <a href="#phong-kham">Dành cho phòng khám</a>
          <a href="#lien-he">Liên hệ</a>
        </nav>
        <Button className="bg-primary text-primary-foreground">
          Đặt lịch ngay
        </Button>
      </div>
    </header>
  );
}

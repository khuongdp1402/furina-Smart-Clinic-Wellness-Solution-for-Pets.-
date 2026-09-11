import type { Metadata } from "next";
import { Fraunces, Inter } from "next/font/google";
import "./globals.css";

// Variable names match tokens.css's raw `--furina-*` tokens, NOT the
// Tailwind theme keys (--font-heading/--font-ui) those tokens feed into
// via theme.css's `@theme inline` — see tokens.css's top comment for why
// that distinction is load-bearing, not just naming taste.
const fraunces = Fraunces({
  subsets: ["latin"],
  variable: "--furina-font-heading",
  display: "swap",
});

const inter = Inter({
  subsets: ["latin"],
  variable: "--furina-font-ui",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Furina — Chăm sóc thú cưng, trọn vẹn yêu thương",
  description:
    "Furina là hệ sinh thái số hỗ trợ chuyển đổi số cho các trung tâm y tế và trị liệu thú cưng.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="vi" className={`${fraunces.variable} ${inter.variable}`}>
      <body className="font-ui">{children}</body>
    </html>
  );
}

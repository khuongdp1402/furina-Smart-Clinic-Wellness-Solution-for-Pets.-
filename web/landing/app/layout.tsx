import type { Metadata } from "next";
import { Fraunces, Inter } from "next/font/google";
import "./globals.css";

const fraunces = Fraunces({
  subsets: ["latin"],
  variable: "--font-heading",
  display: "swap",
});

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-ui",
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

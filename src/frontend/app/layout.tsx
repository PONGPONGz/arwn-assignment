import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Clinic POS",
  description: "Clinic POS Platform v1",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={`${geistSans.variable} ${geistMono.variable}`}>
        <nav style={{ padding: "1rem", borderBottom: "1px solid #eee", display: "flex", gap: "1rem", alignItems: "center" }}>
          <strong>Clinic POS</strong>
          <a href="/patients">Patients</a>
        </nav>
        <main style={{ padding: "1rem", maxWidth: "960px", margin: "0 auto" }}>
          {children}
        </main>
      </body>
    </html>
  );
}

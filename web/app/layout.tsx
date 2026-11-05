import {LanguageProvider} from "./language";
import type { Metadata } from "next";
import "./easymount.css";
import "./midnight-jade.css";
import "./patch-068.css";
import "./patch-0610.css";
import "./patch-0611.css";
import "./release-070.css";

export const metadata: Metadata = {
  metadataBase: new URL("https://easymount-cloud-drive.trtqt1203.chatgpt.site"),
  title: "EasyMount — Nhiều cloud. Một ổ đĩa.",
  description: "Gắn cloud vào File Explorer, gộp nhiều tài khoản theo thứ tự ưu tiên. Miễn phí, mã nguồn mở cho Windows.",
  icons: {
    icon: "/logo.svg",
    shortcut: "/logo.svg",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="antialiased"><LanguageProvider>{children}</LanguageProvider></body>
    </html>
  );
}

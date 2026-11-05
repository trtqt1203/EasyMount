"use client";
import {useLanguage} from "./language";
import { useCallback, useEffect, useRef, useState } from "react";
const childNames = ["travis1", "travis2", "travis3", "travis4", "travis5"];
const used = [425.4, 73.7, 1.2, 0, 4.3];
const totals = [5120, 400, 5120, 5120, 5120];
const totalText = (n: number) => n >= 1024 ? `${(n / 1024).toFixed(1).replace(/\.0$/, " ").trim()} TiB` : `${n} GiB`;
const types = ["Google Drive", "OneDrive", "Google Drive", "Dropbox", "Google Drive"];
const letters = ["R:", "S:", "T:", "U:", "V:"];
const colors = ["#4285F4", "#0078D4", "#4285F4", "#0061FF", "#4285F4"];
const usageColor = (percent: number) => percent > 90 ? "#F87171" : percent >= 70 ? "#F59E0B" : "#00E5B4";
export default function AppPreview() {
const {t,lang,setLang,theme,setTheme}=useLanguage();
const settings=useRef<HTMLDialogElement>(null);
    const [loading, setLoading] = useState(false), [flash, setFlash] = useState(false), [checked, setChecked] = useState(false);
    const about = useRef<HTMLDialogElement>(null), [updateStatus, setUpdateStatus] = useState(""), [updateBusy, setUpdateBusy] = useState(false), [selected, setSelected] = useState<number | null>(null);
    const checkUpdate = async () => { if (updateBusy)
        return; setUpdateBusy(true); setUpdateStatus(t("\u0110ang ki\u1EC3m tra\u2026")); try {
        const r = await fetch("/api/releases");
        if (!r.ok)
            throw new Error();
        const data = await r.json() as {status?:string;version?:string};
        setUpdateStatus(data.status === "published" ? `${t("Bản công khai:")} v${data.version}` : t("Ch\u01B0a c\u00F3 b\u1EA3n ph\u00E1t h\u00E0nh c\u00F4ng khai tr\u00EAn GitHub."));
    }
    catch {
        setUpdateStatus(t("Kh\u00F4ng ki\u1EC3m tra \u0111\u01B0\u1EE3c. Vui l\u00F2ng th\u1EED l\u1EA1i."));
    }
    finally {
        setUpdateBusy(false);
    } };
    const busy = useRef(false), timer = useRef<ReturnType<typeof setTimeout> | null>(null), flashTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
    const refreshAll = useCallback(() => { if (busy.current)
        return; busy.current = true; setLoading(true); timer.current = setTimeout(() => { setChecked(true); setLoading(false); busy.current = false; }, 1500); }, []);
    useEffect(() => { const onKey = (e: KeyboardEvent) => { if (!e.ctrlKey || e.key.toLowerCase() !== "r")
        return; e.preventDefault(); setFlash(true); if (flashTimer.current)
        clearTimeout(flashTimer.current); flashTimer.current = setTimeout(() => setFlash(false), 300); refreshAll(); }; document.addEventListener("keydown", onKey); return () => { document.removeEventListener("keydown", onKey); if (timer.current)
        clearTimeout(timer.current); if (flashTimer.current)
        clearTimeout(flashTimer.current); }; }, [refreshAll]);
    const total = totals.reduce((a, b) => a + b, 0), usedTotal = used.reduce((a, b) => a + b, 0), percent = usedTotal / total * 100;
    return <div className="app-preview jade-preview patched-preview" aria-label={t("Minh h\u1ECDa EasyMount, d\u1EEF li\u1EC7u v\u00ED d\u1EE5")}>
  <div className="window-chrome"><span>EasyMount</span><span data-i18n="Windows x64 \\u00B7 v0.7.0">{t("Windows x64 \u00B7 v0.7.0")}</span></div>
  <div className="jade-preview-layout"><aside className="jade-preview-sidebar">
   <div className="mini-brand-line"><img src="/logo.svg" width="25" height="25" alt=""/><strong>EasyMount</strong></div>
   <span className="mini-nav-section" data-i18n="WORKSPACE HI\\u1EC6N T\\u1EA0I">{t("WORKSPACE HI\u1EC6N T\u1EA0I")}</span><div className="mini-workspace" data-i18n="\\u25CF T\\u1EA5t c\\u1EA3 \\u1ED5 \\u0111\\u0129a \\u25BE">{t("Chưa có workspace")}</div><div className="mini-divider"/>
   <span className="mini-nav-item active" data-i18n="\\u1ED4 \\u0111\\u0129a">{t("\u1ED4 \u0111\u0129a")}<b>6</b></span><span className="mini-nav-item" data-i18n="T\\u00E0i kho\\u1EA3n cloud \\u00B7">{t("T\u00E0i kho\u1EA3n cloud \u00B7")}</span><span className="mini-nav-item" data-i18n="Nh\\u1EADt k\\u00FD ho\\u1EA1t \\u0111\\u1ED9ng">{t("Nh\u1EADt k\u00FD ho\u1EA1t \u0111\u1ED9ng")}</span><div className="mini-divider"/>
   <span className="mini-nav-section">SYSTEM</span><button type="button" className="mini-nav-item mini-settings-button" onClick={()=>settings.current?.showModal()}>{t("Cài đặt")}</button><span className="mini-nav-item" data-i18n="H\\u01B0\\u1EDBng d\\u1EABn">{t("H\u01B0\u1EDBng d\u1EABn")}</span><button className="mini-nav-item mini-about-button" onClick={() => about.current?.showModal()}>{t("Gi\u1EDBi thi\u1EC7u")}</button>
   <div className="mini-sidebar-footer sidebar-about-info"><div className="sidebar-about-brand"><img src="/logo.svg" width="24" height="24" alt=""/><strong>EasyMount</strong></div><div className="sidebar-about-tagline">YOUR CLOUD. YOUR DRIVE.</div><div className="mini-divider"/><div className="sidebar-copyright" data-i18n="\\u00A9 2026 TravisTRTQT">{t("\u00A9 2026 TravisTRTQT")}</div><div className="sidebar-rights">All rights reserved.</div><div className="mini-divider"/><div className="sidebar-about-version" data-i18n="Windows x64 \\u00B7 v0.7.0">{t("Windows x64 \u00B7 v0.7.0")}</div></div>
  </aside><div className="jade-preview-content">
   <div className="mini-topbar"><span data-i18n="Workspace \\u203A T\\u1EA5t c\\u1EA3 \\u1ED5 \\u0111\\u0129a">{t("Workspace \u203A T\u1EA5t c\u1EA3 \u1ED5 \u0111\u0129a")}</span><div className="preview-refresh-wrap"><button type="button" className={"preview-refresh " + (flash ? "hotkey-flash" : "")} onClick={refreshAll} disabled={loading} aria-busy={loading}>{loading && <i className="refresh-spinner"/>}<span>{loading ? t("\u0110ang l\u00E0m m\u1EDBi...") : t("L\u00E0m m\u1EDBi")}</span></button><span role="tooltip" className="preview-tip refresh-tip" data-i18n="L\\u00E0m m\\u1EDBi k\\u1EBFt n\\u1ED1i t\\u1EA5t c\\u1EA3 \\u1ED5 \\u0111\\u0129a (Ctrl+R)">{t("L\u00E0m m\u1EDBi k\u1EBFt n\u1ED1i t\u1EA5t c\u1EA3 \u1ED5 \u0111\u0129a (Ctrl+R)")}</span></div><a className="preview-add" href="/guide#providers" data-i18n="btn_add">{t("+ Thêm ổ đĩa")}</a></div>
   <div className="mini-stats"><div><strong>6</strong><small data-i18n="\\u1ED4 \\u0110\\u0128A">{t("\u1ED4 \u0110\u0128A")}</small></div><div><strong>4</strong><small>ONLINE</small></div><div><strong>5</strong><small>ACCOUNTS</small></div><div className="workspace-usage"><strong>{usedTotal.toFixed(1)} GiB</strong><span>/ {totalText(total)} · {percent.toFixed(1)}%</span><div className="sample-bar"><i style={{ width: percent + "%", background: usageColor(percent) }}/></div><small data-i18n="\\u0110\\u00C3 S\\u1EEC D\\u1EE4NG \\u00B7 WORKSPACE HI\\u1EC6N T\\u1EA0I">{t("\u0110\u00C3 S\u1EEC D\u1EE4NG \u00B7 WORKSPACE HI\u1EC6N T\u1EA0I")}</small></div></div>
   <div className="mini-section sample-chips">{[t("T\u1EA5t c\u1EA3"), t("\u0110\u00E3 k\u1EBFt n\u1ED1i"), t("Ch\u01B0a k\u1EBFt n\u1ED1i"), "Google Drive", "OneDrive", "Dropbox"].map((label, i) => <span key={label} className={i === 0 ? "mini-filter-active" : ""}>{label}</span>)}</div>
   <div className="sample-heading"><span data-i18n="T\\u00CAN \\u1ED4 \\u0110\\u0128A">{t("T\u00CAN \u1ED4 \u0110\u0128A")}</span><span data-i18n="DUNG L\\u01AF\\u1EE2NG">{t("DUNG L\u01AF\u1EE2NG")}</span><span data-i18n="C\\u1EACP NH\\u1EACT">{t("C\u1EACP NH\u1EACT")}</span><span>CLOUD</span></div>
   {childNames.map((name, i) => <div key={name}><div className={"sample-row " + (selected === i ? "selected-drive" : "")} data-drive={name} tabIndex={0} role="button" aria-label={t("Chi ti\u1EBFt ") + name} onClick={() => setSelected(i)} onKeyDown={e => { if (e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        setSelected(i);
    } }}><span className="mini-avatar">{letters[i]}<i style={{ background: i === 4 ? "#F87171" : "#00E5B4" }}/></span><div className="mini-row-name"><strong>{name}</strong><small>drive{i + 1}@example.com</small></div><div className="sample-usage"><div className="sample-bar"><i style={{ width: used[i] / totals[i] * 100 + "%", background: usageColor(used[i] / totals[i] * 100) }}/></div>{used[i]} GiB / {totalText(totals[i])}</div><span className="sample-time" data-i18n-fn="time_not_checked">{checked ? t("V\u1EEBa ki\u1EC3m tra") : t("Ch\u01B0a ki\u1EC3m tra")}</span><span className="sample-cloud" style={{ color: colors[i] }} title={types[i]}>{types[i] === "Google Drive" ? "G" : types[i] === "OneDrive" ? "O" : "D"}</span><span className="mini-row-action">{i === 4 ? t("X\u00E1c th\u1EF1c l\u1EA1i") : t("M\u1EDF")}</span></div></div>)}
   <div className={"sample-row pooled-row " + (selected === 5 ? "selected-drive" : "")} tabIndex={0} data-drive="Drive chung" role="button" aria-label={t("Chi ti\u1EBFt Drive chung")} onClick={() => setSelected(5)} onKeyDown={e => { if (e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        setSelected(5);
    } }}><span className="mini-avatar">W:</span><div className="mini-row-name"><strong>Drive chung</strong><small>{childNames.join(" · ")}</small></div><div className="sample-usage"><div className="sample-bar"><i style={{ width: percent + "%" }}/></div>{usedTotal.toFixed(1)} GiB / {totalText(total)}</div><span className="sample-time" data-i18n-fn="time_not_checked">{checked ? t("V\u1EEBa ki\u1EC3m tra") : t("Ch\u01B0a ki\u1EC3m tra")}</span><span className="sample-cloud">P</span><span className="mini-row-action" data-i18n="K\\u1EBFt n\\u1ED1i">{t("K\u1EBFt n\u1ED1i")}</span><div role="tooltip" className="preview-tip pool-tip">{childNames.map(name => <div key={name}>{name}</div>)}</div></div>
   {selected !== null && <aside className="preview-drive-detail" aria-label={t("Chi ti\u1EBFt \u1ED5 \u0111\u0129a minh h\u1ECDa")}><button className="detail-close" aria-label={t("\u0110\u00F3ng chi ti\u1EBFt \u1ED5 \u0111\u0129a")} onClick={() => setSelected(null)}>{t("\u00D7")}</button><span data-i18n="CHI TI\\u1EBET \\u1ED4 \\u0110\\u0128A">{t("CHI TI\u1EBET \u1ED4 \u0110\u0128A")}</span><h3>{selected === 5 ? "Drive chung" : childNames[selected]}</h3><p>{selected === 5 ? childNames.join(" · ") : `drive${selected + 1}@example.com`}</p><strong>{(selected === 5 ? usedTotal : used[selected]).toFixed(1)} GiB / {totalText(selected === 5 ? total : totals[selected])}</strong><div className="sample-bar"><i style={{ width: (selected === 5 ? percent : used[selected] / totals[selected] * 100) + "%", background: usageColor(selected === 5 ? percent : used[selected] / totals[selected] * 100) }}/></div><p>{(selected === 5 ? percent : used[selected] / totals[selected] * 100).toFixed(1)}{t("% \u0111\u00E3 s\u1EED d\u1EE5ng")}</p><small data-i18n="D\\u1EEF li\\u1EC7u v\\u00ED d\\u1EE5">{t("D\u1EEF li\u1EC7u v\u00ED d\u1EE5")}</small></aside>}
   <div className="mini-ready"><span role="status" aria-live="polite"><span className="status-dot"/>{loading ? t("\u0110ang l\u00E0m m\u1EDBi t\u1EA5t c\u1EA3 \u1ED5 \u0111\u0129a minh h\u1ECDa\u2026") : checked ? t("\u0110\u00E3 l\u00E0m m\u1EDBi t\u1EA5t c\u1EA3 6 \u1ED5 \u00B7 D\u1EEF li\u1EC7u v\u00ED d\u1EE5") : t("EasyMount \u00B7 4 / 6 online \u00B7 D\u1EEF li\u1EC7u v\u00ED d\u1EE5")}</span><span className="preview-status-version" data-i18n="Windows x64 \\u00B7 v0.7.0 \\u00B7 \\u00A9 TravisTRTQT">{t("Windows x64 \u00B7 v0.7.0 \u00B7 \u00A9 TravisTRTQT")}</span></div>
  </div></div><div className="preview-caption" data-i18n="Giao di\\u1EC7n minh h\\u1ECDa \\u00B7 D\\u1EEF li\\u1EC7u v\\u00ED d\\u1EE5 \\u00B7 Ctrl+R th\\u1EED l\\u00E0m m\\u1EDBi to\\u00E0n b\\u1ED9 danh s\\u00E1ch">{t("Giao di\u1EC7n minh h\u1ECDa \u00B7 D\u1EEF li\u1EC7u v\u00ED d\u1EE5 \u00B7 Ctrl+R th\u1EED l\u00E0m m\u1EDBi to\u00E0n b\u1ED9 danh s\u00E1ch")}</div>
 <dialog ref={settings} className="about-dialog preview-settings" aria-label={t("Cài đặt")}><h2>{t("Cài đặt")}</h2><label>{t("Ngôn ngữ")}<select value={lang} onChange={e=>setLang(e.target.value as "vi"|"en")}><option value="en">English</option><option value="vi">Tiếng Việt</option></select></label><label>{t("Giao diện")}<select value={theme} onChange={e=>setTheme(e.target.value as "light"|"dark")}><option value="dark">{t("Tối")}</option><option value="light">{t("Sáng")}</option></select></label><p>{t("Đây là giao diện minh họa. Tài khoản cloud được quản lý trong ứng dụng Windows.")}</p><button type="button" onClick={()=>settings.current?.close()}>{t("Đóng")}</button></dialog>
 <dialog ref={about} className="about-dialog" aria-label={t("Gi\u1EDBi thi\u1EC7u EasyMount")}><img src="/logo.svg" width="48" height="48" alt=""/><h2>EasyMount</h2><span className="about-version">v0.7.0</span><p className="about-tagline">YOUR CLOUD. YOUR DRIVE.</p><hr /><strong data-i18n="\\u00A9 2026 TravisTRTQT">{t("\u00A9 2026 TravisTRTQT")}</strong><span className="about-rights">All rights reserved.</span><p className="about-description" role="status">{(updateStatus&&t(updateStatus)) || <>{t("Ph\u1EA7n m\u1EC1m qu\u1EA3n l\u00FD \u1ED5 \u0111\u0129a \u0111\u00E1m m\u00E2y d\u00E0nh cho")}<br />{t("designer v\u00E0 ng\u01B0\u1EDDi d\u00F9ng chuy\u00EAn nghi\u1EC7p.")}</>}</p><hr /><div className="about-actions"><button onClick={checkUpdate} disabled={updateBusy}>{updateBusy ? t("\u0110ang ki\u1EC3m tra\u2026") : t("Ki\u1EC3m tra c\u1EADp nh\u1EADt")}</button><button onClick={() => about.current?.close()}>{t("\u0110\u00F3ng")}</button></div></dialog>
 </div>;
}

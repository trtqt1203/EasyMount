"use client";
import {createContext,useContext,useEffect,useState,type ReactNode} from "react";
import english from "./translations.json";
type Language="vi"|"en";
const dictionary=english as Record<string,string>;
export const TRANSLATIONS={vi:{},en:dictionary};
const Context=createContext({lang:"en" as Language,theme:"dark" as "dark"|"light",setTheme:(_theme:"dark"|"light")=>{},setLang:(_lang:Language)=>{},t:(text:string)=>text});
export function LanguageProvider({children}:{children:ReactNode}){
 const [lang,setLang]=useState<Language>("en");
 const [theme,setTheme]=useState<"dark"|"light">("dark");
 const [ready,setReady]=useState(false);
 useEffect(()=>{try{const l=localStorage.getItem("easymount-language"),th=localStorage.getItem("easymount-theme");if(l==="en"||l==="vi")setLang(l);if(th==="light"||th==="dark")setTheme(th)}catch{}setReady(true)},[]);
 useEffect(()=>{document.documentElement.dataset.theme=theme;if(ready){try{localStorage.setItem("easymount-language",lang);localStorage.setItem("easymount-theme",theme)}catch{}}},[lang,theme,ready]);
 useEffect(()=>{document.documentElement.lang=lang},[lang]);
 const t=(text:string)=>lang==="en"?(dictionary[text]??text):text;
 return <Context.Provider value={{lang,setLang,t,theme,setTheme}}>{children}</Context.Provider>;
}
export const useLanguage=()=>useContext(Context);
export function LanguageToggle(){const {lang,setLang}=useLanguage();return <div className="language-toggle" role="group" aria-label={lang==="vi"?"Ngôn ngữ":"Language"}>{(["vi","en"] as const).map(value=><button key={value} type="button" lang={value} aria-pressed={lang===value} onClick={()=>setLang(value)}>{value.toUpperCase()}</button>)}</div>}

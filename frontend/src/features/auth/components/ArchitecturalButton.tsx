/**
 * ArchitecturalButton — Primary luxury architectural glass button component.
 *
 * Light Mode: Polished architectural glass with gentle top highlight, subtle reflection, gentle bottom shadow.
 * Dark Mode: Reflective dark glass façade of a modern glass skyscraper. Inside the button is a subtle grid
 * suggesting building windows with very few windows emitting a warm golden light (#FFD98A).
 */

import React from "react";
import { RefreshCw } from "lucide-react";

interface ArchitecturalButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  children: React.ReactNode;
  isLoading?: boolean;
  loadingText?: string;
  isDark?: boolean;
}

export function ArchitecturalButton({
  children,
  isLoading = false,
  loadingText,
  isDark = false,
  disabled,
  className = "",
  type = "submit",
  ...props
}: ArchitecturalButtonProps) {
  return (
    <button
      type={type}
      disabled={disabled || isLoading}
      className={`group relative w-full h-10 rounded-lg text-[13.5px] font-medium transition-all duration-300 flex items-center justify-center gap-2 cursor-pointer overflow-hidden outline-none select-none disabled:opacity-60 ${
        isDark
          ? "bg-[#161B22] text-[#F0F3F6] border border-white/10 hover:border-white/25 shadow-lg shadow-black/40 hover:shadow-black/60 focus:ring-2 focus:ring-[#656D4A]/40"
          : "bg-[#414833] hover:bg-[#333D29] text-white shadow-xs focus:ring-2 focus:ring-[#A4AC86]/40"
      } ${className}`}
      {...props}
    >
      {/* Dark Mode: Architectural Skyscraper Windows Grid Overlay */}
      {isDark && (
        <div className="absolute inset-0 pointer-events-none opacity-30 group-hover:opacity-45 transition-opacity duration-300">
          {/* Subtle facade grid lines */}
          <div
            className="w-full h-full"
            style={{
              backgroundImage:
                "linear-gradient(to right, rgba(255, 255, 255, 0.05) 1px, transparent 1px), linear-gradient(to bottom, rgba(255, 255, 255, 0.05) 1px, transparent 1px)",
              backgroundSize: "14px 10px",
            }}
          />
          {/* Minimal Warm Golden Window Illumination Lights (#FFD98A) */}
          <div className="absolute top-[3px] left-[18px] w-2 h-1.5 rounded-[0.5px] bg-[#FFD98A] opacity-35 blur-[0.4px]" />
          <div className="absolute bottom-[4px] right-[24px] w-2 h-1.5 rounded-[0.5px] bg-[#FFD98A] opacity-40 blur-[0.4px]" />
          <div className="absolute top-[12px] right-[48%] w-2 h-1.5 rounded-[0.5px] bg-[#FFD98A] opacity-25 blur-[0.4px]" />
        </div>
      )}

      {/* Top Glass Reflection Line */}
      <div
        className="absolute top-0 left-0 right-0 h-[1px] pointer-events-none"
        style={{
          background: isDark
            ? "linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent)"
            : "linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.35), transparent)",
        }}
      />

      {/* Hover Glass Light Reflection Slide Effect */}
      <div
        className="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-500 pointer-events-none"
        style={{
          background: isDark
            ? "linear-gradient(105deg, transparent 20%, rgba(255, 255, 255, 0.06) 50%, transparent 80%)"
            : "linear-gradient(105deg, transparent 20%, rgba(255, 255, 255, 0.15) 50%, transparent 80%)",
        }}
      />

      {/* Button Content */}
      <span className="relative z-10 flex items-center justify-center gap-2">
        {isLoading ? (
          <>
            <RefreshCw size={14} className="animate-spin" />
            <span>{loadingText ?? children}</span>
          </>
        ) : (
          children
        )}
      </span>
    </button>
  );
}

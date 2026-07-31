/**
 * ArchitecturalLineIllustration — Adaptive Architectural Glass Skyscraper Illustration.
 * Light Mode: High-contrast, crisp architectural line art with prominent, large Sun in top-left corner.
 * Dark Mode: Dark glass skyscraper facade after sunset with warm illuminated windows (#FFD98A) and large glowing Moon in top-left.
 */

import React from "react";

interface ArchitecturalLineIllustrationProps {
  isDark?: boolean;
}

export function ArchitecturalLineIllustration({ isDark = false }: ArchitecturalLineIllustrationProps) {
  const strokeColor = isDark ? "#656D4A" : "#333D29";
  const strokeOpacity = isDark ? 0.6 : 0.55;
  const windowLitFill = "#FFD98A";

  // Pre-selected indices for warm illuminated windows at night
  const litCentralWindows = new Set(["ct-110-235", "ct-138-210", "ct-164-260", "ct-220-235", "ct-248-210"]);
  const litMidLeftWindows = new Set(["ml-178-136", "ml-230-162"]);
  const litRightWindows = new Set(["rt-196-342", "rt-244-320"]);

  return (
    <svg
      viewBox="0 0 440 340"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className="w-full h-full transition-colors duration-350"
      aria-hidden="true"
    >
      <defs>
        {/* Soft Ambient Window & Celestial Glow Filter */}
        <filter id="windowGlow" x="-30%" y="-30%" width="160%" height="160%">
          <feGaussianBlur stdDeviation="2.5" result="blur" />
          <feComposite in="SourceGraphic" in2="blur" operator="over" />
        </filter>

        {/* Daytime Sun Soft Radial Aura */}
        <radialGradient id="sunGlowLight" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor="#414833" stopOpacity="0.25" />
          <stop offset="60%" stopColor="#A4AC86" stopOpacity="0.12" />
          <stop offset="100%" stopColor="#FAFAF7" stopOpacity="0" />
        </radialGradient>
      </defs>

      {/* ============================================================ */}
      {/* CELESTIAL ELEMENT IN TOP-LEFT CORNER (LARGE SUN / LARGE MOON) */}
      {/* ============================================================ */}
      {isDark ? (
        /* NIGHTTIME MOON & CELESTIAL STARS — LARGE SCALE */
        <g id="architectural-moon" className="transition-opacity duration-350">
          {/* Outer Moon Ambient Glow Disk */}
          <circle cx="68" cy="68" r="40" fill="#FFD98A" fillOpacity="0.12" filter="url(#windowGlow)" />

          {/* Large Glowing Golden Crescent Moon */}
          <path
            d="M 52 46 A 25 25 0 1 0 90 84 A 21 21 0 1 1 52 46 Z"
            fill={windowLitFill}
            fillOpacity="0.92"
            stroke="#FFD98A"
            strokeWidth="1"
            filter="url(#windowGlow)"
          />

          {/* Larger Architectural Stars */}
          <circle cx="112" cy="42" r="2" fill="#FFD98A" fillOpacity="0.85" />
          <circle cx="26" cy="94" r="1.6" fill="#FFD98A" fillOpacity="0.7" />
          <circle cx="128" cy="80" r="1.4" fill="#FFD98A" fillOpacity="0.6" />
        </g>
      ) : (
        /* DAYTIME HIGH-CONTRAST ARCHITECTURAL SUN — LARGE SCALE */
        <g id="architectural-sun" className="transition-opacity duration-350">
          {/* Outer Soft Ambient Aura */}
          <circle cx="68" cy="68" r="54" fill="url(#sunGlowLight)" />

          {/* Outer Geometric Dashed Halo Ring */}
          <circle
            cx="68"
            cy="68"
            r="42"
            stroke="#333D29"
            strokeWidth="1.4"
            strokeDasharray="5 4"
            strokeOpacity="0.75"
          />

          {/* 12 Architectural Ray Lines (Cardinal & Diagonal) */}
          {[0, 30, 60, 90, 120, 150, 180, 210, 240, 270, 300, 330].map((angle) => {
            const rad = (angle * Math.PI) / 180;
            const isCardinal = angle % 90 === 0;
            const innerR = 26;
            const outerR = isCardinal ? 38 : 34;
            const x1 = 68 + Math.cos(rad) * innerR;
            const y1 = 68 + Math.sin(rad) * innerR;
            const x2 = 68 + Math.cos(rad) * outerR;
            const y2 = 68 + Math.sin(rad) * outerR;
            return (
              <line
                key={`sun-ray-${angle}`}
                x1={x1}
                y1={y1}
                x2={x2}
                y2={y2}
                stroke="#333D29"
                strokeWidth={isCardinal ? "1.8" : "1.3"}
                strokeOpacity={isCardinal ? "0.9" : "0.7"}
              />
            );
          })}

          {/* Main Sun Body Disk */}
          <circle
            cx="68"
            cy="68"
            r="23"
            stroke="#333D29"
            strokeWidth="1.8"
            strokeOpacity="0.95"
            fill="#F4F1EA"
          />

          {/* Inner Accent Ring & Core Dot */}
          <circle cx="68" cy="68" r="11" fill="#414833" fillOpacity="0.85" />
          <circle cx="68" cy="68" r="4.5" fill="#FFD98A" fillOpacity="0.95" />
        </g>
      )}

      {/* Ground line */}
      <line
        x1="0"
        y1="290"
        x2="440"
        y2="290"
        stroke={strokeColor}
        strokeWidth="1.2"
        strokeOpacity={strokeOpacity}
      />

      {/* Left Lowrise Structure */}
      <rect
        x="20"
        y="220"
        width="56"
        height="70"
        rx="1"
        stroke={strokeColor}
        strokeWidth="1"
        strokeOpacity={strokeOpacity * 0.9}
        fill={isDark ? "rgba(22, 27, 34, 0.4)" : "rgba(255, 255, 255, 0.2)"}
      />
      <rect x="28" y="232" width="16" height="14" rx="0.5" stroke={strokeColor} strokeWidth="0.8" strokeOpacity={strokeOpacity * 0.85} fill="none" />
      <rect x="50" y="232" width="16" height="14" rx="0.5" stroke={strokeColor} strokeWidth="0.8" strokeOpacity={strokeOpacity * 0.85} fill="none" />
      <rect x="28" y="254" width="16" height="14" rx="0.5" stroke={strokeColor} strokeWidth="0.8" strokeOpacity={strokeOpacity * 0.85} fill="none" />
      <rect x="50" y="254" width="16" height="14" rx="0.5" stroke={strokeColor} strokeWidth="0.8" strokeOpacity={strokeOpacity * 0.85} fill="none" />

      {/* Mid-Left High-rise */}
      <rect
        x="100"
        y="140"
        width="76"
        height="150"
        rx="1"
        stroke={strokeColor}
        strokeWidth="1.1"
        strokeOpacity={strokeOpacity * 1.05}
        fill={isDark ? "rgba(22, 27, 34, 0.5)" : "rgba(255, 255, 255, 0.3)"}
      />
      <rect x="100" y="126" width="76" height="14" rx="0.5" stroke={strokeColor} strokeWidth="0.85" strokeOpacity={strokeOpacity * 0.85} fill="none" />
      {[152, 178, 204, 230, 256].map((y) => (
        <React.Fragment key={`ml-${y}`}>
          {[110, 136, 162].map((x) => {
            const key = `ml-${y}-${x}`;
            const isLit = isDark && litMidLeftWindows.has(key);
            return (
              <rect
                key={key}
                x={x}
                y={y}
                width="18"
                height="16"
                rx="0.5"
                stroke={isLit ? windowLitFill : strokeColor}
                strokeWidth="0.8"
                strokeOpacity={isLit ? 0.85 : strokeOpacity * 0.85}
                fill={isLit ? windowLitFill : "none"}
                fillOpacity={isLit ? 0.75 : 0}
                filter={isLit ? "url(#windowGlow)" : undefined}
              />
            );
          })}
        </React.Fragment>
      ))}

      {/* Central Modern Glass Tower */}
      <rect
        x="200"
        y="70"
        width="90"
        height="220"
        rx="1"
        stroke={strokeColor}
        strokeWidth="1.3"
        strokeOpacity={strokeOpacity * 1.2}
        fill={isDark ? "rgba(22, 27, 34, 0.7)" : "rgba(255, 255, 255, 0.4)"}
      />
      <rect x="212" y="52" width="66" height="18" rx="0.5" stroke={strokeColor} strokeWidth="0.9" strokeOpacity={strokeOpacity} fill="none" />
      <line x1="245" y1="20" x2="245" y2="52" stroke={isDark ? windowLitFill : strokeColor} strokeWidth="1.2" strokeOpacity={isDark ? 0.85 : 0.7} />
      <circle cx="245" cy="18" r="2.8" fill={isDark ? windowLitFill : "#936639"} filter={isDark ? "url(#windowGlow)" : undefined} />

      {[82, 110, 138, 164, 192, 220, 248].map((y) => (
        <React.Fragment key={`ct-${y}`}>
          {[210, 235, 260].map((x) => {
            const key = `ct-${y}-${x}`;
            const isLit = isDark && litCentralWindows.has(key);
            return (
              <rect
                key={key}
                x={x}
                y={y}
                width="20"
                height="18"
                rx="0.5"
                stroke={isLit ? windowLitFill : strokeColor}
                strokeWidth="0.85"
                strokeOpacity={isLit ? 0.95 : strokeOpacity * 0.9}
                fill={isLit ? windowLitFill : "none"}
                fillOpacity={isLit ? 0.85 : 0}
                filter={isLit ? "url(#windowGlow)" : undefined}
              />
            );
          })}
        </React.Fragment>
      ))}

      {/* Right Mid-tower */}
      <rect
        x="310"
        y="160"
        width="70"
        height="130"
        rx="1"
        stroke={strokeColor}
        strokeWidth="1.0"
        strokeOpacity={strokeOpacity}
        fill={isDark ? "rgba(22, 27, 34, 0.45)" : "rgba(255, 255, 255, 0.25)"}
      />
      {[172, 196, 220, 244, 268].map((y) => (
        <React.Fragment key={`rt-${y}`}>
          {[320, 342, 364].map((x) => {
            const key = `rt-${y}-${x}`;
            const isLit = isDark && litRightWindows.has(key);
            return (
              <rect
                key={key}
                x={x}
                y={y}
                width="16"
                height="14"
                rx="0.5"
                stroke={isLit ? windowLitFill : strokeColor}
                strokeWidth="0.8"
                strokeOpacity={isLit ? 0.85 : strokeOpacity * 0.8}
                fill={isLit ? windowLitFill : "none"}
                fillOpacity={isLit ? 0.7 : 0}
                filter={isLit ? "url(#windowGlow)" : undefined}
              />
            );
          })}
        </React.Fragment>
      ))}

      {/* Far Right Structure */}
      <rect
        x="395"
        y="210"
        width="35"
        height="80"
        rx="1"
        stroke={strokeColor}
        strokeWidth="0.9"
        strokeOpacity={strokeOpacity * 0.85}
        fill={isDark ? "rgba(22, 27, 34, 0.35)" : "rgba(255, 255, 255, 0.2)"}
      />
    </svg>
  );
}

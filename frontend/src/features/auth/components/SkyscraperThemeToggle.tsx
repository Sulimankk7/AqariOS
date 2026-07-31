/**
 * SkyscraperThemeToggle — Custom Architectural Theme Switch.
 * Replaces standard sun/moon toggle with a tiny glass skyscraper.
 *
 * Light Mode: Transparent reflective glass tower with sky reflection.
 * Dark Mode: Smoked dark glass tower with warm golden illuminated windows (#FFD98A).
 * Smooth 350ms day-to-night transition animation with gentle lighting reflections.
 */

interface SkyscraperThemeToggleProps {
  isDark: boolean;
  onToggle: () => void;
}

export function SkyscraperThemeToggle({ isDark, onToggle }: SkyscraperThemeToggleProps) {
  return (
    <button
      type="button"
      onClick={onToggle}
      aria-label={isDark ? "Switch to Daytime Architectural Mode" : "Switch to Nighttime Skyscraper Mode"}
      title={isDark ? "Daytime Architecture" : "Nighttime Architecture"}
      className="group relative flex items-center justify-between w-16 h-8 p-1 rounded-full bg-gradient-to-r border transition-all duration-350 cursor-pointer outline-none focus:ring-2 focus:ring-[#A4AC86]/30 overflow-hidden shadow-xs select-none"
      style={{
        backgroundColor: isDark ? "rgba(22, 27, 34, 0.9)" : "rgba(243, 244, 242, 0.9)",
        borderColor: isDark ? "rgba(255, 255, 255, 0.12)" : "rgba(0, 0, 0, 0.08)",
      }}
    >
      {/* Sky / Ambient Backlight Glow inside switch */}
      <div
        className="absolute inset-0 transition-opacity duration-350 pointer-events-none"
        style={{
          background: isDark
            ? "radial-gradient(circle at 80% 50%, rgba(255, 217, 138, 0.12), transparent 70%)"
            : "radial-gradient(circle at 20% 50%, rgba(164, 172, 134, 0.15), transparent 70%)",
        }}
      />

      {/* Day / Night Text Labels (Subtle) */}
      <span
        className={`text-[9px] font-mono font-medium tracking-tighter transition-all duration-350 ${
          isDark ? "opacity-0 translate-x-2" : "opacity-60 translate-x-1 text-[#656D4A]"
        }`}
      >
        DAY
      </span>
      <span
        className={`text-[9px] font-mono font-medium tracking-tighter transition-all duration-350 ${
          isDark ? "opacity-70 -translate-x-1 text-[#FFD98A]" : "opacity-0 -translate-x-2"
        }`}
      >
        NIGHT
      </span>

      {/* Sliding Glass Skyscraper Knob */}
      <div
        className="absolute top-1 w-6 h-6 rounded-md flex items-center justify-center transition-all duration-350 ease-out shadow-sm border overflow-hidden"
        style={{
          transform: isDark ? "translateX(32px)" : "translateX(0px)",
          backgroundColor: isDark ? "#1C2128" : "#FFFFFF",
          borderColor: isDark ? "rgba(255, 255, 255, 0.18)" : "rgba(0, 0, 0, 0.12)",
          boxShadow: isDark
            ? "0 2px 8px rgba(0, 0, 0, 0.5), inset 0 1px 0 rgba(255, 255, 255, 0.1)"
            : "0 2px 6px rgba(0, 0, 0, 0.08), inset 0 1px 0 rgba(255, 255, 255, 0.8)",
        }}
      >
        {/* Tiny Glass Skyscraper SVG Facade */}
        <svg
          viewBox="0 0 20 20"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          className="w-4 h-4 transition-transform duration-350"
        >
          {/* Main Tower Body */}
          <rect
            x="5"
            y="3"
            width="10"
            height="14"
            rx="1"
            stroke={isDark ? "#A4AC86" : "#414833"}
            strokeWidth="0.8"
            fill={isDark ? "#161B22" : "#FAFAF7"}
            fillOpacity={isDark ? "0.9" : "0.7"}
          />

          {/* Roof Spire */}
          <line
            x1="10"
            y1="1"
            x2="10"
            y2="3"
            stroke={isDark ? "#FFD98A" : "#656D4A"}
            strokeWidth="0.8"
          />

          {/* Windows Grid (Row 1) */}
          <rect
            x="7"
            y="5"
            width="2"
            height="2"
            rx="0.3"
            fill={isDark ? "#FFD98A" : "#A4AC86"}
            fillOpacity={isDark ? "0.9" : "0.4"}
          />
          <rect
            x="11"
            y="5"
            width="2"
            height="2"
            rx="0.3"
            fill={isDark ? "none" : "#A4AC86"}
            stroke={isDark ? "#A4AC86" : "none"}
            strokeWidth="0.5"
            fillOpacity="0.4"
          />

          {/* Windows Grid (Row 2) */}
          <rect
            x="7"
            y="9"
            width="2"
            height="2"
            rx="0.3"
            fill={isDark ? "none" : "#A4AC86"}
            stroke={isDark ? "#A4AC86" : "none"}
            strokeWidth="0.5"
            fillOpacity="0.4"
          />
          <rect
            x="11"
            y="9"
            width="2"
            height="2"
            rx="0.3"
            fill={isDark ? "#FFD98A" : "#A4AC86"}
            fillOpacity={isDark ? "0.85" : "0.4"}
          />

          {/* Windows Grid (Row 3 - Entrance) */}
          <rect
            x="7"
            y="13"
            width="2"
            height="3"
            rx="0.3"
            fill={isDark ? "#FFD98A" : "#656D4A"}
            fillOpacity={isDark ? "0.95" : "0.5"}
          />
          <rect
            x="11"
            y="13"
            width="2"
            height="3"
            rx="0.3"
            fill={isDark ? "none" : "#A4AC86"}
            stroke={isDark ? "#A4AC86" : "none"}
            strokeWidth="0.5"
            fillOpacity="0.4"
          />
        </svg>

        {/* Polished Glass Top Highlight Line */}
        <div
          className="absolute top-0 left-0 right-0 h-[1px] pointer-events-none"
          style={{
            background: isDark
              ? "linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.25), transparent)"
              : "linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.9), transparent)",
          }}
        />
      </div>
    </button>
  );
}

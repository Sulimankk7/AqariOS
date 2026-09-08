import { useEffect, useId, type AnimationEvent } from "react";

interface AqariosLogoDrawAnimationProps {
  isAnimating: boolean;
  prefersReducedMotion: boolean;
  onComplete: () => void;
}

const LOGO_ASSET = "/branding/aqarios-logo.png";

export function AqariosLogoDrawAnimation({
  isAnimating,
  prefersReducedMotion,
  onComplete,
}: AqariosLogoDrawAnimationProps) {
  const instanceId = useId().replace(/:/g, "");
  const architectureMaskId = `${instanceId}-architecture-mask`;
  const accentMaskId = `${instanceId}-accent-mask`;
  const accentOnlyFilterId = `${instanceId}-accent-only`;
  const withoutAccentFilterId = `${instanceId}-without-accent`;
  const shouldDraw = isAnimating && !prefersReducedMotion;

  useEffect(() => {
    if (isAnimating && prefersReducedMotion) onComplete();
  }, [isAnimating, onComplete, prefersReducedMotion]);

  const handleFinalStrokeComplete = (event: AnimationEvent<SVGPathElement>) => {
    if (shouldDraw && event.animationName === "aqarios-logo-draw-stroke") onComplete();
  };

  return (
    <svg
      className="aqarios-logo-draw"
      viewBox="0 0 1280 1280"
      role="img"
      aria-label="شعار AqariOS"
      preserveAspectRatio="xMidYMid meet"
    >
      <defs>
        <filter
          id={accentOnlyFilterId}
          x="-10%"
          y="-10%"
          width="120%"
          height="120%"
          colorInterpolationFilters="sRGB"
        >
          <feColorMatrix
            in="SourceGraphic"
            type="matrix"
            values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  -40 40 0 0 0"
            result="green-accent-mask"
          />
          <feComposite in="SourceGraphic" in2="green-accent-mask" operator="in" />
        </filter>
        <filter
          id={withoutAccentFilterId}
          x="-10%"
          y="-10%"
          width="120%"
          height="120%"
          colorInterpolationFilters="sRGB"
        >
          <feColorMatrix
            in="SourceGraphic"
            type="matrix"
            values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  -40 40 0 0 0"
            result="green-accent-mask"
          />
          <feComposite in="SourceGraphic" in2="green-accent-mask" operator="out" />
        </filter>

        <mask id={architectureMaskId} maskUnits="userSpaceOnUse" x="0" y="0" width="1280" height="1280">
          <rect width="1280" height="1280" fill="black" />
          {shouldDraw && <g className="aqarios-logo-draw__mask-paths">
            <path
              className="aqarios-logo-draw__stroke aqarios-logo-draw__stroke--outer-left"
              pathLength="1"
              d="M242 1090 L242 258 L487 130 L487 815 L350 1030 Q302 1090 242 1090"
            />
            <path
              className="aqarios-logo-draw__stroke aqarios-logo-draw__stroke--outer-right"
              pathLength="1"
              d="M1038 1080 L1038 532 L754 384 L754 520 L570 520 L570 760"
            />
            <path
              className="aqarios-logo-draw__stroke aqarios-logo-draw__stroke--connectors"
              pathLength="1"
              d="M488 850 L612 674 L612 520 M754 520 L754 704"
            />
            <path
              className="aqarios-logo-draw__stroke aqarios-logo-draw__stroke--q"
              pathLength="1"
              d="M350 1030 C456 920 514 758 648 730 C790 700 920 786 924 918 C927 1015 862 1082 790 1092 C744 1052 710 1003 660 1000 C620 998 586 1020 558 1050"
            />
            <path
              className="aqarios-logo-draw__stroke aqarios-logo-draw__stroke--q-tail"
              pathLength="1"
              d="M642 1002 C684 958 746 958 796 1004 L884 1080 C930 1118 984 1118 1038 1080"
            />
          </g>}
        </mask>

        <mask id={accentMaskId} maskUnits="userSpaceOnUse" x="0" y="0" width="1280" height="1280">
          <rect width="1280" height="1280" fill="black" />
          {shouldDraw && <path
            className="aqarios-logo-draw__stroke aqarios-logo-draw__stroke--accent"
            pathLength="1"
            d="M515 958 C552 1050 652 1110 805 1062"
            onAnimationEnd={handleFinalStrokeComplete}
          />}
        </mask>
      </defs>

      {shouldDraw ? (
        <>
          <image
            href={LOGO_ASSET}
            width="1280"
            height="1280"
            filter={`url(#${withoutAccentFilterId})`}
            mask={`url(#${architectureMaskId})`}
          />
          <image
            href={LOGO_ASSET}
            width="1280"
            height="1280"
            filter={`url(#${accentOnlyFilterId})`}
            mask={`url(#${accentMaskId})`}
          />
        </>
      ) : (
        <image href={LOGO_ASSET} width="1280" height="1280" />
      )}
    </svg>
  );
}

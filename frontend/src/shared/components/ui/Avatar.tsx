/**
 * Avatar & CompanyAvatar Components — User & Tenant logo avatars.
 * Supports image loading, initial letter fallbacks, and AqariOS brand brown/green styling.
 */

import React from "react";
import { Building2 } from "lucide-react";

export interface AvatarProps {
  name: string;
  src?: string;
  size?: "sm" | "md" | "lg";
  className?: string;
}

const sizeMap = {
  sm: "w-7 h-7 text-[10px]",
  md: "w-9 h-9 text-xs",
  lg: "w-12 h-12 text-base",
};

export function Avatar({ name, src, size = "md", className = "" }: AvatarProps) {
  const initials = name
    ? name
        .split(" ")
        .map((n) => n[0])
        .slice(0, 2)
        .join("")
        .toUpperCase()
    : "U";

  return (
    <div
      className={`relative inline-flex items-center justify-center rounded-full bg-brand-brown-600 text-white font-bold font-mono overflow-hidden shrink-0 select-none shadow-xs ${sizeMap[size]} ${className}`}
    >
      {src ? (
        <img src={src} alt={name} className="w-full h-full object-cover" />
      ) : (
        <span>{initials}</span>
      )}
    </div>
  );
}

export interface CompanyAvatarProps {
  name: string;
  code?: string;
  size?: "sm" | "md" | "lg";
  className?: string;
}

export function CompanyAvatar({
  name,
  code,
  size = "md",
  className = "",
}: CompanyAvatarProps) {
  return (
    <div
      className={`inline-flex items-center justify-center rounded-lg bg-brand-green-900 text-white font-bold font-mono shrink-0 shadow-xs ${sizeMap[size]} ${className}`}
      title={name}
    >
      {code ? code.slice(0, 2).toUpperCase() : <Building2 className="w-4 h-4" />}
    </div>
  );
}

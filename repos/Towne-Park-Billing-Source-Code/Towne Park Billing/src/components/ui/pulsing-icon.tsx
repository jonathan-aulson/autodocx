import "./pulsing-icon.css";

import React, { ReactNode } from "react";

interface PulsingIconProps {
  isPulsing?: boolean;
  children: ReactNode;
}

const PulsingIcon: React.FC<PulsingIconProps> = ({ children, isPulsing = true }) => {

  return (
    <div className={`pulsing-icon ${isPulsing ? "pulse" : ""}`}>{children}</div>
  );
};

export default PulsingIcon;

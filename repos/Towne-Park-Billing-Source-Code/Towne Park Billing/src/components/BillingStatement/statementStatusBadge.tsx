import { Badge, BadgeProps } from "@/components/ui/badge";
import { StatementStatus } from "@/lib/models/Statement";
import React from "react";

type StatementStatusBadgeProps = {
  status: StatementStatus;
};

const StatementStatusBadge: React.FC<StatementStatusBadgeProps> = ({ status }) => {
  const badgeProps: BadgeProps = { variant: "indigo" };
  switch (status) {
    case StatementStatus.SENT:
      badgeProps.variant = "indigo";
      break;
    case StatementStatus.NEEDS_REVIEW:
      badgeProps.variant = "amber";
      break;
    case StatementStatus.APPROVED:
    case StatementStatus.READY_TO_SEND:
      badgeProps.variant = "green";
      break;
    case StatementStatus.AR_REVIEW:
    case StatementStatus.APPROVAL_TEAM:
    case StatementStatus.FAILED:
      badgeProps.variant = "destructive";
      break;
      default:
      badgeProps.variant = "default";
      break;
  }

  return <Badge className="truncate" variant={badgeProps.variant}>{status}</Badge>;
};

export default StatementStatusBadge;

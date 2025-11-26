import {createContext, useContext} from "react";

interface StatementContextValue {
  reloadStatementsToggle: () => void
}
export const StatementContext = createContext<StatementContextValue>({reloadStatementsToggle: () => {}});

export const useStatementContext = () => {
  const context = useContext(StatementContext);
  if (!context) {
    throw new Error("useStatementContext must be used within an StatementContext provider");
  }
  return context;
}
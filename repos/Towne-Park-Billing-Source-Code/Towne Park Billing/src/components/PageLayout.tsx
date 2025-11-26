import { Outlet } from "react-router-dom";
import { SiteHeader } from "./SiteHeader";

const PageLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  return (
      <div className="flex flex-col">
        <SiteHeader />
        <main className="flex-1 p-4 pt-20 pb-20 md:pt-18 lg:pt-28">
          {children || <Outlet />}
        </main>
      </div>
  );
};

export default PageLayout;
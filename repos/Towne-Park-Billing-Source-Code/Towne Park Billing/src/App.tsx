import { routes } from "@/authConfig";
import { Route, Routes } from "react-router-dom";
import "./App.css";
import AdminPanel from "./components/AdminPanel/AdminPanel";
import PageLayout from "./components/PageLayout";
import { AuthProvider } from './contexts/AuthContext';
import { CustomerProvider } from './contexts/CustomerContext';
import { CustomerProvider as CustomerDetailsProvider } from "./pages/customersDetails/CustomersDetailContext";
import { CustomersDetails } from "./pages/customersDetails/CustomersDetails";
import { CustomersList } from "./pages/customersList/CustomersList";
import ErrorPage from "./pages/errorPage/ErrorPage";
import { Forecasts } from "./pages/forecasts/forecasts";
import { LoginPage } from "./pages/loginPage/LoginPage";
import PnlView from "./pages/pnl/PnlView";
import StatementsGlobalList from "./pages/statements/StatementsGlobalList";

function App() {
  return (
    <AuthProvider>
      <CustomerProvider>
        <Routes>
          <Route path="/" element={<LoginPage />} />
          <Route path={routes.customersList} element={<PageLayout><CustomersList /></PageLayout>} />
          <Route path={routes.customersDetails} element={
            <PageLayout>
              <CustomerDetailsProvider>
                <CustomersDetails />
              </CustomerDetailsProvider>
            </PageLayout>
          } />
          <Route path={routes.forecasts} element={<PageLayout><Forecasts /></PageLayout>} />
          <Route path={routes.adminPanel} element={<PageLayout><AdminPanel /></PageLayout>} />
          <Route path={routes.statements} element={
            <PageLayout>
              <CustomerDetailsProvider>
                <StatementsGlobalList />
              </CustomerDetailsProvider>
            </PageLayout>} />
          <Route path={routes.pnlView} element={<PageLayout><PnlView /></PageLayout>} />
          <Route path={routes.pnlViewWithId} element={<PageLayout><PnlView /></PageLayout>} />
          <Route path="*" element={<PageLayout><ErrorPage /></PageLayout>} />
        </Routes>
      </CustomerProvider>
    </AuthProvider>
  );
}

export default App;
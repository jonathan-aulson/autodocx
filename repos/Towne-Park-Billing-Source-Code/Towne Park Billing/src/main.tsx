import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import { ThemeProvider } from "./components/ThemeProvider";
import { Toaster } from "./components/ui/toaster";
import './index.css';


const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement);
root.render(
  <React.StrictMode>
    <BrowserRouter>
    <ThemeProvider defaultTheme="light" storageKey="vite-ui-theme">
    <Toaster />
      <App />
    </ThemeProvider>
    </BrowserRouter>
  </React.StrictMode>
);
import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/layout/ProtectedRoute';
import MainLayout from './components/layout/MainLayout';

// Pages
import LoginPage from './features/auth/pages/LoginPage';
import PosPage from './features/pos/pages/PosPage';
import OrdersPage from './features/orders/pages/OrdersPage';
import ProductsPage from './features/products/pages/ProductsPage';
import CategoriesPage from './features/categories/pages/CategoriesPage';
import TreasuryPage from './features/treasury/pages/TreasuryPage';
import ReportsPage from './features/reports/pages/ReportsPage';
import DashboardPage from './features/dashboard/pages/DashboardPage';
import SettingsPage from './features/settings/pages/SettingsPage';

export const App: React.FC = () => {
  return (
    <AuthProvider>
      <BrowserRouter>
        {/* Global Toast Notifications Configured with Arabic RTL Styling */}
        <Toaster
          position="top-center"
          reverseOrder={false}
          toastOptions={{
            duration: 3500,
            style: {
              fontFamily: 'Cairo, sans-serif',
              borderRadius: '10px',
              background: '#ffffff',
              color: '#111827',
              boxShadow: '0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.04)',
              border: '1px solid #e5e7eb',
              padding: '12px 18px',
              fontSize: '0.9375rem',
            },
            success: {
              iconTheme: {
                primary: '#10b981',
                secondary: '#ffffff',
              },
            },
            error: {
              iconTheme: {
                primary: '#ef4444',
                secondary: '#ffffff',
              },
            },
          }}
        />

        <Routes>
          {/* Public Route: Login */}
          <Route path="/login" element={<LoginPage />} />

          {/* Protected Routes inside Main Shell Layout */}
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <MainLayout />
              </ProtectedRoute>
            }
          >
            {/* Default Route redirect */}
            <Route index element={<Navigate to="/pos" replace />} />

            {/* POS Screen (F1) */}
            <Route
              path="pos"
              element={
                <ProtectedRoute requiredPermission="Permission.NewOrder">
                  <PosPage />
                </ProtectedRoute>
              }
            />

            {/* Orders Screen (F2) */}
            <Route
              path="orders"
              element={
                <ProtectedRoute requiredPermission="Permission.Orders">
                  <OrdersPage />
                </ProtectedRoute>
              }
            />

            {/* Products Screen (F3) */}
            <Route
              path="products"
              element={
                <ProtectedRoute requiredPermission="Permission.Products">
                  <ProductsPage />
                </ProtectedRoute>
              }
            />

            {/* Categories Screen */}
            <Route
              path="categories"
              element={
                <ProtectedRoute requiredPermission="Permission.Categories">
                  <CategoriesPage />
                </ProtectedRoute>
              }
            />

            {/* Treasury Screen (F4) */}
            <Route
              path="treasury"
              element={
                <ProtectedRoute requiredPermission="Permission.Treasury">
                  <TreasuryPage />
                </ProtectedRoute>
              }
            />

            {/* Reports Screen (F5) */}
            <Route
              path="reports"
              element={
                <ProtectedRoute requiredPermission="Permission.Reports">
                  <ReportsPage />
                </ProtectedRoute>
              }
            />

            {/* Dashboard Screen */}
            <Route
              path="dashboard"
              element={
                <ProtectedRoute requiredPermission="Permission.Dashboard">
                  <DashboardPage />
                </ProtectedRoute>
              }
            />

            {/* Settings Screen */}
            <Route
              path="settings"
              element={
                <ProtectedRoute requireAdmin={true}>
                  <SettingsPage />
                </ProtectedRoute>
              }
            />
          </Route>

          {/* Catch-all Fallback */}
          <Route path="*" element={<Navigate to="/pos" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
};

export default App;

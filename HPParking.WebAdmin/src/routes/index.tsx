import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AuthGuard } from '@/components/layout/AuthGuard';
import { AppLayout } from '@/components/layout/AppLayout';
import { LoginPage } from '@/pages/LoginPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { CompaniesPage } from '@/pages/CompaniesPage';
import { DepartmentsPage } from '@/pages/DepartmentsPage';
import { ContractorsPage } from '@/pages/ContractorsPage';
import { GatesPage } from '@/pages/GatesPage';
import { LanesPage } from '@/pages/LanesPage';
import { DevicesPage } from '@/pages/DevicesPage';
import { ClientsPage } from '@/pages/ClientsPage';
import { VehiclesPage } from '@/pages/VehiclesPage';
import { ParkingSessionsPage } from '@/pages/ParkingSessionsPage';
import { RecycleBinPage } from '@/pages/RecycleBinPage';
import { NotFoundPage } from '@/pages/NotFoundPage';

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/',
    element: (
      <AuthGuard>
        <AppLayout />
      </AuthGuard>
    ),
    children: [
      {
        index: true,
        element: <Navigate to="/dashboard" replace />,
      },
      {
        path: 'dashboard',
        element: <DashboardPage />,
      },
      {
        path: 'companies',
        element: <CompaniesPage />,
      },
      {
        path: 'departments',
        element: <DepartmentsPage />,
      },
      {
        path: 'contractors',
        element: <ContractorsPage />,
      },
      {
        path: 'gates',
        element: <GatesPage />,
      },
      {
        path: 'lanes',
        element: <LanesPage />,
      },
      {
        path: 'devices',
        element: <DevicesPage />,
      },
      {
        path: 'clients',
        element: <ClientsPage />,
      },
      {
        path: 'vehicles',
        element: <VehiclesPage />,
      },
      {
        path: 'parking-sessions',
        element: <ParkingSessionsPage />,
      },
      {
        path: 'recycle-bin',
        element: <RecycleBinPage />,
      },
    ],
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
]);

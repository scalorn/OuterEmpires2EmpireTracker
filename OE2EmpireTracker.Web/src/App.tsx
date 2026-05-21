import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom';
import { AppShell } from './components/layout/AppShell';
import { AuthGuard } from './auth/AuthGuard';
import { LoginPage } from './auth/LoginPage';
import { NotFound } from './pages/NotFound';
import { ErrorBoundary } from './components/common/ErrorBoundary';
import { BlueprintBrowser } from './pages/public/BlueprintBrowser';
import { BlueprintDetail } from './pages/public/BlueprintDetail';
import { SurveyBrowser } from './pages/public/SurveyBrowser';
import { SurveyDetail } from './pages/public/SurveyDetail';
import { ColonyPlanner } from './pages/planner/ColonyPlanner';
import { Dashboard } from './pages/authenticated/Dashboard';
import { ColonyManager } from './pages/authenticated/ColonyManager';
import { BlueprintManager } from './pages/authenticated/BlueprintManager';
import { SurveyManager } from './pages/authenticated/SurveyManager';
import { ProfileEditor } from './pages/authenticated/ProfileEditor';
import { FactionView } from './pages/authenticated/FactionView';
import { SharingConfig } from './pages/authenticated/SharingConfig';
import { useWebSocket } from './ws/useWebSocket';

const router = createBrowserRouter([
  {
    path: '/',
    element: <AppShell />,
    errorElement: <NotFound />,
    children: [
      { index: true, element: <Navigate to="/blueprints" replace /> },
      { path: 'blueprints', element: <BlueprintBrowser /> },
      { path: 'blueprints/:id', element: <BlueprintDetail /> },
      { path: 'surveys', element: <SurveyBrowser /> },
      { path: 'surveys/:id', element: <SurveyDetail /> },
      { path: 'planner', element: <ColonyPlanner /> },
      { path: 'login', element: <LoginPage /> },
      {
        path: 'app',
        element: <AuthGuard />,
        children: [
          { index: true, element: <Dashboard /> },
          { path: 'colonies', element: <ColonyManager /> },
          { path: 'blueprints', element: <BlueprintManager /> },
          { path: 'surveys', element: <SurveyManager /> },
          { path: 'profile', element: <ProfileEditor /> },
          { path: 'faction', element: <FactionView /> },
          { path: 'sharing', element: <SharingConfig /> },
        ],
      },
      { path: '*', element: <NotFound /> },
    ],
  },
]);

export function App() {
  useWebSocket();
  return (
    <ErrorBoundary>
      <RouterProvider router={router} />
    </ErrorBoundary>
  );
}

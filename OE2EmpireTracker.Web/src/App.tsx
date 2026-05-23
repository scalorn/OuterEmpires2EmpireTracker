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
import { ColonyForm } from './pages/authenticated/ColonyForm';
import { BlueprintForm } from './pages/authenticated/BlueprintForm';
import { SurveyForm } from './pages/authenticated/SurveyForm';
import { ProfileForm } from './pages/authenticated/ProfileForm';
import { RouteForm } from './pages/authenticated/RouteForm';
import { ExecutionForm } from './pages/authenticated/ExecutionForm';
import { ShipTemplateForm } from './pages/authenticated/ShipTemplateForm';
import { MarketForm } from './pages/authenticated/MarketForm';
import { SupplyChainForm } from './pages/authenticated/SupplyChainForm';
import { StockTargetForm } from './pages/authenticated/StockTargetForm';
import { PricingPlanForm } from './pages/authenticated/PricingPlanForm';
import { SharedDataView } from './pages/authenticated/SharedDataView';
import { ColonyActivityPage } from './pages/authenticated/ColonyActivityPage';
import { DailyBuildPage } from './pages/authenticated/DailyBuildPage';
import { BuildPlannerForm } from './pages/authenticated/BuildPlannerForm';
import { ContactsForm } from './pages/authenticated/ContactsForm';
import { StationsForm } from './pages/authenticated/StationsForm';
import { AsteroidsForm } from './pages/authenticated/AsteroidsForm';
import { FactionView } from './pages/authenticated/FactionView';
import { SharingConfig } from './pages/authenticated/SharingConfig';
import { useWebSocket } from './ws/useWebSocket';
import { ConnectionBanner } from './components/domain/ConnectionBanner';

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
          { path: 'colonies', element: <ColonyForm /> },
          { path: 'blueprints', element: <BlueprintForm /> },
          { path: 'surveys', element: <SurveyForm /> },
          { path: 'profiles', element: <ProfileForm /> },
          { path: 'routes', element: <RouteForm /> },
          { path: 'delivery', element: <ExecutionForm /> },
          { path: 'ships', element: <ShipTemplateForm /> },
          { path: 'market', element: <MarketForm /> },
          { path: 'supply-chains', element: <SupplyChainForm /> },
          { path: 'stock-targets', element: <StockTargetForm /> },
          { path: 'pricing-plans', element: <PricingPlanForm /> },
          { path: 'shared', element: <SharedDataView /> },
          { path: 'activity', element: <ColonyActivityPage /> },
          { path: 'daily-build', element: <DailyBuildPage /> },
          { path: 'build-planner', element: <BuildPlannerForm /> },
          { path: 'contacts', element: <ContactsForm /> },
          { path: 'stations', element: <StationsForm /> },
          { path: 'asteroids', element: <AsteroidsForm /> },
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

import { useEffect, useState } from 'react'
import { Navigate, Route, Routes, useNavigate } from 'react-router-dom'

import LoginPage from '../pages/auth/LoginPage'
import RegisterPage from '../pages/auth/RegisterPage'
import ConfirmEmailPage from '../pages/auth/ConfirmEmailPage'
import { RequireAuth } from './guards'
import { ROLES, useAuthStore } from '../stores/authStore'
import { candidateApi } from '../api/candidate.api'
import Spinner from '../shared/components/Spinner'

import CandidateLayout from '../layouts/CandidateLayout'
import OnboardingPage from '../pages/candidate/OnboardingPage'
import ProfilePage from '../pages/candidate/ProfilePage'
import CategoryPage from '../pages/candidate/CategoryPage'
import TestPage from '../pages/candidate/TestPage'
import TestSessionPage from '../pages/candidate/TestSessionPage'
import InvitationsPage from '../pages/candidate/InvitationsPage'
import VacanciesPage from '../pages/candidate/VacanciesPage'
import SettingsPage from '../pages/candidate/SettingsPage'

import EmployerLayout from '../layouts/EmployerLayout'
import CompanyPage from '../pages/employer/CompanyPage'
import DemandPage from '../pages/employer/DemandPage'
import ShortlistPage from '../pages/employer/ShortlistPage'
import CandidateCardPage from '../pages/employer/CandidateCardPage'
import InvitePage from '../pages/employer/InvitePage'
import SentInvitationsPage from '../pages/employer/SentInvitationsPage'
import EmployerVacanciesPage from '../pages/employer/VacanciesPage'
import SearchPage from '../pages/employer/SearchPage'
import TasksPage from '../pages/employer/TasksPage'
import AtsPage from '../pages/employer/AtsPage'
import MyTasksPage from '../pages/candidate/MyTasksPage'

// Если у кандидата не выбрана специализация — сначала онбординг
function CandidateHome() {
  const [state, setState] = useState('loading')
  useEffect(() => {
    candidateApi
      .getStatus()
      .then(() => setState('ok'))
      .catch(() => setState('onboarding'))
  }, [])
  if (state === 'loading') return <Spinner />
  return (
    <Navigate
      to={state === 'ok' ? '/candidate/category' : '/candidate/onboarding'}
      replace
    />
  )
}

// «Ворота» кабинета кандидата: без специализации — на онбординг
function CandidateGate() {
  const [state, setState] = useState('loading')
  useEffect(() => {
    candidateApi
      .getStatus()
      .then(() => setState('ok'))
      .catch(() => setState('onboarding'))
  }, [])
  if (state === 'loading') return <Spinner />
  if (state === 'onboarding')
    return <Navigate to="/candidate/onboarding" replace />
  return <CandidateLayout />
}

// «Ворота» кабинета работодателя: без профиля компании — на страницу создания
import { employerApi } from '../api/employer.api'
function EmployerGate() {
  const [state, setState] = useState('loading')
  useEffect(() => {
    employerApi
      .myCompany()
      .then(() => setState('ok'))
      .catch(() => setState('noCompany'))
  }, [])
  if (state === 'loading') return <Spinner />
  return <EmployerLayout />
}

function RootRedirect() {
  const { token, role } = useAuthStore()
  if (!token) return <Navigate to="/login" replace />
  return (
    <Navigate
      to={role === ROLES.EMPLOYER ? '/employer' : '/candidate'}
      replace
    />
  )
}

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      {/* Публичная страница результата подтверждения email (ссылка из письма) */}
      <Route path="/confirm-email" element={<ConfirmEmailPage />} />

      {/* Обязательный поток кандидата — полноэкранный */}
      <Route element={<RequireAuth roles={[ROLES.CANDIDATE]} />}>
        <Route path="/candidate/onboarding" element={<OnboardingPage />} />
        <Route path="/candidate/test/:sessionId" element={<TestSessionPage />} />
      </Route>

      {/* Кабинет кандидата */}
      <Route element={<RequireAuth roles={[ROLES.CANDIDATE]} />}>
        <Route path="/candidate" element={<CandidateGate />}>
          <Route index element={<CandidateHome />} />
          <Route path="profile" element={<ProfilePage />} />
          <Route path="category" element={<CategoryPage />} />
          <Route path="test" element={<TestPage />} />
          <Route path="invitations" element={<InvitationsPage />} />
          <Route path="vacancies" element={<VacanciesPage />} />
          <Route path="tasks" element={<MyTasksPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
      </Route>

      {/* Кабинет работодателя */}
      <Route element={<RequireAuth roles={[ROLES.EMPLOYER]} />}>
        <Route path="/employer" element={<EmployerGate />}>
          <Route index element={<Navigate to="/employer/shortlist" replace />} />
          <Route path="company" element={<CompanyPage />} />
          <Route path="demand" element={<DemandPage />} />
          <Route path="shortlist" element={<ShortlistPage />} />
          <Route path="candidates/:candidateId" element={<CandidateCardPage />} />
          <Route path="invite/:candidateId" element={<InvitePage />} />
          <Route path="invitations" element={<SentInvitationsPage />} />
          <Route path="vacancies" element={<EmployerVacanciesPage />} />
          <Route path="search" element={<SearchPage />} />
          <Route path="tasks" element={<TasksPage />} />
          <Route path="ats" element={<AtsPage />} />
        </Route>
      </Route>

      <Route path="/" element={<RootRedirect />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
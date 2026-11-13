import { Navigate, Route, Routes } from 'react-router-dom'
import { GuestOnly, RequireAuth } from './auth/routeGuards'
import { LoginPage } from './pages/LoginPage'
import { NotesPage } from './pages/NotesPage'
import { RegisterPage } from './pages/RegisterPage'

// Routes (design 7): /login, /register, protected /notes.
export default function App() {
  return (
    <Routes>
      <Route
        path="/login"
        element={
          <GuestOnly>
            <LoginPage />
          </GuestOnly>
        }
      />
      <Route
        path="/register"
        element={
          <GuestOnly>
            <RegisterPage />
          </GuestOnly>
        }
      />
      <Route
        path="/notes"
        element={
          <RequireAuth>
            <NotesPage />
          </RequireAuth>
        }
      />
      <Route path="*" element={<Navigate to="/notes" replace />} />
    </Routes>
  )
}

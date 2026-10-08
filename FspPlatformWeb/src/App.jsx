import { BrowserRouter } from 'react-router-dom'
import AppRoutes from './app/router'
import Toasts from './shared/components/Toasts'

export default function App() {
  return (
    <BrowserRouter>
      <AppRoutes />
      <Toasts />
    </BrowserRouter>
  )
}
import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { HomePage } from './pages/HomePage'
import { TaskPage } from './pages/TaskPage'
import { ResultPage } from './pages/ResultPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RequireAdmin } from './admin/RequireAdmin'
import { AdminLayout } from './admin/components/AdminLayout'
import { OverviewPage } from './admin/pages/OverviewPage'
import { CategoriesPage } from './admin/pages/CategoriesPage'
import { NewTaskPage } from './admin/pages/NewTaskPage'
import { TaskEditorPage } from './admin/pages/TaskEditorPage'
import { TaskPreviewPage } from './admin/pages/TaskPreviewPage'
import { SubmissionsPage } from './admin/pages/SubmissionsPage'
import { SubmissionDetailPage } from './admin/pages/SubmissionDetailPage'
import { TransferPage } from './admin/pages/TransferPage'

export default function App() {
  return (
    <Routes>
      {/* Jede Ansicht hat eine eigene Adresse. Im Referenzprojekt wurde
          zwischen Aufgabe und Ergebnis nur ein useState umgeschaltet — damit
          gab es keinen teilbaren Link, der Zurück-Knopf des Browsers führte
          aus der Anwendung heraus, und ein Neuladen verlor das Ergebnis. */}
      <Route element={<AppLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/aufgaben/:taskId" element={<TaskPage />} />
        <Route path="/abgaben/:submissionId" element={<ResultPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>

      {/* Eigener Zweig mit eigenem Rahmen: die Verwaltung teilt sich mit der
          Teilnehmersicht weder Seitenleiste noch Kopfzeile. RequireAdmin zeigt
          davor die Anmeldung, ohne die Adresse zu wechseln. */}
      <Route path="/admin" element={<RequireAdmin />}>
        <Route element={<AdminLayout />}>
          <Route index element={<OverviewPage />} />
          <Route path="kategorien" element={<CategoriesPage />} />
          {/* "neu" steht vor ":taskId" — sonst schluckt der Parameter das Wort. */}
          <Route path="aufgaben/neu" element={<NewTaskPage />} />
          <Route path="aufgaben/:taskId" element={<TaskEditorPage />} />
          <Route path="aufgaben/:taskId/vorschau" element={<TaskPreviewPage />} />
          <Route path="abgaben" element={<SubmissionsPage />} />
          <Route path="abgaben/:submissionId" element={<SubmissionDetailPage />} />
          <Route path="transfer" element={<TransferPage />} />
        </Route>
      </Route>
    </Routes>
  )
}

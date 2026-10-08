import { api } from './client'

export const tasksApi = {
  // ---------- Работодатель ----------

  // Создать задачу. Возвращает ID (Guid).
  createTask: (dto) => api.post('/employer-tasks', dto).then((r) => r.data),

  // Мои задачи со статистикой (Назначено / Ответили / Средний балл)
  myTasks: () => api.get('/employer-tasks').then((r) => r.data),

  // Все ответы кандидатов по задачам компании, свежие сверху
  // (включая ответы по архивным задачам — архив закрывает только новые назначения)
  completions: () => api.get('/employer-tasks/completions').then((r) => r.data),

  // Назначить одному кандидату (по ID; совместимость со старым сценарием)
  assignTask: (taskId, candidateId) =>
    api.post(`/employer-tasks/${taskId}/assign`, { candidateId }),

  // Массовое назначение выбранным кандидатам. Возвращает число назначенных.
  assignTaskBulk: (taskId, candidateIds) =>
    api.post(`/employer-tasks/${taskId}/assign-bulk`, { candidateIds }).then((r) => r.data),

  // Архивировать задачу
  archiveTask: (taskId) => api.post(`/employer-tasks/${taskId}/archive`),

  // Вернуть задачу из архива
  restoreTask: (taskId) => api.post(`/employer-tasks/${taskId}/restore`),

  // Оценить выполненный ответ (0..maxScore)
  scoreTask: (candidateTaskId, score) =>
    api.post(`/employer-tasks/completions/${candidateTaskId}/score`, { score }),

  // Отозвать назначение (только до начала работы кандидатом; статус "отменено")
  unassignTask: (candidateTaskId) =>
    api.post(`/employer-tasks/completions/${candidateTaskId}/unassign`),

  // ---------- Кандидат: регулярные короткие задания (ТЗ п.2) ----------

  myAssignments: () => api.get('/my-tasks').then((r) => r.data),
  answerTask: (candidateTaskId, dto) =>
    api.post(`/my-tasks/${candidateTaskId}/answer`, dto),
}